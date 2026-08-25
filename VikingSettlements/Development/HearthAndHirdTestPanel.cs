using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using HearthAndHird.Jobs;
using HearthAndHird.Network;
using HearthAndHird.Settlements;
using UnityEngine;
using UnityEngine.UI;
using VikingSettlements.Npcs;
using VikingSettlements.Party;
using VikingSettlements.Settlements;
using VikingSettlements.World;

namespace VikingSettlements.Development
{
    /// <summary>Host-only spawn configurator and live-unit test controls.</summary>
    internal static class HearthAndHirdTestPanel
    {
        private const float PanelWidth = 920f;
        private const float PanelHeight = 1080f;

        private static readonly string[] ObjectNames = { "Settler", "Seer", "Hearthstone" };
        private static readonly string[] StateNames = { "Wild", "Hird follower", "Assigned settler" };
        private static readonly SettlerState[] States =
            { SettlerState.Wild, SettlerState.Following, SettlerState.Assigned };
        private static readonly int[] Counts = { 1, 2, 3, 5, 10, 20 };
        private static readonly int[] Levels = { 1, 2, 3 };
        private static readonly string[] LevelNames =
            { "Level 1 (0 stars)", "Level 2 (1 star)", "Level 3 (2 stars)" };
        private static readonly string[] KitNames =
            { "Unarmed", "Bronze sword", "Iron sword", "Archer", "Plains warrior" };
        private static readonly WildSettlementTier[] VillageTiers =
        {
            WildSettlementTier.Camp, WildSettlementTier.Homestead,
            WildSettlementTier.Hamlet, WildSettlementTier.Village,
            WildSettlementTier.Hold, WildSettlementTier.GreatHold,
            WildSettlementTier.JarlsSeat,
        };
        private static readonly string[] VillageNames =
            { "Camp", "Homestead", "Hamlet", "Village", "Hold", "Great Hold", "Jarl's Seat" };
        private static readonly string[] VillagePlacementNames =
            { "Best site near me", "Near first spawn" };

        private static GameObject _panel;
        private static SettlerRecruitable _selected;
        private static Text _previewText;
        private static Text _villagePreviewText;
        private static Text _worldStatusText;
        private static Text _selectedStatusText;
        private static int _unitIndex;
        private static int _stateIndex = 1;
        private static int _countIndex;
        private static int _levelIndex;
        private static int _jobIndex;
        private static int _kitIndex = 1;
        private static int _villageIndex = 3;
        private static int _villagePlacementIndex;
        private static VillageSpawnPlan _villagePlan;
        private static string _villagePlanStatus = "Select a settlement type.";

        internal static void OnUpdate()
        {
            if (ModConfig.TestPanelHotkey == null || !ModConfig.TestPanelHotkey.Value.IsDown()
                || Console.IsVisible() || TextInput.IsVisible() || Menu.IsVisible()
                || InventoryGui.IsVisible())
            {
                return;
            }
            if (!TestAuthority.IsHost)
            {
                Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                    TestAuthority.FailureReason());
                return;
            }
            Toggle();
        }

        internal static void Toggle()
        {
            if (_panel != null) Close(); else Open();
        }

        internal static void CloseForCommand()
        {
            Close();
        }

        private static void Open()
        {
            if (!TestAuthority.IsHost || GUIManager.Instance == null
                || GUIManager.CustomGUIFront == null)
            {
                return;
            }
            Build();
            GUIManager.BlockInput(true);
        }

        private static void Close()
        {
            if (_panel != null)
            {
                UnityEngine.Object.Destroy(_panel);
                _panel = null;
                _previewText = null;
                _villagePreviewText = null;
                _worldStatusText = null;
                _selectedStatusText = null;
                GUIManager.BlockInput(false);
            }
        }

        private static void Rebuild()
        {
            Close();
            Open();
        }

