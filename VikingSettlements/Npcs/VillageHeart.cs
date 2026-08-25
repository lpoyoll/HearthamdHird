using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HearthAndHird.Network;
using UnityEngine;

namespace VikingSettlements.Npcs
{
    internal enum WildSettlementTier
    {
        Camp = 0,
        Homestead = 1,
        Hamlet = 2,
        Village = 3,
        Hold = 4,
        GreatHold = 5,
        JarlsSeat = 6,
    }

    internal enum VillageAssaultResponse
    {
        PersonalBrawl = 0,
        VillageBrawl = 1,
        LethalDefense = 2,
    }

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
        private const string HostileLethalKey = "hnh_hostile_lethal";
        private const string LastAssaultKey = "hnh_last_assault";
        private const string AssaultCountKey = "hnh_assault_count";
        private const float RetaliationSeconds = 120f;
        private const float RepeatPunchWindowSeconds = 15f;
        public const int MinRep = -100;
        public const int MaxRep = 100;

        /// <summary>How far from the heart a settler still belongs to the village.</summary>
        public const float VillageRadius = 82f;

        public static readonly List<VillageHeart> Instances = new List<VillageHeart>();

        private ZNetView _nview;
        private float _nextPopulationCheck = 2f;

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

        private void Update()
        {
            if (_nview == null || !_nview.IsValid() || !_nview.IsOwner()
                || Time.time < _nextPopulationCheck)
            {
                return;
            }
            _nextPopulationCheck = Time.time + 10f;
            ConfigureResidents();
        }

        internal ZDOID Id => _nview != null && _nview.IsValid()
            ? _nview.GetZDO().m_uid
            : ZDOID.None;

        internal WildSettlementTier SettlementTier => _nview != null && _nview.IsValid()
            ? (WildSettlementTier)Mathf.Clamp(
                _nview.GetZDO().GetInt(HearthZdoKeys.VillageTier, -1), 0, 6)
            : WildSettlementTier.Camp;

        internal string SettlementName => _nview != null && _nview.IsValid()
            ? _nview.GetZDO().GetString(HearthZdoKeys.VillageName, TierDisplay(SettlementTier))
            : "Settlement";

        internal bool IsTestGenerated => _nview != null && _nview.IsValid()
            && !string.IsNullOrEmpty(
                _nview.GetZDO().GetString(HearthZdoKeys.VillageTestBatch));

        internal float ResidentSoftRadius => 18f + (int)SettlementTier * 2f;
        internal float ResidentHardRadius => FootprintForTier(SettlementTier) + 12f;

        internal static VillageHeart FindById(long user, long id)
        {
            foreach (var heart in Instances)
            {
                var heartId = heart.Id;
                if (heartId.UserID == user && (long)heartId.ID == id)
                {
                    return heart;
                }
            }
            return null;
        }

