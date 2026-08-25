using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using VikingSettlements.Npcs;
using VikingSettlements.Party;
using VikingSettlements.Settlements;

namespace VikingSettlements.Development
{
    /// <summary>Host-only spawn configurator and live-unit test controls.</summary>
    internal static class HearthAndHirdTestPanel
    {
        private const float PanelWidth = 920f;
        private const float PanelHeight = 820f;

        private static readonly string[] UnitNames = { "Settler", "Seer" };
        private static readonly string[] StateNames = { "Wild", "Hird follower", "Assigned settler" };
        private static readonly SettlerState[] States =
            { SettlerState.Wild, SettlerState.Following, SettlerState.Assigned };
        private static readonly int[] Counts = { 1, 2, 3, 5, 10, 20 };
        private static readonly int[] Levels = { 1, 2, 3 };
        private static readonly string[] LevelNames =
            { "Level 1 (0 stars)", "Level 2 (1 star)", "Level 3 (2 stars)" };
        private static readonly string[] KitNames =
            { "Unarmed", "Bronze sword", "Iron sword", "Archer", "Plains warrior" };

        private static GameObject _panel;
        private static SettlerRecruitable _selected;
        private static Text _previewText;
        private static int _unitIndex;
        private static int _stateIndex = 1;
        private static int _countIndex;
        private static int _levelIndex;
        private static int _jobIndex;
        private static int _kitIndex = 1;

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
            Label(WorldStatus(), -405f, -65f, 15, Color.white, 810f,
                TextAnchor.UpperLeft, false, 48f);

            Section("Configure the next spawn", -120f);
            FieldLabel("UNIT", -280f, -153f);
            FieldLabel("ALLEGIANCE", 0f, -153f);
            FieldLabel("COUNT", 280f, -153f);
            DropDown(UnitNames, _unitIndex, -280f, -184f, value => _unitIndex = value);
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

            Section("Selected unit", -385f);
            Label(SelectedStatus(), -405f, -418f, 15, Color.white, 810f,
                TextAnchor.UpperLeft, false, 46f);
            Button("Previous", -320f, -472f, () => SelectRelative(-1), 140f);
            Button("Nearest", -160f, -472f, SelectNearest, 140f);
            Button("Next", 0f, -472f, () => SelectRelative(1), 140f);
            Button("Teleport here", 160f, -472f, TeleportSelected, 140f);
            Button("Open gear", 320f, -472f, OpenGear, 140f);

            Button("Make wild", -320f, -517f, () => SetSelectedState(SettlerState.Wild), 140f);
            Button("Join Hird", -160f, -517f, () => SetSelectedState(SettlerState.Following), 140f);
            Button("Assign", 0f, -517f, () => SetSelectedState(SettlerState.Assigned), 140f);
            Button("Previous job", 160f, -517f, () => CycleJob(-1), 140f);
            Button("Next job", 320f, -517f, () => CycleJob(1), 140f);
            Button("Selected follow", -320f, -557f, () => OrderSelected(PartyStance.Follow), 140f);
            Button("Selected hold", -160f, -557f, () => OrderSelected(PartyStance.Hold), 140f);
            Button("Selected retreat", 0f, -557f, () => OrderSelected(PartyStance.Fallback), 140f);
            Button("Level down", 180f, -557f, () => ChangeLevel(-1), 140f);
            Button("Level up", 340f, -557f, () => ChangeLevel(1), 140f);

            Section("Whole local Hird", -604f);
            Button("All follow", -320f, -637f, () => OrderAll(PartyStance.Follow), 140f);
            Button("All hold", -160f, -637f, () => OrderAll(PartyStance.Hold), 140f);
            Button("All retreat", 0f, -637f, () => OrderAll(PartyStance.Fallback), 140f);
            Button("Formation", 160f, -637f, CycleFormation, 140f);
            Button("Combat stance", 320f, -637f, CycleCombatStance, 140f);

            Section("Cleanup", -682f);
            Button("DISBAND ALL HIRD", -240f, -721f, DisbandAllHird, 220f, 40f);
            Button("DESPAWN TEST UNITS", 0f, -721f, DespawnTestUnits, 220f, 40f);
            Button("Close", 240f, -721f, Close, 180f, 40f);
            Label("Despawn removes only units created by this panel. Disband releases your entire local Hird.",
                -405f, -772f, 14, new Color(0.78f, 0.73f, 0.63f), 810f,
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
            Action<int> changed)
        {
            var go = GUIManager.Instance.CreateDropDown(_panel.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(x, y), 16, 240f, 36f);
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
        }

        private static string SpawnPreview()
        {
            var state = States[Mathf.Clamp(_stateIndex, 0, States.Length - 1)];
            var job = state == SettlerState.Assigned
                ? JobNames()[Mathf.Clamp(_jobIndex, 0, SettlerRecruitable.JobCount - 1)]
                : "no settlement job";
            return $"Next: {Counts[_countIndex]} × {LevelNames[_levelIndex]} {UnitNames[_unitIndex]}\n"
                + $"{StateNames[_stateIndex]} • {KitNames[_kitIndex]} • {job}";
        }

        private static string WorldStatus()
        {
            var units = Candidates();
            var test = units.Count(unit => unit.IsTestSpawned);
            var hird = units.Count(unit => unit.State == SettlerState.Following
                && unit.GetComponent<PartyMember>()?.IsActiveMember == true);
            return $"Loaded controllable: {units.Count}    Test-spawned: {test}    Local Hird: {hird}    "
                + $"Formation: {PartySystem.Formation}    Combat: {PartySystem.CombatStance}";
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
            return $"{tag} • {_selected.GetHoverName()} • Level {_selected.Level} "
                + $"({_selected.Level - 1} stars) • {_selected.State}/{_selected.Job} • "
                + $"{distance:0.0}m • ZDO owner {owner}";
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

        private static void DespawnTestUnits()
        {
            var player = Player.m_localPlayer;
            var units = Candidates().Where(unit => unit.IsTestSpawned).ToList();
            var removed = 0;
            foreach (var unit in units)
            {
                if (unit.DespawnForTest(player)) removed++;
            }
            _selected = null;
            player?.Message(MessageHud.MessageType.Center,
                $"Despawned {removed} loaded test unit{(removed == 1 ? "" : "s")}.");
            Rebuild();
        }

        private sealed class PanelBehaviour : MonoBehaviour
        {
            private void Update()
            {
                if (Input.GetKeyDown(KeyCode.Escape) || !TestAuthority.IsHost) Close();
            }
        }
    }
}