        private static void Build()
        {
            _panel = GUIManager.Instance.CreateWoodpanel(
                GUIManager.CustomGUIFront.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, PanelWidth, PanelHeight);
            _panel.AddComponent<PanelBehaviour>();

            Label("HEARTH & HIRD — TEST MUSTER", 0f, -28f, 27,
                GUIManager.Instance.ValheimOrange, 840f, TextAnchor.MiddleCenter, true);
            _worldStatusText = Label(WorldStatus(), -405f, -65f, 15, Color.white, 810f,
                TextAnchor.UpperLeft, false, 48f).GetComponent<Text>();

            Section("Configure the next spawn", -120f);
            FieldLabel("OBJECT", -280f, -153f);
            FieldLabel("ALLEGIANCE", 0f, -153f);
            FieldLabel("COUNT", 280f, -153f);
            DropDown(ObjectNames, _unitIndex, -280f, -184f, value => _unitIndex = value);
            DropDown(StateNames, _stateIndex, 0f, -184f, value => _stateIndex = value);
            DropDown(Counts.Select(value => value.ToString()).ToArray(), _countIndex,
                280f, -184f, value => _countIndex = value);

            FieldLabel("LEVEL", -280f, -234f);
            FieldLabel("JOB", 0f, -234f);
            FieldLabel("EQUIPMENT", 280f, -234f);
            DropDown(LevelNames, _levelIndex, -280f, -265f, value => _levelIndex = value);
            DropDown(JobNames(), _jobIndex, 0f, -265f, value => _jobIndex = value);
            DropDown(KitNames, _kitIndex, 280f, -265f, value => _kitIndex = value);

            var preview = Label(SpawnPreview(), -405f, -314f, 17,
                new Color(0.78f, 0.95f, 0.72f), 610f, TextAnchor.MiddleLeft, true, 58f);
            _previewText = preview.GetComponent<Text>();
            Button("SPAWN", 315f, -314f, SpawnConfigured, 180f, 48f);

            Section("Spawn a village or town", -365f);
            FieldLabel("HEARTHSTONE", -320f, -393f);
            FieldLabel("SETTLEMENT", -105f, -393f);
            FieldLabel("PLACEMENT", 120f, -393f);
            Button("SPAWN HEARTHSTONE", -320f, -424f,
                () => SpawnHearthstone(Player.m_localPlayer), 185f, 38f);
            DropDown(VillageNames, _villageIndex, -105f, -424f, value =>
            {
                _villageIndex = value;
                InvalidateVillagePlan();
            }, 200f);
            DropDown(VillagePlacementNames, _villagePlacementIndex, 120f, -424f, value =>
            {
                _villagePlacementIndex = value;
                InvalidateVillagePlan();
            }, 205f);
            Button("SPAWN SETTLEMENT", 340f, -424f, SpawnSelectedVillage, 190f, 38f);
            EnsureVillagePlan();
            var villagePreview = Label(VillagePlanPreview(), -405f, -465f, 14,
                new Color(0.78f, 0.91f, 0.72f), 810f,
                TextAnchor.MiddleLeft, false, 40f);
            _villagePreviewText = villagePreview.GetComponent<Text>();

            Section("Physical work test setup", -510f);
            Button("FORESTRY MARKER", -320f, -545f,
                () => SpawnWorkPiece(SettlementPieces.ForestryMarker, "Forestry Marker", -2.5f), 150f, 38f);
            Button("TIMBER STORE", -160f, -545f,
                () => SpawnWorkPiece(SettlementPieces.TimberStore, "Timber Store", 2.5f), 150f, 38f);
            Button("3 TEST TREES", 0f, -545f, SpawnTestTrees, 150f, 38f);
            Button("LUMBERJACK", 160f, -545f,
                () => SetSelectedJob(SettlerJob.Lumberjack), 150f, 38f);
            Button("HAULER", 320f, -545f,
                () => SetSelectedJob(SettlerJob.Hauler), 150f, 38f);

            Section("Selected unit", -590f);
            _selectedStatusText = Label(SelectedStatus(), -405f, -623f, 15, Color.white, 810f,
                TextAnchor.UpperLeft, false, 64f).GetComponent<Text>();
            Button("Previous", -320f, -685f, () => SelectRelative(-1), 140f);
            Button("Nearest", -160f, -685f, SelectNearest, 140f);
            Button("Next", 0f, -685f, () => SelectRelative(1), 140f);
            Button("Teleport here", 160f, -685f, TeleportSelected, 140f);
            Button("Open gear", 320f, -685f, OpenGear, 140f);

            Button("Make wild", -320f, -730f, () => SetSelectedState(SettlerState.Wild), 140f);
            Button("Join Hird", -160f, -730f, () => SetSelectedState(SettlerState.Following), 140f);
            Button("Assign", 0f, -730f, () => SetSelectedState(SettlerState.Assigned), 140f);
            Button("Previous job", 160f, -730f, () => CycleJob(-1), 140f);
            Button("Next job", 320f, -730f, () => CycleJob(1), 140f);
            Button("Selected follow", -320f, -770f, () => OrderSelected(PartyStance.Follow), 140f);
            Button("Selected hold", -160f, -770f, () => OrderSelected(PartyStance.Hold), 140f);
            Button("Selected retreat", 0f, -770f, () => OrderSelected(PartyStance.Fallback), 140f);
            Button("Level down", 180f, -770f, () => ChangeLevel(-1), 140f);
            Button("Level up", 340f, -770f, () => ChangeLevel(1), 140f);

            Section("Whole local Hird", -817f);
            Button("All follow", -320f, -850f, () => OrderAll(PartyStance.Follow), 140f);
            Button("All hold", -160f, -850f, () => OrderAll(PartyStance.Hold), 140f);
            Button("All retreat", 0f, -850f, () => OrderAll(PartyStance.Fallback), 140f);
            Button("Formation", 160f, -850f, CycleFormation, 140f);
            Button("Combat stance", 320f, -850f, CycleCombatStance, 140f);

            Section("Cleanup and relationship testing", -895f);
            Button("DISBAND ALL HIRD", -300f, -934f, DisbandAllHird, 190f, 40f);
            Button("RESET RELATION", -95f, -934f, ResetSelectedRelationship, 190f, 40f);
            Button("DESPAWN TEST OBJECTS", 120f, -934f, DespawnTestObjects, 215f, 40f);
            Button("Close", 325f, -934f, Close, 150f, 40f);
            Label("Despawn removes loaded units, Hearthstones and settlements created by this panel. Disband releases your local Hird.",
                -405f, -985f, 14, new Color(0.78f, 0.73f, 0.63f), 810f,
                TextAnchor.MiddleCenter, false, 24f);
        }