        internal static VillageHeart ForSettler(SettlerRecruitable settler)
        {
            if (settler == null)
            {
                return null;
            }
            var resident = settler.GetComponent<VillageResident>();
            return resident != null && resident.Heart != null
                ? resident.Heart
                : FindNearest(settler.transform.position);
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

        public void MarkHostile(Player player, float seconds = RetaliationSeconds,
            bool lethal = true)
        {
            if (player == null || _nview == null || !_nview.IsValid())
            {
                return;
            }
            _nview.ClaimOwnership();
            var until = DateTime.UtcNow.AddSeconds(seconds).Ticks;
            _nview.GetZDO().Set(HostilityKey(player.GetPlayerID()), until);
            _nview.GetZDO().Set(LethalKey(player.GetPlayerID()), lethal);
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

        internal bool IsLethallyHostileTo(Player player)
        {
            if (player == null || _nview == null || !_nview.IsValid())
            {
                return false;
            }
            return ReputationFor(player) <= -50
                || (IsHostileTo(player)
                    && _nview.GetZDO().GetBool(LethalKey(player.GetPlayerID())));
        }

        internal VillageAssaultResponse RegisterAssault(Player player, bool unarmed)
        {
            if (player == null || _nview == null || !_nview.IsValid())
            {
                return VillageAssaultResponse.PersonalBrawl;
            }
            _nview.ClaimOwnership();
            var playerId = player.GetPlayerID();
            var zdo = _nview.GetZDO();
            var now = DateTime.UtcNow;
            if (!unarmed)
            {
                zdo.Set(LastAssaultKeyFor(playerId), now.Ticks);
                zdo.Set(AssaultCountKeyFor(playerId), 3);
                MarkHostile(player, RetaliationSeconds, true);
                return VillageAssaultResponse.LethalDefense;
            }

            var lastTicks = zdo.GetLong(LastAssaultKeyFor(playerId));
            var repeated = lastTicks > 0L
                && new TimeSpan(now.Ticks - lastTicks).TotalSeconds <= RepeatPunchWindowSeconds;
            var count = repeated ? zdo.GetInt(AssaultCountKeyFor(playerId)) + 1 : 1;
            zdo.Set(LastAssaultKeyFor(playerId), now.Ticks);
            zdo.Set(AssaultCountKeyFor(playerId), count);
            if (count <= 1)
            {
                return VillageAssaultResponse.PersonalBrawl;
            }
            MarkHostile(player, count >= 3 ? 40f : 25f, false);
            return VillageAssaultResponse.VillageBrawl;
        }

        internal void ClearTemporaryHostility(Player player)
        {
            if (player == null || _nview == null || !_nview.IsValid())
            {
                return;
            }
            _nview.ClaimOwnership();
            var playerId = player.GetPlayerID();
            _nview.GetZDO().Set(HostilityKey(playerId), 0L);
            _nview.GetZDO().Set(LethalKey(playerId), false);
            _nview.GetZDO().Set(LastAssaultKeyFor(playerId), 0L);
            _nview.GetZDO().Set(AssaultCountKeyFor(playerId), 0);
        }

        internal void ConfigureGenerated(WildSettlementTier tier, string batch)
        {
            if (_nview == null || !_nview.IsValid())
            {
                return;
            }
            _nview.ClaimOwnership();
            var zdo = _nview.GetZDO();
            zdo.Set(HearthZdoKeys.VillageTier, (int)tier);
            zdo.Set(HearthZdoKeys.VillageName, TierDisplay(tier));
            if (!string.IsNullOrEmpty(batch))
            {
                zdo.Set(HearthZdoKeys.VillageTestBatch, batch);
            }
            _nextPopulationCheck = 0f;
        }

        internal void ConfigureResidents()
        {
            var residents = SettlerRecruitable.Instances
                .Where(settler => settler != null && settler.State == SettlerState.Wild
                    && Vector3.Distance(transform.position, settler.transform.position)
                        <= ResidentHardRadius + 8f)
                .Where(settler =>
                {
                    var member = settler.GetComponent<VillageResident>();
                    return member != null && (member.Heart == this || member.Heart == null);
                })
                .OrderBy(settler =>
                {
                    var view = settler.GetComponent<ZNetView>();
                    return view != null && view.IsValid()
                        ? view.GetZDO().m_uid.GetHashCode()
                        : settler.GetInstanceID();
                })
                .ToList();
            if (residents.Count == 0)
            {
                return;
            }

            if (_nview.GetZDO().GetInt(HearthZdoKeys.VillageTier, -1) < 0)
            {
                ConfigureGenerated(TierForPopulation(residents.Count), "");
            }
            var tier = SettlementTier;
            var leaderAssigned = false;
            var militaryIndex = 0;
            var defenderCount = DefenderCount(tier, residents.Count);
            foreach (var settler in residents)
            {
                var member = settler.GetComponent<VillageResident>();
                if (member == null)
                {
                    continue;
                }
                var isSeer = settler.gameObject.name.StartsWith(SettlerPrefabs.Seer);
                VillageResidentRole role;
                if (isSeer)
                {
                    role = VillageResidentRole.Seer;
                }
                else if (!leaderAssigned)
                {
                    role = LeaderRole(tier);
                    leaderAssigned = true;
                }
                else if (militaryIndex < defenderCount)
                {
                    role = MilitaryRole(tier, militaryIndex++);
                }
                else
                {
                    role = VillageResidentRole.Villager;
                }
                member.Bind(this, role, settler.transform.position);
                ApplyRankAndKit(settler, role, tier);
            }
        }

        private static void ApplyRankAndKit(SettlerRecruitable settler,
            VillageResidentRole role, WildSettlementTier tier)
        {
            var character = settler.GetComponent<Character>();
            var view = settler.GetComponent<ZNetView>();
            if (character == null || view == null || !view.IsValid() || !view.IsOwner())
            {
                return;
            }
            var roll = (int)((uint)view.GetZDO().m_uid.GetHashCode() % 100u);
            var level = LevelForRoll(tier, roll);
            if (role == VillageResidentRole.Jarl || role == VillageResidentRole.Housecarl)
            {
                level = Mathf.Max(level, 3);
            }
            else if (role == VillageResidentRole.Hersir || role == VillageResidentRole.Guard
                     || role == VillageResidentRole.Elder)
            {
                level = Mathf.Max(level, 2);
            }
            character.SetLevel(Mathf.Clamp(level, 1, 3));
            character.SetHealth(character.GetMaxHealth());
            settler.GetComponent<SettlerEquipment>()?.ApplyVillageKit(tier, role);
        }

        private static int LevelForRoll(WildSettlementTier tier, int roll)
        {
            switch (tier)
            {
                case WildSettlementTier.Camp:
                case WildSettlementTier.Homestead: return roll < 80 ? 1 : 2;
                case WildSettlementTier.Hamlet: return roll < 70 ? 1 : roll < 95 ? 2 : 3;
                case WildSettlementTier.Village: return roll < 55 ? 1 : roll < 90 ? 2 : 3;
                case WildSettlementTier.Hold: return roll < 40 ? 1 : roll < 80 ? 2 : 3;
                case WildSettlementTier.GreatHold:
                case WildSettlementTier.JarlsSeat: return roll < 30 ? 1 : roll < 70 ? 2 : 3;
                default: return 1;
            }
        }

        private static VillageResidentRole LeaderRole(WildSettlementTier tier)
        {
            if (tier >= WildSettlementTier.Hold) return VillageResidentRole.Jarl;
            if (tier >= WildSettlementTier.Hamlet) return VillageResidentRole.Elder;
            return VillageResidentRole.Headman;
        }

        private static VillageResidentRole MilitaryRole(WildSettlementTier tier, int index)
        {
            if (index == 0 && tier >= WildSettlementTier.Village)
            {
                return VillageResidentRole.Hersir;
            }
            return tier >= WildSettlementTier.Hold
                ? VillageResidentRole.Housecarl
                : VillageResidentRole.Guard;
        }

        private static int DefenderCount(WildSettlementTier tier, int population)
        {
            switch (tier)
            {
                case WildSettlementTier.Camp: return Mathf.Min(1, population - 1);
                case WildSettlementTier.Homestead: return Mathf.Min(2, population - 1);
                case WildSettlementTier.Hamlet: return Mathf.Min(4, population - 1);
                case WildSettlementTier.Village: return Mathf.Min(7, population - 1);
                case WildSettlementTier.Hold: return Mathf.Min(12, population - 1);
                default: return Mathf.Min(16, population - 1);
            }
        }

        internal static int PopulationForTier(WildSettlementTier tier)
        {
            switch (tier)
            {
                case WildSettlementTier.Camp: return 4;
                case WildSettlementTier.Homestead: return 8;
                case WildSettlementTier.Hamlet: return 14;
                case WildSettlementTier.Village: return 22;
                case WildSettlementTier.Hold: return 32;
                case WildSettlementTier.GreatHold: return 48;
                default: return 64;
            }
        }

        internal static float FootprintForTier(WildSettlementTier tier)
        {
            return 12f + (int)tier * 6f;
        }

        internal static string TierDisplay(WildSettlementTier tier)
        {
            switch (tier)
            {
                case WildSettlementTier.Camp: return "Camp";
                case WildSettlementTier.Homestead: return "Homestead";
                case WildSettlementTier.Hamlet: return "Hamlet";
                case WildSettlementTier.Village: return "Village";
                case WildSettlementTier.Hold: return "Hold";
                case WildSettlementTier.GreatHold: return "Great Hold";
                default: return "Jarl's Seat";
            }
        }

        private static WildSettlementTier TierForPopulation(int population)
        {
            if (population <= 4) return WildSettlementTier.Camp;
            if (population <= 8) return WildSettlementTier.Homestead;
            if (population <= 14) return WildSettlementTier.Hamlet;
            if (population <= 22) return WildSettlementTier.Village;
            if (population <= 32) return WildSettlementTier.Hold;
            if (population <= 48) return WildSettlementTier.GreatHold;
            return WildSettlementTier.JarlsSeat;
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

        private static string LethalKey(long playerId)
        {
            return HostileLethalKey + "_" + playerId.ToString(CultureInfo.InvariantCulture);
        }

        private static string LastAssaultKeyFor(long playerId)
        {
            return LastAssaultKey + "_" + playerId.ToString(CultureInfo.InvariantCulture);
        }

        private static string AssaultCountKeyFor(long playerId)
        {
            return AssaultCountKey + "_" + playerId.ToString(CultureInfo.InvariantCulture);
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
