using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using HearthAndHird.Settlements;
using UnityEngine;

namespace VikingSettlements.Settlements
{
    /// <summary>
    /// Creates the buildable Hearthstone piece. It is cloned from the
    /// ward (guard stone), stripped of its ward logic, and given the
    /// PlayerSettlement behavior plus a PlayerBase effect area so Valheim's
    /// native random events treat the settlement as a raid-able base.
    /// </summary>
    internal static class SettlementPieces
    {
        public const string Banner = "VS_SettlementBanner";
        public const string SupplyChest = "VS_BuildChest";
        public const string BuildSite = "VS_BuildSite";
        public const string RallyBanner = "VS_RallyBanner";
        public const string ForestryMarker = "HNH_ForestryMarker";
        public const string TimberStore = "HNH_TimberStore";

        private static bool _created;

        public static void CreateAll()
        {
            if (_created)
            {
                return;
            }
            _created = true;

            CreateSupplyChest();
            CreateBuildSite();
            CreateRallyBanner();
            CreateForestryMarker();
            CreateTimberStore();

            if (PrefabManager.Instance.GetPrefab("guard_stone") == null)
            {
                Jotunn.Logger.LogWarning("Could not create VS_SettlementBanner: guard_stone prefab not found");
                return;
            }

            var clone = PrefabManager.Instance.CreateClonedPrefab(Banner, "guard_stone");

            var privateArea = clone.GetComponent<PrivateArea>();
            if (privateArea != null)
            {
                Object.DestroyImmediate(privateArea);
            }

            clone.AddComponent<PlayerSettlement>();
            AddPlayerBaseArea(clone);

            var piece = new CustomPiece(clone, false, new PieceConfig
            {
                Name = "$hnh_hearthstone",
                Description = "$hnh_hearthstone_desc",
                PieceTable = "Hammer",
                Category = "Misc",
                CraftingStation = "piece_workbench",
                Requirements = new[]
                {
                    new RequirementConfig("Wood", 10, 0, true),
                    new RequirementConfig("Stone", 5, 0, true),
                    new RequirementConfig("TrophyDeer", 1, 0, true),
                },
            });
            PieceManager.Instance.AddPiece(piece);
            Jotunn.Logger.LogInfo("Created buildable piece VS_SettlementBanner");
        }

        private static void CreateSupplyChest()
        {
            if (PrefabManager.Instance.GetPrefab("piece_chest_wood") == null)
            {
                Jotunn.Logger.LogWarning("Could not create VS_BuildChest: piece_chest_wood prefab not found");
                return;
            }
            var clone = PrefabManager.Instance.CreateClonedPrefab(SupplyChest, "piece_chest_wood");
            clone.AddComponent<BuildChest>();
            var container = clone.GetComponent<Container>();
            if (container != null)
            {
                container.m_name = "$vs_buildchest";
            }
            PieceManager.Instance.AddPiece(new CustomPiece(clone, false, new PieceConfig
            {
                Name = "$vs_buildchest",
                Description = "$vs_buildchest_desc",
                PieceTable = "Hammer",
                Category = "Misc",
                CraftingStation = "piece_workbench",
                Requirements = new[]
                {
                    new RequirementConfig("Wood", 10, 0, true),
                },
            }));
            Jotunn.Logger.LogInfo("Created buildable piece VS_BuildChest");
        }

        private static void CreateForestryMarker()
        {
            if (PrefabManager.Instance.GetPrefab("piece_banner01") == null)
            {
                Jotunn.Logger.LogWarning("Could not create HNH_ForestryMarker: piece_banner01 prefab not found");
                return;
            }
            var clone = PrefabManager.Instance.CreateClonedPrefab(ForestryMarker, "piece_banner01");
            clone.AddComponent<ForestryZone>();
            PieceManager.Instance.AddPiece(new CustomPiece(clone, false, new PieceConfig
            {
                Name = "$hnh_forestry_marker",
                Description = "$hnh_forestry_marker_desc",
                PieceTable = "Hammer",
                Category = "Misc",
                CraftingStation = "piece_workbench",
                Requirements = new[]
                {
                    new RequirementConfig("Wood", 6, 0, true),
                    new RequirementConfig("Resin", 2, 0, true),
                },
            }));
            Jotunn.Logger.LogInfo("Created buildable piece HNH_ForestryMarker");
        }

