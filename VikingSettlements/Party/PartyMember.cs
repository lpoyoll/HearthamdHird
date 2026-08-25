using System.Collections.Generic;
using System.Globalization;
using HearthAndHird.AI;
using HearthAndHird.Network;
using HearthAndHird.NPC;
using UnityEngine;
using VikingSettlements.Npcs;

namespace VikingSettlements.Party
{
    /// <summary>
    /// Per-settler party behavior: stance handling, out-of-combat recovery,
    /// the gravely-wounded auto-retreat, and the owner-proximity flag the
    /// damage contract patch reads. Every settler carries this component but
    /// it only acts while the settler is a flagged party member.
    /// </summary>
    public class PartyMember : MonoBehaviour
    {
        public static readonly List<PartyMember> Instances = new List<PartyMember>();

        private const float RegenDelaySeconds = 10f;
        private const float GravelyWoundedFraction = 0.25f;
        private const float RecoveredFraction = 0.5f;
        private const float TickInterval = 0.5f;
        private const float DefensiveMemberRange = 22f;
        private const float DefensiveOwnerRange = 28f;
        private const float AggressiveRange = 40f;
        private const float RetaliationRange = 45f;
        private const float RetaliationMemorySeconds = 8f;
        private const float DefensiveLeash = 42f;
        private const float AggressiveLeash = 65f;

        private ZNetView _nview;
        private Character _character;
        private MonsterAI _ai;
        private SettlerRecruitable _settler;
        private SettlerDirectiveState _directives;
        private float _lastDamageTime = -1000f;
        private Character _recentAttacker;
        private float _recentAttackerTime = -1000f;
        private float _nextTick;
        private bool _autoFellBack;
        private bool _formationArrived;
        private float _baseRandomMoveInterval;
        private float _baseRandomMoveRange;

        /// <summary>
        /// Whether the recruiter is close enough for this member's fate to be
        /// in play. Updated owner-side, and the damage patch also runs on the
        /// owning machine, so no synchronization is needed. Defaults to false:
        /// when in doubt, the member is protected.
        /// </summary>
        internal bool OwnerNearby { get; private set; }

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
            _character = GetComponent<Character>();
            _ai = GetComponent<MonsterAI>();
            _settler = GetComponent<SettlerRecruitable>();
            _directives = GetComponent<SettlerDirectiveState>();
            if (_ai != null)
            {
                _baseRandomMoveInterval = _ai.m_randomMoveInterval;
                _baseRandomMoveRange = _ai.m_randomMoveRange;
            }
            if (_character != null)
            {
                _character.m_onDamaged += OnDamaged;
            }
        }

        private void OnDestroy()
        {
            if (_character != null)
            {
                _character.m_onDamaged -= OnDamaged;
            }
        }

        private void OnEnable()
        {
            Instances.Add(this);
        }

        private void OnDisable()
        {
            Instances.Remove(this);
        }

        internal bool IsActiveMember => _nview != null && _nview.IsValid()
            && _settler != null && _settler.State == SettlerState.Following
            && _nview.GetZDO().GetBool(PartySystem.PartyKey);

        internal PartyStance Stance => _nview != null && _nview.IsValid()
            ? (PartyStance)_nview.GetZDO().GetInt(PartySystem.StanceKey)
            : PartyStance.Follow;

        internal HirdCombatStance CombatStance => _nview != null && _nview.IsValid()
            ? (HirdCombatStance)_nview.GetZDO().GetInt(HearthZdoKeys.HirdCombatStance)
            : HirdCombatStance.Defensive;

        internal HirdFormation Formation => _nview != null && _nview.IsValid()
            ? (HirdFormation)_nview.GetZDO().GetInt(HearthZdoKeys.HirdFormation)
            : HirdFormation.Follow;

        internal ZDOID Id => _nview != null && _nview.IsValid()
            ? _nview.GetZDO().m_uid
            : ZDOID.None;

        internal string MemberName => _character != null ? _character.m_name : "";

