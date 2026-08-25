using System;
using System.Globalization;
using HarmonyLib;
using UnityEngine;

namespace VikingSettlements.Npcs
{
    /// <summary>
    /// Feeds the village reputation from what happens to a wild settler:
    /// - A player hurting them costs standing; killing them costs a lot.
    /// - A monster hurting them while a player is nearby earns standing -
    ///   the village saw you stand with them.
    /// Only wild settlers report to their village; recruited settlers left it.
    /// </summary>
    public class SettlerReputation : MonoBehaviour
    {
        private const int AccidentalPunchPenalty = -2;
        private const int PlayerHitPenalty = -5;
        private const int ArmedHitPenalty = -10;
        private const int PlayerKillPenalty = -25;
        private const int DefenseReward = 1;
        private const float PlayerHitCooldown = 5f;
        private const float DefenseCooldown = 60f;
        private const float DefenderRange = 40f;
        private const float KillAttributionWindow = 10f;
        private const float VillageDefenseRange = 58f;
        private const float DefenseScanInterval = 0.5f;
        private const string PersonalHostileUntilKey = "hnh_settler_hostile_until";
        private const string PersonalLethalKey = "hnh_settler_hostile_lethal";
        private const string HirdThreatUntilKey = "hnh_settler_hird_threat";
        private const float PersonalBrawlSeconds = 8f;
        private const float VillageBrawlSeconds = 25f;
        private const float LethalRetaliationSeconds = 120f;
        private const float UnprovokedThreatSeconds = 25f;

        private ZNetView _nview;
        private Character _character;
        private MonsterAI _ai;
        private SettlerRecruitable _settler;
        private float _playerHitCooldown;
        private float _defenseCooldown;
        private float _lastPlayerHitTime = -1000f;
        private long _lastPlayerAttacker;
        private float _nextDefenseScan;
        private long _incomingPlayerId;
        private bool _incomingPlayerUnarmed;

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
            _character = GetComponent<Character>();
            _ai = GetComponent<MonsterAI>();
            _settler = GetComponent<SettlerRecruitable>();
            if (_character != null)
            {
                _character.m_onDamaged += OnDamaged;
                _character.m_onDeath += OnDeath;
            }
        }

        private void OnDestroy()
        {
            if (_character != null)
            {
                _character.m_onDamaged -= OnDamaged;
                _character.m_onDeath -= OnDeath;
            }
        }

        private void Update()
        {
            if (_playerHitCooldown > 0f)
            {
                _playerHitCooldown -= Time.deltaTime;
            }
            if (_defenseCooldown > 0f)
            {
                _defenseCooldown -= Time.deltaTime;
            }
            if (Time.time >= _nextDefenseScan)
            {
                _nextDefenseScan = Time.time + DefenseScanInterval;
                UpdateVillageDefense();
            }
        }

        private bool Tracks()
        {
            return _nview != null && _nview.IsValid() && _nview.IsOwner()
                   && _settler != null && _settler.State == SettlerState.Wild;
        }

        private void OnDamaged(float damage, Character attacker)
        {
            if (!Tracks() || attacker == null || damage <= 0f)
            {
                return;
            }

            if (attacker.IsPlayer())
            {
                var player = attacker as Player;
                if (player == null)
                {
                    return;
                }
                _lastPlayerHitTime = Time.time;
                _lastPlayerAttacker = player.GetPlayerID();
                var heart = VillageHeart.ForSettler(_settler);
                var unarmed = _incomingPlayerId == player.GetPlayerID()
                    ? _incomingPlayerUnarmed
                    : IsPlayerUnarmed(player);
                _incomingPlayerId = 0L;
                var response = heart != null
                    ? heart.RegisterAssault(player, unarmed)
                    : (unarmed ? VillageAssaultResponse.PersonalBrawl
                        : VillageAssaultResponse.LethalDefense);
                var lethal = response == VillageAssaultResponse.LethalDefense;
                var seconds = lethal ? LethalRetaliationSeconds
                    : response == VillageAssaultResponse.VillageBrawl
                        ? VillageBrawlSeconds
                        : PersonalBrawlSeconds;
                MarkPersonalHostile(player, seconds, lethal);
                if (ModConfig.ReputationEnabled.Value && _playerHitCooldown <= 0f)
                {
                    _playerHitCooldown = PlayerHitCooldown;
                    var penalty = !unarmed ? ArmedHitPenalty
                        : response == VillageAssaultResponse.PersonalBrawl
                            ? AccidentalPunchPenalty
                            : PlayerHitPenalty;
                    heart?.AddReputation(player, penalty);
                }
                Engage(player);
                return;
            }

            // Attacked by a monster: if a player is close, they stood with us.
            if (ModConfig.ReputationEnabled.Value && _defenseCooldown <= 0f
                && Player.IsPlayerInRange(transform.position, DefenderRange))
            {
                _defenseCooldown = DefenseCooldown;
                var heart = VillageHeart.ForSettler(_settler);
                var witness = heart?.FindFriendlyWitness(transform.position, DefenderRange);
                if (witness != null)
                {
                    heart.AddReputation(witness, DefenseReward);
                }
            }
        }