        private static void CreateTimberStore()
        {
            if (PrefabManager.Instance.GetPrefab("piece_chest_wood") == null)
            {
                Jotunn.Logger.LogWarning("Could not create HNH_TimberStore: piece_chest_wood prefab not found");
                return;
            }
            var clone = PrefabManager.Instance.CreateClonedPrefab(TimberStore, "piece_chest_wood");
            clone.AddComponent<TimberStockpile>();
            var container = clone.GetComponent<Container>();
            if (container != null)
            {
                container.m_name = "$hnh_timber_store";
            }
            PieceManager.Instance.AddPiece(new CustomPiece(clone, false, new PieceConfig
            {
                Name = "$hnh_timber_store",
                Description = "$hnh_timber_store_desc",
                PieceTable = "Hammer",
                Category = "Misc",
                CraftingStation = "piece_workbench",
                Requirements = new[]
                {
                    new RequirementConfig("Wood", 10, 0, true),
                    new RequirementConfig("FineWood", 2, 0, true),
                },
            }));
            Jotunn.Logger.LogInfo("Created buildable piece HNH_TimberStore");
        }

        // The rally standard: a cheap plantable banner the war party can be
        // ordered to hold at (see Party.RallyPoint).
        private static void CreateRallyBanner()
        {
            if (PrefabManager.Instance.GetPrefab("piece_banner01") == null)
            {
                Jotunn.Logger.LogWarning("Could not create VS_RallyBanner: piece_banner01 prefab not found");
                return;
            }
            var clone = PrefabManager.Instance.CreateClonedPrefab(RallyBanner, "piece_banner01");
            clone.AddComponent<Party.RallyPoint>();
            PieceManager.Instance.AddPiece(new CustomPiece(clone, false, new PieceConfig
            {
                Name = "$vs_rally",
                Description = "$vs_rally_desc",
                PieceTable = "Hammer",
                Category = "Misc",
                Requirements = new[]
                {
                    new RequirementConfig("Wood", 6, 0, true),
                    new RequirementConfig("LeatherScraps", 2, 0, true),
                },
            }));
            Jotunn.Logger.LogInfo("Created buildable piece VS_RallyBanner");
        }

        // The construction site marker is spawned by code (via a builder's
        // talk menu), not built with the hammer, so it is a plain prefab.
        private static void CreateBuildSite()
        {
            if (PrefabManager.Instance.GetPrefab("wood_stack") == null)
            {
                Jotunn.Logger.LogWarning("Could not create VS_BuildSite: wood_stack prefab not found");
                return;
            }
            var clone = PrefabManager.Instance.CreateClonedPrefab(BuildSite, "wood_stack");
            var piece = clone.GetComponent<Piece>();
            if (piece != null)
            {
                Object.DestroyImmediate(piece);
            }
            clone.AddComponent<ConstructionSite>();
            PrefabManager.Instance.AddPrefab(new CustomPrefab(clone, false));
            Jotunn.Logger.LogInfo("Created prefab VS_BuildSite");
        }

        // Native raid events check for player-base effect areas around the
        // player; the banner provides one covering the settlement.
        private static void AddPlayerBaseArea(GameObject prefab)
        {
            var area = new GameObject("VS_PlayerBaseArea");
            area.transform.SetParent(prefab.transform, false);

            var layer = LayerMask.NameToLayer("character_trigger");
            if (layer >= 0)
            {
                area.layer = layer;
            }

            var collider = area.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = HearthstoneProgression.Get(1).WorkRadius;

            var effectArea = area.AddComponent<EffectArea>();
            effectArea.m_type = EffectArea.Type.PlayerBase;
        }
    }
}