        private static void Section(string text, float y)
        {
            Label(text, -405f, y, 19, GUIManager.Instance.ValheimOrange,
                810f, TextAnchor.MiddleLeft, true);
        }

        private static void FieldLabel(string text, float x, float y)
        {
            Label(text, x - 120f, y, 14, new Color(0.82f, 0.75f, 0.62f),
                240f, TextAnchor.MiddleLeft, true);
        }

        private static GameObject Label(string text, float x, float y, int size, Color colour,
            float width, TextAnchor alignment, bool bold, float height = 30f)
        {
            var go = GUIManager.Instance.CreateText(text, _panel.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, y),
                bold ? GUIManager.Instance.AveriaSerifBold : GUIManager.Instance.AveriaSerif,
                size, colour, true, Color.black, width, height, false);
            go.GetComponent<Text>().alignment = alignment;
            return go;
        }

        private static void Button(string text, float x, float y, Action action,
            float width = 150f, float height = 34f)
        {
            var go = GUIManager.Instance.CreateButton(text, _panel.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(x, y), width, height);
            go.GetComponent<Button>().onClick.AddListener(() => action());
        }

        private static void DropDown(string[] options, int value, float x, float y,
            Action<int> changed, float width = 240f)
        {
            var go = GUIManager.Instance.CreateDropDown(_panel.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(x, y), 16, width, 36f);
            var dropdown = go.GetComponent<Dropdown>();
            dropdown.ClearOptions();
            dropdown.AddOptions(options.ToList());
            dropdown.value = Mathf.Clamp(value, 0, options.Length - 1);
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(next =>
            {
                changed(next);
                RefreshPreview();
            });
        }

        private static string[] JobNames()
        {
            var names = new string[SettlerRecruitable.JobCount];
            for (var i = 0; i < names.Length; i++)
            {
                names[i] = Localization.instance.Localize(
                    SettlerRecruitable.JobToken((SettlerJob)i));
            }
            return names;
        }

        private static void RefreshPreview()
        {
            if (_previewText != null) _previewText.text = SpawnPreview();
            EnsureVillagePlan();
            if (_villagePreviewText != null) _villagePreviewText.text = VillagePlanPreview();
        }

        private static string SpawnPreview()
        {
            if (_unitIndex == 2)
            {
                return "Next: 1 × Hearthstone (Camp tier)\n"
                    + "Founder: you • count, level, allegiance, job and equipment ignored";
            }
            var state = States[Mathf.Clamp(_stateIndex, 0, States.Length - 1)];
            var job = state == SettlerState.Assigned
                ? JobNames()[Mathf.Clamp(_jobIndex, 0, SettlerRecruitable.JobCount - 1)]
                : "no settlement job";
            return $"Next: {Counts[_countIndex]} × {LevelNames[_levelIndex]} {ObjectNames[_unitIndex]}\n"
                + $"{StateNames[_stateIndex]} • {KitNames[_kitIndex]} • {job}";
        }

        private static string WorldStatus()
        {
            var units = Candidates();
            var test = units.Count(unit => unit.IsTestSpawned);
            var hird = units.Count(unit => unit.State == SettlerState.Following
                && unit.GetComponent<PartyMember>()?.IsActiveMember == true);
            var hearthstones = PlayerSettlement.Instances.Count(settlement => settlement.IsTestSpawned);
            var villages = VillageHeart.Instances.Count(heart => heart.IsTestGenerated);
            var forestry = ForestryZone.Instances.Count(zone => zone != null);
            var timberStores = TimberStockpile.Instances.Count(store => store != null);
            return $"Loaded controllable: {units.Count}    Test-spawned: {test}    Test Hearthstones: {hearthstones}    Test settlements: {villages}    Local Hird: {hird}    "
                + $"Work zones/stores: {forestry}/{timberStores}    Formation: {PartySystem.Formation}    Combat: {PartySystem.CombatStance}";
        }

        private static string SelectedStatus()
        {
            if (_selected == null)
            {
                return "None selected — choose Nearest/Previous/Next or spawn a unit.";
            }
            var view = _selected.GetComponent<ZNetView>();
            var owner = view != null && view.IsValid() ? view.GetZDO().GetOwner() : 0L;
            var distance = Player.m_localPlayer != null
                ? Vector3.Distance(Player.m_localPlayer.transform.position, _selected.transform.position) : 0f;
            var tag = _selected.IsTestSpawned ? "TEST" : "WORLD";
            var resident = _selected.GetComponent<VillageResident>();
            var heart = resident != null ? resident.Heart : null;
            var village = heart != null ? $" • {heart.SettlementName}" : "";
            var homeDistance = resident != null
                ? $" • {Vector3.Distance(_selected.transform.position, resident.Home):0.0}m from home"
                : "";
            var task = PhysicalTaskTelemetry.Describe(view);
            return $"{tag} • {_selected.GetHoverName()} • Level {_selected.Level} "
                + $"({_selected.Level - 1} stars) • {_selected.State}/{_selected.Job} • "
                + $"{distance:0.0}m{village}{homeDistance} • ZDO owner {owner}\nTask: {task}";
        }

        private static List<SettlerRecruitable> Candidates()
        {
            var player = Player.m_localPlayer;
            if (player == null) return new List<SettlerRecruitable>();
            var playerId = player.GetPlayerID();
            return SettlerRecruitable.Instances
                .Where(unit => unit != null && (unit.RecruiterId == 0L || unit.RecruiterId == playerId))
                .OrderBy(unit => unit.GetHoverName()).ThenBy(unit => unit.GetInstanceID())
                .ToList();
        }

        private static void SpawnConfigured()
        {
            var player = Player.m_localPlayer;
            if (!TestAuthority.IsHost || player == null) return;
            if (_unitIndex == 2)
            {
                SpawnHearthstone(player);
                return;
            }
            var state = States[_stateIndex];
            var settlement = state == SettlerState.Assigned
                ? PlayerSettlement.FindOwnedContaining(player.transform.position, player.GetPlayerID()) : null;
            if (state == SettlerState.Assigned && settlement == null)
            {
                player.Message(MessageHud.MessageType.Center,
                    "Stand inside a Hearthstone you founded before spawning assigned settlers.");
                return;
            }

            var prefabName = _unitIndex == 1 ? SettlerPrefabs.Seer : SettlerPrefabs.Settler;
            var prefab = PrefabManager.Instance.GetPrefab(prefabName);
            if (prefab == null) return;
            var count = Counts[_countIndex];
            for (var i = 0; i < count; i++)
            {
                var position = SpawnPosition(player, i, count);
                var gameObject = UnityEngine.Object.Instantiate(prefab, position,
                    Quaternion.LookRotation(-player.transform.forward, Vector3.up));
                var unit = gameObject.GetComponent<SettlerRecruitable>();
                if (unit == null) continue;
                unit.MarkTestSpawned(Levels[_levelIndex]);
                unit.ConfigureForTest(player, state, settlement);
                if (state == SettlerState.Assigned)
                {
                    unit.SetJob((SettlerJob)_jobIndex);
                }
                ApplyKit(unit.GetComponent<SettlerEquipment>(), _kitIndex);
                _selected = unit;
            }
            Rebuild();
        }

        private static void SpawnHearthstone(Player player)
        {
            if (!TestAuthority.IsHost || player == null)
            {
                return;
            }
            var prefab = PrefabManager.Instance.GetPrefab(SettlementPieces.Banner);
            if (prefab == null)
            {
                player.Message(MessageHud.MessageType.Center, "Hearthstone prefab is unavailable.");
                return;
            }
            var position = SpawnPosition(player, 0, 1);
            var gameObject = UnityEngine.Object.Instantiate(prefab, position,
                Quaternion.LookRotation(-player.transform.forward, Vector3.up));
            var settlement = gameObject.GetComponent<PlayerSettlement>();
            if (settlement == null || !settlement.ConfigureForTest(player))
            {
                if (ZNetScene.instance != null) ZNetScene.instance.Destroy(gameObject);
                else UnityEngine.Object.Destroy(gameObject);
                player.Message(MessageHud.MessageType.Center, "Could not claim the test Hearthstone.");
                return;
            }
            player.Message(MessageHud.MessageType.Center,
                "Spawned a Camp-tier Hearthstone founded by you.");
            Rebuild();
        }

        private static void SpawnWorkPiece(string prefabName, string label, float sideOffset)
        {
            var player = Player.m_localPlayer;
            if (!TestAuthority.IsHost || player == null)
            {
                return;
            }
            if (PlayerSettlement.FindOwnedContaining(player.transform.position,
                    player.GetPlayerID()) == null)
            {
                player.Message(MessageHud.MessageType.Center,
                    "Spawn a Hearthstone, then stand inside its work radius.");
                return;
            }
            var prefab = PrefabManager.Instance.GetPrefab(prefabName);
            if (prefab == null)
            {
                player.Message(MessageHud.MessageType.Center, label + " prefab is unavailable.");
                return;
            }
            var position = SpawnPosition(player, 0, 1) + player.transform.right * sideOffset;
            if (ZoneSystem.instance != null)
            {
                position.y = ZoneSystem.instance.GetGroundHeight(position);
            }
            var gameObject = UnityEngine.Object.Instantiate(prefab, position,
                Quaternion.LookRotation(-player.transform.forward, Vector3.up));
            MarkTestWorkObject(gameObject);
            player.Message(MessageHud.MessageType.Center,
                $"Spawned {label}. It is marked for F7 cleanup.");
            Rebuild();
        }

        private static void SpawnTestTrees()
        {
            var player = Player.m_localPlayer;
            if (!TestAuthority.IsHost || player == null)
            {
                return;
            }
            var prefab = new[] { "Beech1", "Beech2", "Birch1" }
                .Select(PrefabManager.Instance.GetPrefab).FirstOrDefault(value => value != null);
            if (prefab == null)
            {
                player.Message(MessageHud.MessageType.Center, "No test-tree prefab is available.");
                return;
            }
            for (var i = 0; i < 3; i++)
            {
                var position = player.transform.position
                    + player.transform.forward * (10f + i * 2f)
                    + player.transform.right * ((i - 1) * 3.2f);
                if (ZoneSystem.instance != null)
                {
                    position.y = ZoneSystem.instance.GetGroundHeight(position);
                }
                var tree = UnityEngine.Object.Instantiate(prefab, position,
                    Quaternion.Euler(0f, i * 71f, 0f));
                MarkTestWorkObject(tree);
            }
            player.Message(MessageHud.MessageType.Center,
                "Spawned 3 mature test trees. Place/enable a Forestry Marker over them.");
            Rebuild();
        }

        private static void MarkTestWorkObject(GameObject gameObject)
        {
            var view = gameObject != null ? gameObject.GetComponent<ZNetView>() : null;
            if (view != null && view.IsValid())
            {
                view.ClaimOwnership();
                view.GetZDO().Set(HearthZdoKeys.WorkPieceTestSpawned, true);
            }
        }

        private static void SetSelectedJob(SettlerJob job)
        {
            var player = Player.m_localPlayer;
            if (_selected == null || player == null)
            {
                player?.Message(MessageHud.MessageType.Center, "Select a settler first.");
                return;
            }
            if (_selected.State != SettlerState.Assigned)
            {
                var settlement = PlayerSettlement.FindOwnedContaining(player.transform.position,
                    player.GetPlayerID());
                if (settlement == null)
                {
                    player.Message(MessageHud.MessageType.Center,
                        "Stand inside your Hearthstone to assign this settler first.");
                    return;
                }
                _selected.ConfigureForTest(player, SettlerState.Assigned, settlement);
            }
            _selected.SetJob(job);
            player.Message(MessageHud.MessageType.Center,
                $"{_selected.GetHoverName()} is now a {job}.");
            Rebuild();
        }

        private static void InvalidateVillagePlan()
        {
            _villagePlan = null;
            _villagePlanStatus = "Finding a suitable site…";
        }

        private static void EnsureVillagePlan()
        {
            if (_villagePlan != null)
            {
                return;
            }
            var player = Player.m_localPlayer;
            var tier = VillageTiers[Mathf.Clamp(_villageIndex, 0, VillageTiers.Length - 1)];
            if (!TestVillageSpawner.TryPlan(player, tier, _villagePlacementIndex == 1,
                out _villagePlan, out _villagePlanStatus))
            {
                _villagePlan = null;
            }
        }

        private static string VillagePlanPreview()
        {
            EnsureVillagePlan();
            if (_villagePlan == null)
            {
                return "Site unavailable: " + _villagePlanStatus;
            }
            var distance = Player.m_localPlayer != null
                ? Vector3.Distance(Player.m_localPlayer.transform.position, _villagePlan.Origin)
                : 0f;
            return "Ready: " + _villagePlan.Summary
                + $" • {distance:0}m away. Structures are grounded by foundation after terrain preparation.";
        }

        private static void SpawnSelectedVillage()
        {
            EnsureVillagePlan();
            var player = Player.m_localPlayer;
            if (_villagePlan == null || player == null)
            {
                player?.Message(MessageHud.MessageType.Center, _villagePlanStatus);
                return;
            }
            var plan = _villagePlan;
            _villagePlan = null;
            Close();
            TestVillageSpawner.Begin(player, plan);
        }

        private static Vector3 SpawnPosition(Player player, int index, int count)
        {
            const int columns = 5;
            var row = index / columns;
            var columnCount = Mathf.Min(columns, count - row * columns);
            var column = index % columns;
            var sideways = column - (columnCount - 1) * 0.5f;
            var position = player.transform.position
                + player.transform.forward * (5f + row * 2.5f)
                + player.transform.right * (sideways * 2.2f);
            if (ZoneSystem.instance != null)
            {
                position.y = ZoneSystem.instance.GetGroundHeight(position);
            }
            return position;
        }

        private static void ApplyKit(SettlerEquipment equipment, int kit)
        {
            if (equipment == null) return;
            equipment.ClearTestItems();
            string[] items;
            switch (kit)
            {
                case 1: items = new[] { "SwordBronze", "ShieldBronzeBuckler", "HelmetBronze", "ArmorBronzeChest", "ArmorBronzeLegs" }; break;
                case 2: items = new[] { "SwordIron", "ShieldIronSquare", "HelmetIron", "ArmorIronChest", "ArmorIronLegs" }; break;
                case 3: items = new[] { "BowFineWood", "HelmetTrollLeather", "ArmorTrollLeatherChest", "ArmorTrollLeatherLegs" }; break;
                case 4: items = new[] { "SwordBlackmetal", "ShieldBlackmetal", "HelmetPadded", "ArmorPaddedCuirass", "ArmorPaddedGreaves" }; break;
                default: items = Array.Empty<string>(); break;
            }
            foreach (var item in items) equipment.SetTestItem(item);
        }

        private static void SelectNearest()
        {
            var player = Player.m_localPlayer;
            _selected = Candidates().OrderBy(unit =>
                Vector3.Distance(player.transform.position, unit.transform.position)).FirstOrDefault();
            Rebuild();
        }

        private static void SelectRelative(int direction)
        {
            var units = Candidates();
            if (units.Count == 0) _selected = null;
            else
            {
                var index = units.IndexOf(_selected);
                _selected = units[(index + direction + units.Count) % units.Count];
            }
            Rebuild();
        }

        private static void SetSelectedState(SettlerState state)
        {
            var player = Player.m_localPlayer;
            if (_selected == null || player == null) return;
            var settlement = state == SettlerState.Assigned
                ? PlayerSettlement.FindOwnedContaining(player.transform.position, player.GetPlayerID()) : null;
            if (state == SettlerState.Assigned && settlement == null)
            {
                player.Message(MessageHud.MessageType.Center, "Stand inside your Hearthstone first.");
                return;
            }
            _selected.ConfigureForTest(player, state, settlement);
            Rebuild();
        }

        private static void TeleportSelected()
        {
            if (_selected == null || Player.m_localPlayer == null) return;
            _selected.GetComponent<PartyMember>()?.WarpTo(
                Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 3f);
            Rebuild();
        }

        private static void OpenGear()
        {
            if (_selected == null) return;
            var selected = _selected;
            Close();
            SettlerGearPanel.Open(selected);
        }

        private static void CycleJob(int direction)
        {
            if (_selected == null || _selected.State != SettlerState.Assigned) return;
            var next = ((int)_selected.Job + direction + SettlerRecruitable.JobCount)
                % SettlerRecruitable.JobCount;
            _selected.SetJob((SettlerJob)next);
            Rebuild();
        }

        private static void ChangeLevel(int direction)
        {
            if (_selected == null) return;
            _selected.SetTestLevel(_selected.Level + direction);
            Rebuild();
        }

        private static void OrderAll(PartyStance stance)
        {
            PartySystem.TestCommandAll(Player.m_localPlayer, stance);
            Rebuild();
        }

        private static void OrderSelected(PartyStance stance)
        {
            var member = _selected != null ? _selected.GetComponent<PartyMember>() : null;
            if (member != null && member.IsActiveMember)
            {
                member.SetStance(stance, Player.m_localPlayer);
            }
            Rebuild();
        }

        private static void CycleFormation()
        {
            PartySystem.TestCycleFormation(Player.m_localPlayer);
            Rebuild();
        }

        private static void CycleCombatStance()
        {
            PartySystem.TestCycleCombatStance(Player.m_localPlayer);
            Rebuild();
        }

        private static void DisbandAllHird()
        {
            var count = PartySystem.TestDisbandAll(Player.m_localPlayer);
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                $"Disbanded {count} Hird member{(count == 1 ? "" : "s")}.");
            Rebuild();
        }

        private static void ResetSelectedRelationship()
        {
            var player = Player.m_localPlayer;
            if (_selected == null || player == null)
            {
                player?.Message(MessageHud.MessageType.Center,
                    "Select a wild village resident first.");
                return;
            }
            var resident = _selected.GetComponent<VillageResident>();
            resident?.Heart?.ClearTemporaryHostility(player);
            _selected.GetComponent<SettlerReputation>()?.ClearTemporaryHostility(player);
            player.Message(MessageHud.MessageType.Center,
                "Cleared temporary brawl/hostility timers for the selected resident and village.");
            Rebuild();
        }

        private static void DespawnTestObjects()
        {
            var player = Player.m_localPlayer;
            var removedVillageParts = TestVillagePart.DestroyLoaded();
            var workObjects = UnityEngine.Object.FindObjectsOfType<ZNetView>()
                .Where(view => view != null && view.IsValid()
                    && view.GetZDO().GetBool(HearthZdoKeys.WorkPieceTestSpawned))
                .Select(view => view.gameObject).ToList();
            var removedWorkObjects = 0;
            foreach (var workObject in workObjects)
            {
                var view = workObject != null ? workObject.GetComponent<ZNetView>() : null;
                if (workObject == null || view == null || !view.IsValid())
                {
                    continue;
                }
                view.ClaimOwnership();
                if (ZNetScene.instance != null) ZNetScene.instance.Destroy(workObject);
                else UnityEngine.Object.Destroy(workObject);
                removedWorkObjects++;
            }
            var units = Candidates().Where(unit => unit.IsTestSpawned)
                .Where(unit =>
                {
                    var view = unit.GetComponent<ZNetView>();
                    return view == null || !view.IsValid()
                        || string.IsNullOrEmpty(
                            view.GetZDO().GetString(HearthZdoKeys.VillageTestBatch));
                }).ToList();
            var removedUnits = 0;
            foreach (var unit in units)
            {
                if (unit.DespawnForTest(player)) removedUnits++;
            }
            var hearthstones = PlayerSettlement.Instances
                .Where(settlement => settlement.IsTestSpawned).ToList();
            var removedHearthstones = 0;
            foreach (var settlement in hearthstones)
            {
                if (settlement.DespawnForTest(player)) removedHearthstones++;
            }
            _selected = null;
            player?.Message(MessageHud.MessageType.Center,
                $"Despawned {removedUnits} test unit{(removedUnits == 1 ? "" : "s")} and "
                + $"{removedHearthstones} test Hearthstone{(removedHearthstones == 1 ? "" : "s")}; "
                + $"removed {removedVillageParts} loaded test-village object{(removedVillageParts == 1 ? "" : "s")} and "
                + $"{removedWorkObjects} physical-work object{(removedWorkObjects == 1 ? "" : "s")}.");
            Rebuild();
        }

        private sealed class PanelBehaviour : MonoBehaviour
        {
            private float _nextRefresh;

            private void Update()
            {
                if (Input.GetKeyDown(KeyCode.Escape) || !TestAuthority.IsHost) Close();
                if (Time.time < _nextRefresh)
                {
                    return;
                }
                _nextRefresh = Time.time + 0.5f;
                if (_worldStatusText != null) _worldStatusText.text = WorldStatus();
                if (_selectedStatusText != null) _selectedStatusText.text = SelectedStatus();
            }
        }
    }
}