        internal long RecruiterId => _nview != null && _nview.IsValid()
            ? _nview.GetZDO().GetLong(SettlerRecruitable.OwnerKey)
            : 0L;

        internal bool IsDead => _character == null || _character.IsDead();

        internal float HealthFraction => _character != null ? _character.GetHealthPercentage() : 1f;

        internal bool UsesRangedWeapon
        {
            get
            {
                var equipment = GetComponent<SettlerEquipment>();
                var spec = equipment != null ? equipment.SlotSpec(0) : "";
                if (string.IsNullOrEmpty(spec) || ObjectDB.instance == null)
                {
                    return false;
                }
                var prefab = ObjectDB.instance.GetItemPrefab(spec.Split(':')[0]);
                var drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                return drop != null
                    && drop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow;
            }
        }

        internal static PartyMember FindById(ZDOID id)
        {
            foreach (var member in Instances)
            {
                if (member._nview != null && member._nview.IsValid()
                    && member._nview.GetZDO().m_uid == id)
                {
                    return member;
                }
            }
            return null;
        }

        private void Update()
        {
            if (_nview == null || !_nview.IsValid() || !_nview.IsOwner())
            {
                return;
            }
            SyncMovementMode();
            if (!IsActiveMember)
            {
                return;
            }

            // Retreat and passive behavior outrank autonomous MonsterAI target
            // acquisition. An explicit focus-fire directive may temporarily
            // override Passive until that target is gone.
            var explicitAttack = _directives != null
                && _directives.Kind == SettlerDirectiveKind.Attack
                && _ai != null && _ai.m_targetCreature != null
                && !_ai.m_targetCreature.IsDead();
            if (_ai != null && _ai.m_targetCreature != null
                && (Stance == PartyStance.Fallback
                    || (CombatStance == HirdCombatStance.Passive && !explicitAttack)))
            {
                _ai.m_targetCreature = null;
            }

            if (Time.time < _nextTick)
            {
                return;
            }
            _nextTick = Time.time + TickInterval;

            UpdateOwnerNearby();
            ApplyStanceAI();
            ApplyCombatAI();
            Regen();
            AutoFallback();
        }

        private void SyncMovementMode()
        {
            if (_ai == null)
            {
                return;
            }
            // Dvergr wandering suits a village, but causes a following Hird
            // to scatter and continually reacquire paths around its owner.
            _ai.m_randomMoveInterval = IsActiveMember ? 0f : _baseRandomMoveInterval;
            _ai.m_randomMoveRange = IsActiveMember ? 0f : _baseRandomMoveRange;
        }

        private void UpdateOwnerNearby()
        {
            var ownerId = _nview.GetZDO().GetLong(SettlerRecruitable.OwnerKey);
            OwnerNearby = false;
            if (ownerId == 0L)
            {
                return;
            }
            foreach (var player in Player.GetAllPlayers())
            {
                // A dead owner does not count: the moment you fall, your
                // party is out of the fight too.
                if (player.GetPlayerID() == ownerId && !player.IsDead()
                    && Vector3.Distance(player.transform.position, transform.position) < PartySystem.GuardDistance)
                {
                    OwnerNearby = true;
                    return;
                }
            }
        }

        private void ApplyStanceAI()
        {
            if (_ai == null)
            {
                return;
            }
            if (Stance == PartyStance.Hold && _ai.GetFollowTarget() != null)
            {
                _ai.SetFollowTarget(null);
                _ai.SetPatrolPoint();
            }
        }