        private void OnDeath()
        {
            if (!Tracks() || !ModConfig.ReputationEnabled.Value)
            {
                return;
            }
            if (Time.time - _lastPlayerHitTime <= KillAttributionWindow)
            {
                VillageHeart.ForSettler(_settler)
                    ?.AddReputation(_lastPlayerAttacker, PlayerKillPenalty);
            }
        }

        internal bool IsVillageHostileTo(Player player)
        {
            return player != null && _settler != null && _settler.State == SettlerState.Wild
                && (IsPersonalHostileTo(player)
                    || VillageHeart.ForSettler(_settler)?.IsHostileTo(player) == true);
        }

        internal bool ShouldHirdDefend(Player player)
        {
            if (player == null || _settler == null || _settler.State != SettlerState.Wild)
            {
                return false;
            }
            if (_nview != null && _nview.IsValid()
                && _nview.GetZDO().GetLong(HirdThreatKey(player.GetPlayerID()))
                    > DateTime.UtcNow.Ticks)
            {
                return true;
            }
            if (IsPersonalHostileTo(player) && IsPersonalLethalTo(player))
            {
                return true;
            }
            return VillageHeart.ForSettler(_settler)?.IsLethallyHostileTo(player) == true;
        }

        internal bool IsMinorBrawlWith(Player player)
        {
            var heart = VillageHeart.ForSettler(_settler);
            if (heart?.IsLethallyHostileTo(player) == true)
            {
                return false;
            }
            return (IsPersonalHostileTo(player) && !IsPersonalLethalTo(player))
                || heart?.IsHostileTo(player) == true;
        }

        internal void ClearTemporaryHostility(Player player)
        {
            if (player == null || _nview == null || !_nview.IsValid())
            {
                return;
            }
            _nview.ClaimOwnership();
            var playerId = player.GetPlayerID();
            _nview.GetZDO().Set(PersonalHostilityKey(playerId), 0L);
            _nview.GetZDO().Set(PersonalLethalKeyFor(playerId), false);
            _nview.GetZDO().Set(HirdThreatKey(playerId), 0L);
            if (_ai != null && _ai.m_targetCreature == player)
            {
                _ai.m_targetCreature = null;
                _ai.SetAlerted(false);
            }
        }

        private void UpdateVillageDefense()
        {
            if (!Tracks() || _ai == null)
            {
                return;
            }
            var hostile = FindHostilePlayer();
            if (hostile != null)
            {
                var resident = GetComponent<VillageResident>();
                var personal = IsPersonalHostileTo(hostile);
                if (!personal && resident != null && !resident.IsDefender)
                {
                    GetComponent<SettlerHome>()?.ReturnFromThreat();
                    return;
                }
                Engage(hostile);
                return;
            }
            var currentPlayer = _ai.m_targetCreature as Player;
            if (currentPlayer != null && !IsVillageHostileTo(currentPlayer))
            {
                _ai.m_targetCreature = null;
                _ai.SetAlerted(false);
            }
        }

        private void Engage(Player player)
        {
            if (_ai == null || player == null || player.IsDead())
            {
                return;
            }
            _ai.m_targetCreature = player;
            _ai.Alert();
        }

