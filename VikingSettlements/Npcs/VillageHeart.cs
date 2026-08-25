using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace VikingSettlements.Npcs
{
    /// <summary>
    /// The invisible, persistent center of a wild settlement, placed by the
    /// village layouts. It stores this specific village's standing toward
    /// each player (-100..+100): earned by defending villagers and donating
    /// coins, lost by attacking them. Standing scales recruit costs; a hated
    /// village refuses to deal with that player and will defend itself.
    /// Villages generated before this feature have no heart and simply behave
    /// neutrally - `spawn VS_VillageHeart` can retrofit one.
    /// </summary>
    public class VillageHeart : MonoBehaviour
    {
        public const string RepKey = "vs_rep";
        private const string HostileUntilKey = "hnh_hostile_until";
        private const float RetaliationSeconds = 120f;
        public const int MinRep = -100;
        public const int MaxRep = 100;

        /// <summary>How far from the heart a settler still belongs to the village.</summary>
        public const float VillageRadius = 48f;

        public static readonly List<VillageHeart> Instances = new List<VillageHeart>();

        private ZNetView _nview;

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
        }

        private void OnEnable()
        {
            Instances.Add(this);
        }

        private void OnDisable()
        {
            Instances.Remove(this);
        }

        public static VillageHeart FindNearest(Vector3 position, float maxDistance = VillageRadius)
        {
            VillageHeart best = null;
            var bestDistance = maxDistance;
            foreach (var heart in Instances)
            {
                var distance = Vector3.Distance(heart.transform.position, position);
                if (distance <= bestDistance)
                {
                    best = heart;
                    bestDistance = distance;
                }
            }
            return best;
        }

        public int Reputation => ReputationFor(Player.m_localPlayer);

        public int ReputationFor(Player player)
        {
            return player != null ? ReputationFor(player.GetPlayerID()) : 0;
        }

        public int ReputationFor(long playerId)
        {
            if (_nview == null || !_nview.IsValid() || playerId == 0L)
            {
                return 0;
            }
            // Existing saves stored one village-wide value. Use it as the
            // starting value until this player earns an individual record.
            return _nview.GetZDO().GetInt(ReputationKey(playerId),
                _nview.GetZDO().GetInt(RepKey));
        }

        public void AddReputation(Player player, int delta)
        {
            if (player != null)
            {
                AddReputation(player.GetPlayerID(), delta);
            }
        }

        public void AddReputation(long playerId, int delta)
        {
            if (_nview == null || !_nview.IsValid() || playerId == 0L || delta == 0)
            {
                return;
            }
            _nview.ClaimOwnership();
            var rep = Mathf.Clamp(ReputationFor(playerId) + delta, MinRep, MaxRep);
            _nview.GetZDO().Set(ReputationKey(playerId), rep);
        }

        public void MarkHostile(Player player)
        {
            if (player == null || _nview == null || !_nview.IsValid())
            {
                return;
            }
            _nview.ClaimOwnership();
            var until = DateTime.UtcNow.AddSeconds(RetaliationSeconds).Ticks;
            _nview.GetZDO().Set(HostilityKey(player.GetPlayerID()), until);
        }

        public bool IsHostileTo(Player player)
        {
            if (player == null || _nview == null || !_nview.IsValid())
            {
                return false;
            }
            return ReputationFor(player) <= -50
                || _nview.GetZDO().GetLong(HostilityKey(player.GetPlayerID())) > DateTime.UtcNow.Ticks;
        }

        internal Player FindHostilePlayer(Vector3 origin, float range)
        {
            Player nearest = null;
            var nearestDistance = range;
            foreach (var player in Player.GetAllPlayers())
            {
                if (player == null || player.IsDead() || !IsHostileTo(player))
                {
                    continue;
                }
                var distance = Vector3.Distance(origin, player.transform.position);
                if (distance < nearestDistance)
                {
                    nearest = player;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }

        internal Player FindFriendlyWitness(Vector3 origin, float range)
        {
            Player nearest = null;
            var nearestDistance = range;
            foreach (var player in Player.GetAllPlayers())
            {
                if (player == null || player.IsDead() || IsHostileTo(player))
                {
                    continue;
                }
                var distance = Vector3.Distance(origin, player.transform.position);
                if (distance < nearestDistance)
                {
                    nearest = player;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }

        private static string ReputationKey(long playerId)
        {
            return RepKey + "_" + playerId.ToString(CultureInfo.InvariantCulture);
        }

        private static string HostilityKey(long playerId)
        {
            return HostileUntilKey + "_" + playerId.ToString(CultureInfo.InvariantCulture);
        }

        // ---- Standing tiers ----

        public static string TierToken(int rep)
        {
            if (rep >= 50) return "$vs_rep_honored";
            if (rep >= 20) return "$vs_rep_friendly";
            if (rep <= -50) return "$vs_rep_hated";
            if (rep <= -20) return "$vs_rep_distrusted";
            return "$vs_rep_neutral";
        }

        /// <summary>Recruit cost scaling: honored villages join for half price, distrusted charge extra.</summary>
        public static float CostMultiplier(int rep)
        {
            if (rep >= 50) return 0.5f;
            if (rep >= 20) return 0.75f;
            if (rep <= -20) return 1.5f;
            return 1f;
        }

        /// <summary>A hated village's settlers refuse to be recruited.</summary>
        public static bool RefusesRecruits(int rep)
        {
            return rep <= -50;
        }
    }
}