        private void ApplyCombatAI()
        {
            if (_ai == null)
            {
                return;
            }

            if (_directives != null && _directives.Kind == SettlerDirectiveKind.Attack
                && (_ai.m_targetCreature == null || _ai.m_targetCreature.IsDead()))
            {
                SetStance(Stance, FindOwnerPlayer());
            }
            if (Stance == PartyStance.Fallback || CombatStance == HirdCombatStance.Passive)
            {
                if (_ai.m_targetCreature == null)
                {
                    _ai.SetAlerted(false);
                }
                return;
            }

            var owner = FindOwnerPlayer();
            if (owner == null)
            {
                return;
            }

            // Keep a live hostile target. MonsterAI handles the actual weapon,
            // movement and attack animation once we give it a creature.
            var target = _ai.m_targetCreature;
            var orderedAttack = _directives != null
                && _directives.Kind == SettlerDirectiveKind.Attack;
            if (target != null && !target.IsDead()
                && BaseAI.IsEnemy(owner, target)
                && (orderedAttack || WithinCombatLeash(target, owner)))
            {
                return;
            }
            _ai.m_targetCreature = null;

            // Retaliation has first priority. This closes the half-second scan
            // gap when a member is struck and also lets a defensive Hird finish
            // reacting to an attacker just outside its normal guard bubble.
            if (_recentAttacker != null
                && Time.time - _recentAttackerTime <= RetaliationMemorySeconds
                && !_recentAttacker.IsDead()
                && BaseAI.IsEnemy(owner, _recentAttacker)
                && Vector3.Distance(transform.position, _recentAttacker.transform.position)
                    <= RetaliationRange)
            {
                Engage(_recentAttacker);
                return;
            }

            if (CombatStance == HirdCombatStance.Aggressive)
            {
                target = PartySystem.FindNearestEnemy(owner, transform.position, AggressiveRange)
                    ?? PartySystem.FindNearestEnemy(owner, owner.transform.position, AggressiveRange);
            }
            else
            {
                // Defensive is the default stance: protect the owner and then
                // the member's immediate space, but do not go hunting broadly.
                target = PartySystem.FindNearestEnemy(
                    owner, owner.transform.position, DefensiveOwnerRange)
                    ?? PartySystem.FindNearestEnemy(owner, transform.position, DefensiveMemberRange);
            }

            if (target != null) Engage(target); else _ai.SetAlerted(false);
        }

        private bool WithinCombatLeash(Character target, Player owner)
        {
            if (target == null || owner == null)
            {
                return false;
            }
            var anchor = Stance == PartyStance.Hold && _directives != null
                ? _directives.Target
                : owner.transform.position;
            var leash = CombatStance == HirdCombatStance.Aggressive
                ? AggressiveLeash
                : DefensiveLeash;
            return Vector3.Distance(anchor, target.transform.position) <= leash;
        }

        private void Engage(Character target)
        {
            if (_ai == null || target == null || target.IsDead())
            {
                return;
            }
            _ai.m_targetCreature = target;
            _ai.Alert();
        }

        // Out of combat, members recover on their own: stakes live inside the
        // fight, not as an attrition tax dragged between fights.
        private void Regen()
        {
            var rate = ModConfig.PartyRegenPerSecond.Value;
            if (rate <= 0f || _character == null || _character.IsDead())
            {
                return;
            }
            if (Time.time - _lastDamageTime < RegenDelaySeconds)
            {
                return;
            }
            var health = _character.GetHealth();
            var max = _character.GetMaxHealth();
            if (health >= max)
            {
                return;
            }
            _character.SetHealth(Mathf.Min(max, health + rate * TickInterval));
        }

        // The telegraphed near-death behavior: a gravely wounded member stops
        // fighting and retreats to its owner, once per wounding episode so a
        // deliberate re-engage order sticks.
        private void AutoFallback()
        {
            if (!ModConfig.PartyAutoFallback.Value || _character == null)
            {
                return;
            }
            var fraction = HealthFraction;
            if (fraction > RecoveredFraction)
            {
                _autoFellBack = false;
                return;
            }
            if (fraction < GravelyWoundedFraction && !_autoFellBack && Stance != PartyStance.Fallback)
            {
                _autoFellBack = true;
                SetStance(PartyStance.Fallback, FindOwnerPlayer());
            }
        }

