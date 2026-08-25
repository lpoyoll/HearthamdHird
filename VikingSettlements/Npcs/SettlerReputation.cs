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
        private const int PlayerHitPenalty = -5;
        private const int PlayerKillPenalty = -25;
        private const int DefenseReward = 1;
        private const float PlayerHitCooldown = 5f;
        private const float DefenseCooldown = 60f;
        private const float DefenderRange = 40f;
        private const float KillAttributionWindow = 10f;
        private const float VillageDefenseRange = 58f;
        private const float DefenseScanInterval = 0.5f;
        private const string PersonalHostileUntilKey = "hnh_settler_hostile_until";
        private const float PersonalRetaliationSeconds = 120f;

        private ZNetView _nview;
        private Character _character;
        private MonsterAI _ai;
        private SettlerRecruitable _settler;
        private float _playerHitCooldown;
        private float _defenseCooldown;
        private float _lastPlayerHitTime = -1000f;
        private long _lastPlayerAttacker;
        private float _nextDefenseScan;

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
                var heart = VillageHeart.FindNearest(transform.position);
                MarkPersonalHostile(player);
                heart?.MarkHostile(player);
                if (ModConfig.ReputationEnabled.Value && _playerHitCooldown <= 0f)
                {
                    _playerHitCooldown = PlayerHitCooldown;
                    heart?.AddReputation(player, PlayerHitPenalty);
                }
                Engage(player);
                return;
            }

            // Attacked by a monster: if a player is close, they stood with us.
            if (ModConfig.ReputationEnabled.Value && _defenseCooldown <= 0f
                && Player.IsPlayerInRange(transform.position, DefenderRange))
            {
                _defenseCooldown = DefenseCooldown;
                var heart = VillageHeart.FindNearest(transform.position);
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
                VillageHeart.FindNearest(transform.position)
                    ?.AddReputation(_lastPlayerAttacker, PlayerKillPenalty);
            }
        }

        internal bool IsVillageHostileTo(Player player)
        {
            return player != null && _settler != null && _settler.State == SettlerState.Wild
                && (IsPersonalHostileTo(player)
                    || VillageHeart.FindNearest(transform.position)?.IsHostileTo(player) == true);
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

        private void MarkPersonalHostile(Player player)
        {
            if (player == null || _nview == null || !_nview.IsValid())
            {
                return;
            }
            _nview.ClaimOwnership();
            _nview.GetZDO().Set(PersonalHostilityKey(player.GetPlayerID()),
                DateTime.UtcNow.AddSeconds(PersonalRetaliationSeconds).Ticks);
        }

        private bool IsPersonalHostileTo(Player player)
        {
            return player != null && _nview != null && _nview.IsValid()
                && _nview.GetZDO().GetLong(PersonalHostilityKey(player.GetPlayerID()))
                    > DateTime.UtcNow.Ticks;
        }

        private static string PersonalHostilityKey(long playerId)
        {
            return PersonalHostileUntilKey + "_"
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