        private Player FindHostilePlayer()
        {
            Player nearest = null;
            var nearestDistance = VillageDefenseRange;
            foreach (var player in Player.GetAllPlayers())
            {
                if (player == null || player.IsDead() || !IsVillageHostileTo(player))
                {
                    continue;
                }
                var distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < nearestDistance)
                {
                    nearest = player;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }

        private void MarkPersonalHostile(Player player, float seconds, bool lethal)
        {
            if (player == null || _nview == null || !_nview.IsValid())
            {
                return;
            }
            _nview.ClaimOwnership();
            _nview.GetZDO().Set(PersonalHostilityKey(player.GetPlayerID()),
                DateTime.UtcNow.AddSeconds(seconds).Ticks);
            _nview.GetZDO().Set(PersonalLethalKeyFor(player.GetPlayerID()), lethal);
        }

        private bool IsPersonalHostileTo(Player player)
        {
            return player != null && _nview != null && _nview.IsValid()
                && _nview.GetZDO().GetLong(PersonalHostilityKey(player.GetPlayerID()))
                    > DateTime.UtcNow.Ticks;
        }

        private bool IsPersonalLethalTo(Player player)
        {
            return player != null && _nview != null && _nview.IsValid()
                && _nview.GetZDO().GetBool(PersonalLethalKeyFor(player.GetPlayerID()));
        }

        private void RecordPlayerHit(Player player, bool unarmed)
        {
            _incomingPlayerId = player != null ? player.GetPlayerID() : 0L;
            _incomingPlayerUnarmed = unarmed;
        }

        private void RecordUnprovokedThreat(Player player)
        {
            if (player == null || _nview == null || !_nview.IsValid()
                || IsMinorBrawlWith(player))
            {
                return;
            }
            _nview.ClaimOwnership();
            _nview.GetZDO().Set(HirdThreatKey(player.GetPlayerID()),
                DateTime.UtcNow.AddSeconds(UnprovokedThreatSeconds).Ticks);
        }

        internal static void RecordDamageContext(Character target, HitData hit)
        {
            if (target == null || hit == null)
            {
                return;
            }
            var attacker = hit.GetAttacker();
            var victimReputation = target.GetComponent<SettlerReputation>();
            if (attacker is Player player && victimReputation != null)
            {
                victimReputation.RecordPlayerHit(player, IsPlayerUnarmed(player));
            }

            var attackerReputation = attacker != null
                ? attacker.GetComponent<SettlerReputation>()
                : null;
            if (attackerReputation == null)
            {
                return;
            }
            var threatenedPlayer = target as Player;
            if (threatenedPlayer == null)
            {
                var targetSettler = target.GetComponent<SettlerRecruitable>();
                if (targetSettler != null && targetSettler.State == SettlerState.Following)
                {
                    threatenedPlayer = FindPlayer(targetSettler.RecruiterId);
                }
            }
            if (threatenedPlayer != null)
            {
                attackerReputation.RecordUnprovokedThreat(threatenedPlayer);
            }
        }

        private static bool IsPlayerUnarmed(Player player)
        {
            var weapon = player != null ? player.GetCurrentWeapon() : null;
            return weapon == null || weapon.m_shared.m_skillType == Skills.SkillType.Unarmed;
        }

        private static Player FindPlayer(long playerId)
        {
            foreach (var player in Player.GetAllPlayers())
            {
                if (player != null && player.GetPlayerID() == playerId)
                {
                    return player;
                }
            }
            return null;
        }

        private static string PersonalHostilityKey(long playerId)
        {
            return PersonalHostileUntilKey + "_"
                + playerId.ToString(CultureInfo.InvariantCulture);
        }

        private static string PersonalLethalKeyFor(long playerId)
        {
            return PersonalLethalKey + "_"
                + playerId.ToString(CultureInfo.InvariantCulture);
        }

        private static string HirdThreatKey(long playerId)
        {
            return HirdThreatUntilKey + "_"
                + playerId.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Valheim factions are global, but village relationships are not. These
    /// narrow overrides make only a hostile village/player pair enemies while
    /// every other village can remain neutral or friendly to that player.
    /// </summary>
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.IsEnemy), new[] { typeof(Character), typeof(Character) })]
    internal static class VillageRelationshipStaticEnemyPatch
    {
        private static void Postfix(Character __0, Character __1, ref bool __result)
        {
            if (!__result && VillageRelationshipEnemy.IsHostilePair(__0, __1))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.IsEnemy), new[] { typeof(Character) })]
    internal static class VillageRelationshipInstanceEnemyPatch
    {
        private static void Postfix(BaseAI __instance, Character __0, ref bool __result)
        {
            if (__result || __instance == null)
            {
                return;
            }
            var self = __instance.GetComponent<Character>();
            if (VillageRelationshipEnemy.IsHostilePair(self, __0))
            {
                __result = true;
            }
        }
    }

    internal static class VillageRelationshipEnemy
    {
        internal static bool IsHostilePair(Character first, Character second)
        {
            return IsHostile(first, second) || IsHostile(second, first);
        }

        private static bool IsHostile(Character settlerCharacter, Character other)
        {
            var player = other as Player;
            var reputation = settlerCharacter != null
                ? settlerCharacter.GetComponent<SettlerReputation>()
                : null;
            return player != null && reputation != null
                && reputation.IsVillageHostileTo(player);
        }
    }
}