        private Player FindOwnerPlayer()
        {
            var ownerId = _nview.GetZDO().GetLong(SettlerRecruitable.OwnerKey);
            foreach (var player in Player.GetAllPlayers())
            {
                if (player.GetPlayerID() == ownerId)
                {
                    return player;
                }
            }
            return null;
        }

        private void OnDamaged(float damage, Character attacker)
        {
            _lastDamageTime = Time.time;
            var owner = FindOwnerPlayer();
            if (attacker == null || owner == null || !BaseAI.IsEnemy(owner, attacker))
            {
                return;
            }
            _recentAttacker = attacker;
            _recentAttackerTime = Time.time;
            if (IsActiveMember && Stance != PartyStance.Fallback
                && CombatStance != HirdCombatStance.Passive)
            {
                Engage(attacker);
            }
        }

        internal void SetStance(PartyStance stance, Player owner)
        {
            if (_nview == null || !_nview.IsValid())
            {
                return;
            }
            _nview.ClaimOwnership();
            _nview.GetZDO().Set(PartySystem.StanceKey, (int)stance);
            _directives?.ApplyLegacy(SettlerDirectiveState.FromPartyStance(stance),
                stance == PartyStance.Hold ? transform.position : Vector3.zero,
                issuerId: owner != null ? owner.GetPlayerID() : RecruiterId);
            if (_ai == null)
            {
                return;
            }
            if (stance == PartyStance.Hold)
            {
                _ai.SetFollowTarget(null);
                _ai.SetPatrolPoint();
            }
            else
            {
                if (owner != null)
                {
                    _ai.SetFollowTarget(owner.gameObject);
                }
                if (stance == PartyStance.Fallback)
                {
                    _ai.m_targetCreature = null;
                    _ai.SetAlerted(false);
                }
            }
        }

        internal void SetCombatStance(HirdCombatStance stance)
        {
            if (_nview == null || !_nview.IsValid())
            {
                return;
            }
            if (CombatStance == stance)
            {
                return;
            }
            _nview.ClaimOwnership();
            _nview.GetZDO().Set(HearthZdoKeys.HirdCombatStance, (int)stance);
            if (stance == HirdCombatStance.Passive && _ai != null)
            {
                _ai.m_targetCreature = null;
                _ai.SetAlerted(false);
            }
        }

        internal void SetFormation(HirdFormation formation)
        {
            if (_nview == null || !_nview.IsValid())
            {
                return;
            }
            if (Formation == formation)
            {
                return;
            }
            _nview.ClaimOwnership();
            _nview.GetZDO().Set(HearthZdoKeys.HirdFormation, (int)formation);
        }

