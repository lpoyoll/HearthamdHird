using System;
using System.Collections.Generic;
using System.Globalization;
using HearthAndHird.Network;
using HearthAndHird.Settlements;
using UnityEngine;
using VikingSettlements.Npcs;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace VikingSettlements.Settlements
{
    /// <summary>
    /// Placed on the buildable Hearthstone. Defines a player settlement:
    /// settlers get assigned to it, its area counts as a player base for
    /// Valheim's native raid events, and rival clans roll a nightly raid
    /// against it while its area is loaded.
    /// </summary>
    public class PlayerSettlement : MonoBehaviour, Hoverable, Interactable, TextReceiver
    {
        private const string LastRaidDayKey = "vs_lastraid";
        private const string PendingRaidKey = "vs_nextraid";
        private const string NameKey = "vs_name";
        private const string SagaKey = "vs_saga";
        private const int SagaMaxEntries = 12;
        public const string TierKey = "vs_tier";
        public const string PeaceDayKey = "vs_peaceday";
        private const int NameCharLimit = 30;
        private const string RegisterUpdateRpc = "HnH_RegisterUpdate";
        private const string RegisterRemoveRpc = "HnH_RegisterRemove";

        public static readonly List<PlayerSettlement> Instances = new List<PlayerSettlement>();

        private ZNetView _nview;
        private Piece _piece;
        private float _captiveTimer;
        private float _nextBedScan;
        private int _cachedBeds;
        private int _appliedTier;

        internal sealed class RegisterEntry
        {
            internal ZDOID Id;
            internal string Name;
            internal SettlerJob Job;
            internal Vector3 Position;
            internal bool Hungry;
            internal int Level = 1;
            internal SettlerRecruitable LoadedSettler;
        }

        /// <summary>The banner's network view, for systems that keep state on it (abductions).</summary>
        internal ZNetView View => _nview;

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
            _piece = GetComponent<Piece>();
            if (_nview != null)
            {
                _nview.Register<string>(RegisterUpdateRpc, RPC_UpdateRegister);
                _nview.Register<string>(RegisterRemoveRpc, RPC_RemoveFromRegister);
            }
            RefreshPlayerBaseArea();
        }

        private void OnEnable()
        {
            Instances.Add(this);
        }

        private void OnDisable()
        {
            Instances.Remove(this);
        }

        public static PlayerSettlement FindNearest(Vector3 position, float maxDistance)
        {
            PlayerSettlement best = null;
            var bestDistance = maxDistance;
            foreach (var settlement in Instances)
            {
                var distance = Vector3.Distance(settlement.transform.position, position);
                if (distance <= bestDistance)
                {
                    best = settlement;
                    bestDistance = distance;
                }
            }
            return best;
        }

        /// <summary>The nearest Hearthstone whose current work area contains this point.</summary>
        internal static PlayerSettlement FindContaining(Vector3 position)
        {
            PlayerSettlement best = null;
            var bestDistance = float.MaxValue;
            foreach (var settlement in Instances)
            {
                var distance = Vector3.Distance(settlement.transform.position, position);
                if (distance <= settlement.WorkRadius && distance < bestDistance)
                {
                    best = settlement;
                    bestDistance = distance;
                }
            }
            return best;
        }

        internal static PlayerSettlement FindOwnedContaining(Vector3 position, long playerId)
        {
            PlayerSettlement best = null;
            var bestDistance = float.MaxValue;
            foreach (var settlement in Instances)
            {
                var distance = Vector3.Distance(settlement.transform.position, position);
                if (settlement.OwnerId == playerId && distance <= settlement.WorkRadius
                    && distance < bestDistance)
                {
                    best = settlement;
                    bestDistance = distance;
                }
            }
            return best;
        }

        internal static float WorkRadiusAt(Vector3 settlementCenter)
        {
            var settlement = FindNearest(settlementCenter, HearthstoneProgression.MaxRadius + 1f);
            return settlement != null
                ? settlement.WorkRadius
                : ModConfig.SettlementRadius.Value;
        }

        internal static PlayerSettlement FindForSettler(SettlerRecruitable settler)
        {
            if (settler == null)
            {
                return null;
            }
            foreach (var settlement in Instances)
            {
                if (settler.BelongsTo(settlement))
                {
                    return settlement;
                }
            }
            if (settler.HasHearthstone)
            {
                return null;
            }
            // Save migration: old assigned settlers only stored a home point.
            return FindContaining(settler.Home);
        }

        /// <summary>The loaded settlers assigned to this exact Hearthstone, sorted by name.</summary>
        internal List<SettlerRecruitable> GetSettlers()
        {
            var settlers = new List<SettlerRecruitable>();
            foreach (var settler in SettlerRecruitable.Instances)
            {
                if (settler.State == SettlerState.Assigned
                    && (settler.BelongsTo(this)
                        || (!settler.HasHearthstone
                            && FindContaining(settler.Home) == this)))
                {
                    settlers.Add(settler);
                }
            }
            settlers.Sort((a, b) => string.CompareOrdinal(a.GetHoverName(), b.GetHoverName()));
            return settlers;
        }

        public int CountAssignedSettlers()
        {
            return GetRegisterEntries().Count;
        }

        internal ZDOID Id => _nview != null && _nview.IsValid()
            ? _nview.GetZDO().m_uid
            : ZDOID.None;

        internal long OwnerId
        {
            get
            {
                if (_nview == null || !_nview.IsValid())
                {
                    return 0L;
                }
                var stored = _nview.GetZDO().GetLong(HearthZdoKeys.HearthOwner);
                return stored != 0L || _piece == null ? stored : _piece.GetCreator();
            }
        }

        /// <summary>Camp (1) through Jarl's Seat (7), upgraded explicitly by biome materials.</summary>
        internal int Tier
        {
            get
            {
                if (_nview == null || !_nview.IsValid())
                {
                    return 1;
                }
                var tier = _nview.GetZDO().GetInt(HearthZdoKeys.HearthTier, -1);
                if (tier >= 1)
                {
                    return Mathf.Clamp(tier, 1, HearthstoneProgression.MaxTier);
                }
                // Legacy three-tier banners map Hamlet/Village/Town to
                // Hamlet/Village/Hold without losing their earned progress.
                var legacy = _nview.GetZDO().GetInt(TierKey, -1);
                return legacy >= 1 ? Mathf.Clamp(legacy + 2, 1, 5) : 1;
            }
        }

        internal int TierPopulationCap => HearthstoneProgression.Get(Tier).Population;

        internal float WorkRadius => HearthstoneProgression.Get(Tier).WorkRadius;

        internal int BedCapacity
        {
            get
            {
                if (Time.time >= _nextBedScan)
                {
                    _nextBedScan = Time.time + 2f;
                    _cachedBeds = CountAvailableBeds();
                }
                return _cachedBeds;
            }
        }

        /// <summary>Population is constrained by both tier ceiling and unclaimed beds.</summary>
        internal int SettlerCap => Mathf.Min(TierPopulationCap, BedCapacity);

        internal static string TierToken(int tier)
        {
            return HearthstoneProgression.Get(tier).NameToken;
        }

        /// <summary>Whether rival raids are suspended (warlord recently slain).</summary>
        internal bool InPeace(int day)
        {
            return _nview != null && _nview.IsValid()
                && _nview.GetZDO().GetInt(PeaceDayKey) > day;
        }

        internal void GrantPeace(int untilDay)
        {
            if (_nview == null || !_nview.IsValid())
            {
                return;
            }
            _nview.ClaimOwnership();
            _nview.GetZDO().Set(PeaceDayKey, untilDay);
        }

        /// <summary>
        /// Appends a line to the settlement's saga - the chronicle of raids,
        /// weddings, losses and triumphs shown in the Saga panel. Entries are
        /// "day|text" lines, oldest dropped past the cap.
        /// </summary>
        internal void RecordSaga(string text)
        {
            if (_nview == null || !_nview.IsValid() || EnvMan.instance == null
                || string.IsNullOrEmpty(text))
            {
                return;
            }
            _nview.ClaimOwnership();
            var zdo = _nview.GetZDO();
            var entries = new List<string>(
                zdo.GetString(SagaKey).Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries));
            entries.Add($"{EnvMan.instance.GetCurrentDay()}|{text.Replace('\n', ' ').Replace('|', '/')}");
            while (entries.Count > SagaMaxEntries)
            {
                entries.RemoveAt(0);
            }
            zdo.Set(SagaKey, string.Join("\n", entries));
        }

        /// <summary>The saga entries, oldest first, as (day, text) pairs.</summary>
        internal List<(int Day, string Text)> SagaEntries()
        {
            var result = new List<(int, string)>();
            if (_nview == null || !_nview.IsValid())
            {
                return result;
            }
            foreach (var line in _nview.GetZDO().GetString(SagaKey)
                .Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                var split = line.IndexOf('|');
                if (split <= 0 || !int.TryParse(line.Substring(0, split), out var day))
                {
                    continue;
                }
                result.Add((day, line.Substring(split + 1)));
            }
            return result;
        }

        /// <summary>
        /// Persistent settlement register. Live settlers refresh their entry,
        /// while unloaded settlers retain their last known job and position.
        /// </summary>
        internal List<RegisterEntry> GetRegisterEntries()
        {
            var entries = ReadRegister();
            foreach (var settler in GetSettlers())
            {
                MergeLive(entries, settler);
            }
            entries.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return entries;
        }

        internal void UpdateRegister(SettlerRecruitable settler)
        {
            if (settler == null || _nview == null || !_nview.IsValid())
            {
                return;
            }
            var settlerView = settler.GetComponent<ZNetView>();
            if (settlerView == null || !settlerView.IsValid())
            {
                return;
            }
            if (_nview.IsOwner())
            {
                UpdateRegisterOwner(settler);
                return;
            }
            _nview.InvokeRPC(_nview.GetZDO().GetOwner(), RegisterUpdateRpc,
                FormatId(settlerView.GetZDO().m_uid));
        }

        internal void RemoveFromRegister(ZDOID id)
        {
            if (id == ZDOID.None || _nview == null || !_nview.IsValid())
            {
                return;
            }
            if (_nview.IsOwner())
            {
                RemoveFromRegisterOwner(id);
                return;
            }
            _nview.InvokeRPC(_nview.GetZDO().GetOwner(), RegisterRemoveRpc, FormatId(id));
        }

        private void RPC_UpdateRegister(long sender, string rawId)
        {
            if (!_nview.IsOwner() || !TryParseId(rawId, out var id))
            {
                return;
            }
            var settler = FindLoadedSettler(id);
            if (settler == null || settler.State != SettlerState.Assigned
                || (!settler.BelongsTo(this)
                    && (settler.HasHearthstone || FindContaining(settler.Home) != this)))
            {
                return;
            }
            UpdateRegisterOwner(settler);
        }

        private void RPC_RemoveFromRegister(long sender, string rawId)
        {
            if (!_nview.IsOwner() || !TryParseId(rawId, out var id))
            {
                return;
            }
            // Never let a remote peer erase a living member that is still
            // assigned to this Hearthstone. Death/unassignment destroys or
            // changes that association before the removal request arrives.
            var settler = FindLoadedSettler(id);
            var character = settler != null ? settler.GetComponent<Character>() : null;
            var settlerView = settler != null ? settler.GetComponent<ZNetView>() : null;
            if (settler != null && settler.State == SettlerState.Assigned
                && settler.BelongsTo(this) && character != null && !character.IsDead()
                && (settlerView == null || !settlerView.IsValid()
                    || settlerView.GetZDO().GetOwner() != sender))
            {
                return;
            }
            RemoveFromRegisterOwner(id);
        }

        private void UpdateRegisterOwner(SettlerRecruitable settler)
        {
            if (!_nview.IsOwner() || settler == null)
            {
                return;
            }
            var entries = ReadRegister();
            MergeLive(entries, settler);
            WriteRegister(entries);
        }

        private void RemoveFromRegisterOwner(ZDOID id)
        {
            if (!_nview.IsOwner())
            {
                return;
            }
            var entries = ReadRegister();
            entries.RemoveAll(entry => entry.Id == id);
            WriteRegister(entries);
        }

        private static SettlerRecruitable FindLoadedSettler(ZDOID id)
        {
            foreach (var settler in SettlerRecruitable.Instances)
            {
                var view = settler != null ? settler.GetComponent<ZNetView>() : null;
                if (view != null && view.IsValid() && view.GetZDO().m_uid == id)
                {
                    return settler;
                }
            }
            return null;
        }

        private static string FormatId(ZDOID id)
        {
            return id.UserID.ToString(CultureInfo.InvariantCulture) + ":"
                + id.ID.ToString(CultureInfo.InvariantCulture);
        }

        private static bool TryParseId(string raw, out ZDOID id)
        {
            id = ZDOID.None;
            var parts = (raw ?? "").Split(':');
            if (parts.Length != 2
                || !long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var user)
                || !uint.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            {
                return false;
            }
            id = new ZDOID(user, number);
            return true;
        }

        private List<RegisterEntry> ReadRegister()
        {
            var result = new List<RegisterEntry>();
            if (_nview == null || !_nview.IsValid())
            {
                return result;
            }
            var raw = _nview.GetZDO().GetString(HearthZdoKeys.HearthRegister);
            foreach (var record in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var fields = record.Split('|');
                if (fields.Length < 9 || fields[0] != "R1")
                {
                    continue;
                }
                var id = fields[1].Split(':');
                if (id.Length != 2
                    || !long.TryParse(id[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var user)
                    || !uint.TryParse(id[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
                    || !int.TryParse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var job)
                    || !float.TryParse(fields[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
                    || !float.TryParse(fields[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var y)
                    || !float.TryParse(fields[6], NumberStyles.Float, CultureInfo.InvariantCulture, out var z)
                    || !int.TryParse(fields[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out var level))
                {
                    continue;
                }
                result.Add(new RegisterEntry
                {
                    Id = new ZDOID(user, number),
                    Name = fields[2],
                    Job = (SettlerJob)Mathf.Clamp(job, 0, SettlerRecruitable.JobCount - 1),
                    Position = new Vector3(x, y, z),
                    Hungry = fields[7] == "1",
                    Level = Mathf.Max(1, level),
                });
            }
            return result;
        }

        private void WriteRegister(List<RegisterEntry> entries)
        {
            if (_nview == null || !_nview.IsValid())
            {
                return;
            }
            _nview.ClaimOwnership();
            var records = new List<string>();
            foreach (var entry in entries)
            {
                if (entry.Id == ZDOID.None)
                {
                    continue;
                }
                records.Add(string.Join("|",
                    "R1",
                    entry.Id.UserID.ToString(CultureInfo.InvariantCulture)
                        + ":" + entry.Id.ID.ToString(CultureInfo.InvariantCulture),
                    SanitizeRegisterText(entry.Name),
                    ((int)entry.Job).ToString(CultureInfo.InvariantCulture),
                    entry.Position.x.ToString("R", CultureInfo.InvariantCulture),
                    entry.Position.y.ToString("R", CultureInfo.InvariantCulture),
                    entry.Position.z.ToString("R", CultureInfo.InvariantCulture),
                    entry.Hungry ? "1" : "0",
                    Mathf.Max(1, entry.Level).ToString(CultureInfo.InvariantCulture)));
            }
            _nview.GetZDO().Set(HearthZdoKeys.HearthRegister, string.Join(";", records));
        }

        private static void MergeLive(List<RegisterEntry> entries, SettlerRecruitable settler)
        {
            var view = settler != null ? settler.GetComponent<ZNetView>() : null;
            if (view == null || !view.IsValid())
            {
                return;
            }
            var id = view.GetZDO().m_uid;
            var entry = entries.Find(existing => existing.Id == id);
            if (entry == null)
            {
                entry = new RegisterEntry { Id = id };
                entries.Add(entry);
            }
            var character = settler.GetComponent<Character>();
            entry.Name = character != null ? character.m_name : settler.GetHoverName();
            entry.Job = settler.Job;
            entry.Position = settler.transform.position;
            entry.Hungry = settler.IsHungry;
            entry.Level = character != null ? character.GetLevel() : 1;
            entry.LoadedSettler = settler;
        }

        private static string SanitizeRegisterText(string value)
        {
            return string.IsNullOrEmpty(value)
                ? "$vs_settler"
                : value.Replace('|', '/').Replace(';', ',').Replace('\n', ' ');
        }

        private Dictionary<SettlerJob, int> CountJobs()
        {
            var jobs = new Dictionary<SettlerJob, int>();
            foreach (var settler in GetRegisterEntries())
            {
                jobs.TryGetValue(settler.Job, out var count);
                jobs[settler.Job] = count + 1;
            }
            return jobs;
        }

        /// <summary>The player-given settlement name, or the localized default.</summary>
        public string DisplayName
        {
            get
            {
                var name = _nview != null && _nview.IsValid()
                    ? _nview.GetZDO().GetString(NameKey)
                    : "";
                return string.IsNullOrEmpty(name)
                    ? Localization.instance.Localize("$hnh_hearthstone")
                    : name;
            }
        }

        public string GetText()
        {
            return _nview != null && _nview.IsValid() ? _nview.GetZDO().GetString(NameKey) : "";
        }

        public void SetText(string text)
        {
            if (_nview == null || !_nview.IsValid())
            {
                return;
            }
            var player = Player.m_localPlayer;
            if (player == null || !CanManage(player))
            {
                return;
            }
            text = text == null ? "" : text.Trim();
            if (text.Length > NameCharLimit)
            {
                text = text.Substring(0, NameCharLimit);
            }
            _nview.ClaimOwnership();
            _nview.GetZDO().Set(NameKey, text);
        }

        private void Update()
        {
            if (_nview == null || !_nview.IsValid())
            {
                return;
            }
            if (_appliedTier != Tier)
            {
                RefreshPlayerBaseArea();
            }
            if (!_nview.IsOwner())
            {
                return;
            }
            if (EnvMan.instance == null)
            {
                return;
            }

            EnsureOwner();

            // Captives don't wait for nightfall: rescue (or loss) resolves
            // within moments of the totem falling or the deadline passing.
            _captiveTimer += Time.deltaTime;
            if (_captiveTimer >= 5f)
            {
                _captiveTimer = 0f;
                Raids.Abduction.CheckCaptive(this, EnvMan.instance.GetCurrentDay());
            }

            // One tick per settlement per night while loaded: roll a rival
            // raid and a growth chance.
            if (!EnvMan.IsNight())
            {
                return;
            }
            var day = EnvMan.instance.GetCurrentDay();
            var lastTickDay = _nview.GetZDO().GetInt(LastRaidDayKey, -1);
            if (day <= lastTickDay)
            {
                return;
            }
            _nview.GetZDO().Set(LastRaidDayKey, day);

            RollRaid(day);
            TryGrow();
            Npcs.SettlerFamily.NightlyTick(this);
        }

        // With a seer in the settlement, a successful raid roll is foreseen a
        // night ahead: tonight the warning, tomorrow the war party. Without
        // one, the raid lands the night it is rolled, as ever.
        private void RollRaid(int day)
        {
            if (!ModConfig.EnableRaids.Value)
            {
                return;
            }
            var zdo = _nview.GetZDO();
            var pending = zdo.GetInt(PendingRaidKey, -1);
            if (InPeace(day))
            {
                if (pending >= 0)
                {
                    zdo.Set(PendingRaidKey, -1); // a warlord's peace unmakes omens
                }
                return;
            }
            if (pending >= 0 && pending <= day)
            {
                zdo.Set(PendingRaidKey, -1);
                Raids.RaidSpawner.SpawnRivalRaid(this);
                return;
            }
            if (pending >= 0 || Random.value >= Raids.RaidSpawner.EffectiveRaidChance())
            {
                return;
            }
            if (!HasSeer())
            {
                Raids.RaidSpawner.SpawnRivalRaid(this);
                return;
            }
            zdo.Set(PendingRaidKey, day + 1);
            var player = Player.m_localPlayer;
            if (player != null
                && Vector3.Distance(player.transform.position, transform.position) < 60f)
            {
                player.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$vs_seer_warning"));
            }
        }

        private bool HasSeer()
        {
            foreach (var settler in GetSettlers())
            {
                if (settler.gameObject.name.StartsWith(SettlerPrefabs.Seer))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// A settlement below its cap attracts a newcomer if it has a spare
        /// unclaimed bed and enough food in its chests to feed one.
        /// </summary>
        private void TryGrow()
        {
            if (!ModConfig.GrowthEnabled.Value)
            {
                return;
            }
            // Families put down roots: settlements with couples grow faster,
            // and their newcomers are sometimes children come of age.
            var couples = SettlerFamily.CountCouples(this);
            var chance = ModConfig.GrowthChancePerDay.Value * (couples > 0 ? 1.5f : 1f);
            if (Random.value >= chance)
            {
                return;
            }
            var assigned = CountAssignedSettlers();
            if (assigned >= SettlerCap)
            {
                return;
            }
            if (BedCapacity < assigned + 1)
            {
                return; // every settler notionally needs a bed, plus one spare
            }
            if (!SettlerWork.ConsumeFoodAround(transform.position, ModConfig.GrowthFoodCost.Value))
            {
                return; // not enough food to attract anyone
            }

            SpawnNewcomer(couples > 0 && Random.value < 0.5f);
        }

        private int CountAvailableBeds()
        {
            var count = 0;
            foreach (var bed in FindObjectsOfType<Bed>())
            {
                if (bed.GetOwner() == 0L
                    && Vector3.Distance(bed.transform.position, transform.position) <= WorkRadius
                    && FindContaining(bed.transform.position) == this)
                {
                    count++;
                }
            }
            return count;
        }

        internal bool CanManage(Player player)
        {
            if (player == null || _nview == null || !_nview.IsValid())
            {
                return false;
            }
            var owner = OwnerId;
            if (owner == 0L)
            {
                _nview.ClaimOwnership();
                _nview.GetZDO().Set(HearthZdoKeys.HearthOwner, player.GetPlayerID());
                return true;
            }
            return owner == player.GetPlayerID();
        }

        private void EnsureOwner()
        {
            if (_nview == null || !_nview.IsValid()
                || _nview.GetZDO().GetLong(HearthZdoKeys.HearthOwner) != 0L)
            {
                return;
            }
            var creator = _piece != null ? _piece.GetCreator() : 0L;
            if (creator != 0L)
            {
                _nview.GetZDO().Set(HearthZdoKeys.HearthOwner, creator);
            }
        }

        private void RefreshPlayerBaseArea()
        {
            var area = transform.Find("VS_PlayerBaseArea");
            var collider = area != null ? area.GetComponent<SphereCollider>() : null;
            if (collider != null)
            {
                collider.radius = WorkRadius;
            }
            _appliedTier = Tier;
        }

        private void SpawnNewcomer(bool bornHere = false)
        {
            // Seers are rare arrivals.
            var prefabName = Random.value < 0.15f ? SettlerPrefabs.Seer : SettlerPrefabs.Settler;
            var prefab = Jotunn.Managers.PrefabManager.Instance.GetPrefab(prefabName)
                         ?? Jotunn.Managers.PrefabManager.Instance.GetPrefab(SettlerPrefabs.Settler);
            if (prefab == null)
            {
                return;
            }

            var center = transform.position;
            var angle = Random.value * 360f * Mathf.Deg2Rad;
            var distance = WorkRadius + 6f;
            var position = center + new Vector3(Mathf.Sin(angle) * distance, 0f, Mathf.Cos(angle) * distance);
            if (ZoneSystem.instance != null)
            {
                position.y = ZoneSystem.instance.GetGroundHeight(position);
            }

            var toCenter = center - position;
            toCenter.y = 0f;
            var newcomer = Object.Instantiate(prefab, position, Quaternion.LookRotation(toCenter.normalized));

            var view = newcomer.GetComponent<ZNetView>();
            if (view != null && view.IsValid())
            {
                view.GetZDO().Set(SettlerRecruitable.StateKey, (int)SettlerState.Assigned);
                view.GetZDO().Set(SettlerRecruitable.JobKey, (int)SettlerJob.Villager);
                view.GetZDO().Set(SettlerRecruitable.HomeKey, center);
                view.GetZDO().Set(SettlerRecruitable.OwnerKey, OwnerId);
            }
            var recruitable = newcomer.GetComponent<SettlerRecruitable>();
            if (recruitable != null)
            {
                recruitable.BindSettlement(this);
                UpdateRegister(recruitable);
            }
            var ai = newcomer.GetComponent<MonsterAI>();
            if (ai != null)
            {
                // Patrol home, so the newcomer walks in from the edge.
                ai.SetPatrolPoint(center);
            }

            if (bornHere)
            {
                var player = Player.m_localPlayer;
                if (player != null
                    && Vector3.Distance(player.transform.position, center) < 50f)
                {
                    player.Message(MessageHud.MessageType.Center,
                        Localization.instance.Localize("$vs_child"));
                }
            }
            Jotunn.Logger.LogInfo(bornHere
                ? $"A settlement child came of age at {center}"
                : $"A newcomer joined the settlement at {center}");
        }

        public string GetHoverName()
        {
            return DisplayName;
        }

        public string GetHoverText()
        {
            var jobs = CountJobs();
            var total = CountAssignedSettlers();
            var parts = new List<string>();
            foreach (var pair in jobs)
            {
                parts.Add($"{pair.Value} {SettlerRecruitable.JobToken(pair.Key)}");
            }
            var breakdown = parts.Count > 0 ? "\n" + string.Join(", ", parts) : "";

            var hungry = 0;
            foreach (var settler in SettlerRecruitable.Instances)
            {
                if (settler.State == SettlerState.Assigned
                    && settler.IsHungry
                    && (settler.BelongsTo(this)
                        || (!settler.HasHearthstone && FindContaining(settler.Home) == this)))
                {
                    hungry++;
                }
            }
            var hungryLine = hungry > 0 ? $"\n$vs_hungry: {hungry}" : "";
            var next = HearthstoneProgression.Next(Tier);
            var upgradeLine = next != null
                ? $"\n$hnh_hearth_upgrade: {next.NameToken} — "
                    + HearthstoneProgression.UpgradeRequirements(next)
                : "\n$hnh_hearth_max";

            return Localization.instance.Localize(
                $"{DisplayName} ({TierToken(Tier)})"
                + $"\n$vs_settlers: {total}/{SettlerCap}"
                + $" — $hnh_beds: {BedCapacity}/{TierPopulationCap}"
                + $"\n$hnh_work_radius: {WorkRadius:0}m{breakdown}{hungryLine}"
                + upgradeLine
                + Raids.Abduction.HoverLine(this)
                + "\n[<color=yellow><b>$KEY_Use</b></color>] $vs_manage"
                + "\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $vs_rename");
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
            {
                return false;
            }
            var player = user as Player;
            if (player == null)
            {
                return false;
            }
            if (!CanManage(player))
            {
                player.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$hnh_hearth_not_owner"));
                return true;
            }
            if (alt)
            {
                if (TextInput.instance != null)
                {
                    TextInput.instance.RequestText(this, "$vs_rename_topic", NameCharLimit);
                }
                return true;
            }
            SettlementPanel.Toggle(this);
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            var player = user as Player;
            if (player == null || item == null)
            {
                return false;
            }
            if (!CanManage(player))
            {
                player.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$hnh_hearth_not_owner"));
                return true;
            }
            var next = HearthstoneProgression.Next(Tier);
            if (next == null)
            {
                player.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$hnh_hearth_max"));
                return true;
            }
            if (!HearthstoneProgression.MatchesUpgradeItem(next, item))
            {
                return false;
            }
            if (!HearthstoneProgression.CanPay(player, next, out var missing))
            {
                player.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize($"$hnh_hearth_need {missing}"));
                return true;
            }
            HearthstoneProgression.Pay(player, next);
            _nview.ClaimOwnership();
            _nview.GetZDO().Set(HearthZdoKeys.HearthTier, next.Tier);
            _nview.GetZDO().Set(TierKey, next.Tier);
            RefreshPlayerBaseArea();
            RecordSaga($"$vs_saga_promoted {next.NameToken}");
            player.Message(MessageHud.MessageType.Center,
                Localization.instance.Localize($"{DisplayName} $vs_promoted {next.NameToken}!"));
            return true;
        }
    }
}
