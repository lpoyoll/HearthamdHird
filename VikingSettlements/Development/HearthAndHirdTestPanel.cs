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
    /// <summary>
    /// Host-only integration test surface. It deliberately drives the same
    /// persisted settler, equipment and Hird components as normal gameplay.
    /// </summary>
    internal static class HearthAndHirdTestPanel
    {
        private const float PanelWidth = 780f;
        private const float PanelHeight = 710f;
        private static GameObject _panel;
        private static SettlerRecruitable _selected;

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
            if (_panel != null)
            {
                Close();
            }
            else
            {
                Open();
            }
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

            Label("HEARTH & HIRD — HOST TEST PANEL", 0f, -32f, 26,
                GUIManager.Instance.ValheimOrange, 700f, TextAnchor.MiddleCenter, true);
            Label(StatusText(), 0f, -86f, 16, Color.white, 700f,
                TextAnchor.UpperLeft, false, 82f);

            Section("Spawn networked unit", -174f);
            Button("Wild", -270f, -207f, () => Spawn(SettlerState.Wild));
            Button("Hird follower", -90f, -207f, () => Spawn(SettlerState.Following));
            Button("Assigned", 90f, -207f, () => Spawn(SettlerState.Assigned));
            Button("Spawn 5 Hird", 270f, -207f, () =>
            {
                for (var i = 0; i < 5; i++) Spawn(SettlerState.Following, false, i);
                Rebuild();
            });

            Section("Select and position", -250f);
            Button("Previous", -270f, -283f, () => SelectRelative(-1));
            Button("Nearest", -90f, -283f, SelectNearest);
            Button("Next", 90f, -283f, () => SelectRelative(1));
            Button("Teleport here", 270f, -283f, TeleportSelected);

            Section("Selected unit state", -326f);
            Button("Make wild", -270f, -359f, () => SetSelectedState(SettlerState.Wild));
            Button("Join Hird", -90f, -359f, () => SetSelectedState(SettlerState.Following));
            Button("Assign", 90f, -359f, () => SetSelectedState(SettlerState.Assigned));
            Button("Open gear", 270f, -359f, OpenGear);

            Section("Job and development loadout", -402f);
            Button("Previous job", -270f, -435f, () => CycleJob(-1));
            Button("Next job", -90f, -435f, () => CycleJob(1));
            Button("Bronze kit", 90f, -435f, () => ApplyKit("bronze"));
            Button("Iron kit", 270f, -435f, () => ApplyKit("iron"));
            Button("Archer kit", -270f, -473f, () => ApplyKit("archer"));
            Button("Plains kit", -90f, -473f, () => ApplyKit("plains"));
            Button("Clear gear", 90f, -473f, () => ApplyKit("clear"));
            Button("+1 star", 270f, -473f, AddStar);

            Section("Selected Hird order", -516f);
            Button("Follow", -180f, -549f, () => OrderSelected(PartyStance.Follow));
            Button("Hold", 0f, -549f, () => OrderSelected(PartyStance.Hold));
            Button("Retreat", 180f, -549f, () => OrderSelected(PartyStance.Fallback));

            Section("Whole local Hird", -592f);
            Button("All follow", -270f, -625f, () => OrderAll(PartyStance.Follow));
            Button("All hold", -90f, -625f, () => OrderAll(PartyStance.Hold));
            Button("All retreat", 90f, -625f, () => OrderAll(PartyStance.Fallback));
            Button("Formation", 270f, -625f, CycleFormation);

            Button("Combat stance", -90f, -665f, CycleCombatStance, 160f);
            Button("Close", 110f, -665f, Close, 160f);
        }

        private static void Section(string text, float y)
        {
            Label(text, -330f, y, 18, GUIManager.Instance.ValheimOrange,
                660f, TextAnchor.MiddleLeft, true);
        }

        private static void Label(string text, float x, float y, int size, Color colour,
            float width, TextAnchor alignment, bool bold, float height = 30f)
        {
            var go = GUIManager.Instance.CreateText(text, _panel.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, y),
                bold ? GUIManager.Instance.AveriaSerifBold : GUIManager.Instance.AveriaSerif,
                size, colour, true, Color.black, width, height, false);
            go.GetComponent<Text>().alignment = alignment;
        }

        private static void Button(string text, float x, float y, Action action, float width = 150f)
        {
            var go = GUIManager.Instance.CreateButton(text, _panel.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(x, y), width, 32f);
            go.GetComponent<Button>().onClick.AddListener(() => action());
        }

        private static string StatusText()
        {
            var player = Player.m_localPlayer;
            var loaded = Candidates().Count;
            if (_selected == null)
            {
                return $"Host authority: YES    Loaded controllable units: {loaded}\n"
                    + "Selected: none — use Nearest, Previous/Next, or spawn a unit.";
            }
            var view = _selected.GetComponent<ZNetView>();
            var member = _selected.GetComponent<PartyMember>();
            var owner = view != null && view.IsValid() ? view.GetZDO().GetOwner() : 0L;
            var distance = player != null
                ? Vector3.Distance(player.transform.position, _selected.transform.position) : 0f;
            var hird = member != null && member.IsActiveMember
                ? $"{member.Stance}/{member.CombatStance}/{member.Formation}" : "no";
            return $"Host authority: YES    Loaded controllable units: {loaded}\n"
                + $"Selected: {_selected.GetHoverName()}  {_selected.State}/{_selected.Job}  "
                + $"distance {distance:0.0}m  ZDO owner {owner}  Hird {hird}";
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
            if (units.Count == 0)
            {
                _selected = null;
            }
            else
            {
                var index = units.IndexOf(_selected);
                _selected = units[(index + direction + units.Count) % units.Count];
            }
            Rebuild();
        }

        private static void Spawn(SettlerState state, bool rebuild = true, int offset = 0)
        {
            var player = Player.m_localPlayer;
            var prefab = PrefabManager.Instance.GetPrefab(SettlerPrefabs.Settler);
            if (!TestAuthority.IsHost || player == null || prefab == null) return;
            var right = Vector3.Cross(Vector3.up, player.transform.forward).normalized;
            var position = player.transform.position + player.transform.forward * (4f + offset * 0.8f)
                + right * ((offset % 2 == 0 ? 1f : -1f) * offset * 0.6f);
            if (ZoneSystem.instance != null) position.y = ZoneSystem.instance.GetGroundHeight(position);
            var gameObject = UnityEngine.Object.Instantiate(prefab, position,
                Quaternion.LookRotation(-player.transform.forward, Vector3.up));
            var unit = gameObject.GetComponent<SettlerRecruitable>();
            var settlement = state == SettlerState.Assigned
                ? PlayerSettlement.FindOwnedContaining(player.transform.position, player.GetPlayerID()) : null;
            if (state != SettlerState.Assigned || settlement != null)
            {
                unit?.ConfigureForTest(player, state, settlement);
            }
            else
            {
                player.Message(MessageHud.MessageType.Center,
                    "Build or stand inside your Hearthstone before spawning an assigned settler.");
            }
            _selected = unit;
            if (rebuild) Rebuild();
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
            if (_selected == null) return;
            var player = Player.m_localPlayer;
            var member = _selected.GetComponent<PartyMember>();
            member?.WarpTo(player.transform.position + player.transform.forward * 3f);
            Rebuild();
        }

        private static void CycleJob(int direction)
        {
            if (_selected == null || _selected.State != SettlerState.Assigned) return;
            var next = ((int)_selected.Job + direction + SettlerRecruitable.JobCount)
                % SettlerRecruitable.JobCount;
            _selected.SetJob((SettlerJob)next);
            Rebuild();
        }

        private static void OpenGear()
        {
            if (_selected == null) return;
            var selected = _selected;
            Close();
            SettlerGearPanel.Open(selected);
        }

        private static void ApplyKit(string kit)
        {
            var equipment = _selected != null ? _selected.GetComponent<SettlerEquipment>() : null;
            if (equipment == null) return;
            equipment.ClearTestItems();
            string[] items;
            switch (kit)
            {
                case "bronze": items = new[] { "SwordBronze", "ShieldBronzeBuckler", "HelmetBronze", "ArmorBronzeChest", "ArmorBronzeLegs" }; break;
                case "iron": items = new[] { "SwordIron", "ShieldIronSquare", "HelmetIron", "ArmorIronChest", "ArmorIronLegs" }; break;
                case "archer": items = new[] { "BowFineWood", "HelmetTrollLeather", "ArmorTrollLeatherChest", "ArmorTrollLeatherLegs" }; break;
                case "plains": items = new[] { "SwordBlackmetal", "ShieldBlackmetal", "HelmetPadded", "ArmorPaddedCuirass", "ArmorPaddedGreaves" }; break;
                default: items = Array.Empty<string>(); break;
            }
            foreach (var item in items) equipment.SetTestItem(item);
            Rebuild();
        }

        private static void AddStar()
        {
            var character = _selected != null ? _selected.GetComponent<Character>() : null;
            if (character != null) character.SetLevel(Mathf.Min(3, character.GetLevel() + 1));
            Rebuild();
        }

        private static void OrderSelected(PartyStance stance)
        {
            var member = _selected != null ? _selected.GetComponent<PartyMember>() : null;
            if (member != null && member.IsActiveMember) member.SetStance(stance, Player.m_localPlayer);
            Rebuild();
        }

        private static void OrderAll(PartyStance stance)
        {
            PartySystem.TestCommandAll(Player.m_localPlayer, stance);
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

        private sealed class PanelBehaviour : MonoBehaviour
        {
            private void Update()
            {
                if (Input.GetKeyDown(KeyCode.Escape) || !TestAuthority.IsHost)
                {
                    Close();
                }
            }
        }
    }
}