        internal void FollowFormationTarget(GameObject target, Player owner)
        {
            if (_ai == null || Stance != PartyStance.Follow)
            {
                return;
            }
            if (target != null)
            {
                var distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance <= 1.6f)
                {
                    _formationArrived = true;
                    if (_ai.GetFollowTarget() == target)
                    {
                        _ai.SetFollowTarget(null);
                    }
                    return;
                }
                if (_formationArrived && distance < 2.8f)
                {
                    return;
                }
                _formationArrived = false;
            }
            else
            {
                _formationArrived = false;
            }
            var desired = target != null ? target : owner != null ? owner.gameObject : null;
            if (desired != null && _ai.GetFollowTarget() != desired)
            {
                _ai.SetFollowTarget(desired);
            }
        }

        /// <summary>Walk to a world position and hold or actively defend it.</summary>
        internal void MoveTo(Vector3 position, Player owner, bool defend)
        {
            SetStance(PartyStance.Hold, owner);
            _directives?.ApplyLegacy(
                defend ? SettlerDirectiveKind.Guard : SettlerDirectiveKind.Hold,
                position,
                issuerId: owner != null ? owner.GetPlayerID() : RecruiterId);
            if (_ai != null)
            {
                _ai.SetPatrolPoint(position);
                if (defend)
                {
                    _ai.Alert();
                }
            }
        }

        /// <summary>Hold at a rally standard instead of in place: walk there, guard there.</summary>
        internal void RallyTo(Vector3 position, Player owner)
        {
            SetStance(PartyStance.Hold, owner);
            _directives?.ApplyLegacy(SettlerDirectiveKind.Hold, position,
                issuerId: owner != null ? owner.GetPlayerID() : RecruiterId);
            if (_ai != null)
            {
                _ai.SetPatrolPoint(position);
                _ai.Alert();
            }
        }

        /// <summary>The focus-fire command: drop everything, attack this one.</summary>
        internal void OrderAttack(Character target)
        {
            if (_ai == null || target == null || target.IsDead())
            {
                return;
            }
            if (_nview != null && _nview.IsValid())
            {
                _nview.ClaimOwnership();
            }
            _ai.m_targetCreature = target;
            _ai.Alert();
            _directives?.ApplyLegacy(SettlerDirectiveKind.Attack, target.transform.position,
                target.m_name, RecruiterId);
        }

        internal void MarkMember(Player owner)
        {
            if (_nview == null || !_nview.IsValid())
            {
                return;
            }
            _nview.ClaimOwnership();
            var zdo = _nview.GetZDO();
            zdo.Set(PartySystem.PartyKey, true);
            zdo.Set(PartySystem.StanceKey, (int)PartyStance.Follow);
            zdo.Set(HearthZdoKeys.HirdCombatStance, (int)PartySystem.CombatStance);
            zdo.Set(HearthZdoKeys.HirdFormation, (int)PartySystem.Formation);
            _directives?.ApplyLegacy(SettlerDirectiveKind.Follow, Vector3.zero,
                issuerId: owner != null ? owner.GetPlayerID() : RecruiterId);
            if (_ai != null && owner != null)
            {
                _ai.SetFollowTarget(owner.gameObject);
            }
        }

        internal void ClearMember()
        {
            if (_nview == null || !_nview.IsValid())
            {
                return;
            }
            _nview.ClaimOwnership();
            var zdo = _nview.GetZDO();
            zdo.Set(PartySystem.PartyKey, false);
            zdo.Set(PartySystem.StanceKey, (int)PartyStance.Follow);
        }

        internal void WarpTo(Vector3 position)
        {
            if (_nview == null || !_nview.IsValid())
            {
                return;
            }
            _nview.ClaimOwnership();
            transform.position = position;
            var body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = position;
            }
            _nview.GetZDO().SetPosition(position);
        }

        /// <summary>
        /// Everything needed to rebuild this member from the player save:
        /// prefab, personal name, health, star level, veterancy XP, and the
        /// five equipment slots (specs contain no field separators).
        /// </summary>
        internal string SerializeStow()
        {
            var prefabName = gameObject.name.Replace("(Clone)", "");
            var hp = _character != null ? _character.GetHealth() : 0f;
            var level = _character != null ? _character.GetLevel() : 1;
            var zdo = _nview.GetZDO();
            var xp = zdo.GetInt(SettlerVeterancy.XpKey);
            var fields = new List<string>
            {
                "S", prefabName, MemberName,
                hp.ToString("F1", CultureInfo.InvariantCulture),
                level.ToString(CultureInfo.InvariantCulture),
                xp.ToString(CultureInfo.InvariantCulture),
                zdo.GetString(SettlerEquipment.SlotKeys[0]),
                zdo.GetString(SettlerEquipment.SlotKeys[1]),
                zdo.GetString(SettlerEquipment.SlotKeys[2]),
                zdo.GetString(SettlerEquipment.SlotKeys[3]),
                zdo.GetString(SettlerEquipment.SlotKeys[4]),
            };
            GetComponent<SettlerProfile>()?.AppendStowFields(fields);
            return string.Join("|", fields);
        }

        internal void DespawnStowed()
        {
            if (_nview == null || !_nview.IsValid() || ZNetScene.instance == null)
            {
                return;
            }
            _nview.ClaimOwnership();
            ZNetScene.instance.Destroy(gameObject);
        }
    }
}
