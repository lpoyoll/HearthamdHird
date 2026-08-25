using System.Collections.Generic;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using VikingSettlements;

namespace HearthAndHird.Hird
{
    /// <summary>
    /// Biome-tier Hird Horns. The best horn carried by a player determines
    /// how many settlers they may recruit into their travelling hird.
    /// </summary>
    internal static class HirdHornItems
    {
        internal const string Crude = "HnH_HirdHorn_Crude";
        internal const string Bronze = "HnH_HirdHorn_Bronze";
        internal const string Iron = "HnH_HirdHorn_Iron";
        internal const string Silver = "HnH_HirdHorn_Silver";
        internal const string Blackmetal = "HnH_HirdHorn_Blackmetal";
        internal const string Eitr = "HnH_HirdHorn_Eitr";
        internal const string Flametal = "HnH_HirdHorn_Flametal";

        private sealed class HornDefinition
        {
            internal string Prefab;
            internal string NameToken;
            internal string DescriptionToken;
            internal int Capacity;
            internal string Station;
            internal int StationLevel;
            internal RequirementConfig[] Requirements;
        }

        private static readonly HornDefinition[] Definitions =
        {
            Horn(Crude, "$hnh_horn_crude", "$hnh_horn_crude_desc", 2,
                "piece_workbench", 1,
                Req("Wood", 5), Req("LeatherScraps", 2), Req("TrophyDeer", 1)),
            Horn(Bronze, "$hnh_horn_bronze", "$hnh_horn_bronze_desc", 3,
                "forge", 1,
                Req(Crude, 1), Req("Bronze", 3), Req("FineWood", 2)),
            Horn(Iron, "$hnh_horn_iron", "$hnh_horn_iron_desc", 4,
                "forge", 2,
                Req(Bronze, 1), Req("Iron", 3), Req("ElderBark", 2)),
            Horn(Silver, "$hnh_horn_silver", "$hnh_horn_silver_desc", 6,
                "forge", 3,
                Req(Iron, 1), Req("Silver", 3), Req("WolfPelt", 2)),
            Horn(Blackmetal, "$hnh_horn_blackmetal", "$hnh_horn_blackmetal_desc", 8,
                "forge", 4,
                Req(Silver, 1), Req("BlackMetal", 3), Req("LinenThread", 2)),
            Horn(Eitr, "$hnh_horn_eitr", "$hnh_horn_eitr_desc", 10,
                "blackforge", 1,
                Req(Blackmetal, 1), Req("Eitr", 3), Req("YggdrasilWood", 2)),
            Horn(Flametal, "$hnh_horn_flametal", "$hnh_horn_flametal_desc", 12,
                "blackforge", 2,
                Req(Eitr, 1), Req("FlametalNew", 3)),
        };

        private static readonly Dictionary<string, HornDefinition> ByPrefab =
            new Dictionary<string, HornDefinition>();
        private static readonly Dictionary<string, HornDefinition> ByNameToken =
            new Dictionary<string, HornDefinition>();
        private static bool _created;

        internal static void CreateAll()
        {
            if (_created)
            {
                return;
            }

            var source = FindVisualSource();
            if (source == null)
            {
                Jotunn.Logger.LogWarning(
                    "Could not create Hird Horns: no vanilla tankard, deer trophy or club prefab was found");
                return;
            }

            foreach (var definition in Definitions)
            {
                Create(definition, source);
            }
            _created = true;
        }

        internal static bool IsHorn(ItemDrop.ItemData item)
        {
            return TryGetDefinition(item, out _);
        }

        internal static int BestCapacity(Player player)
        {
            var best = 0;
            var inventory = player != null ? player.GetInventory() : null;
            if (inventory == null)
            {
                return best;
            }

            foreach (var item in inventory.GetAllItems())
            {
                if (TryGetDefinition(item, out var definition))
                {
                    best = Mathf.Max(best, definition.Capacity);
                }
            }
            return Mathf.Min(best, ModConfig.HirdMaxFollowers.Value);
        }

        internal static string BestHornName(Player player)
        {
            HornDefinition best = null;
            var inventory = player != null ? player.GetInventory() : null;
            if (inventory == null)
            {
                return "";
            }
            foreach (var item in inventory.GetAllItems())
            {
                if (TryGetDefinition(item, out var definition)
                    && (best == null || definition.Capacity > best.Capacity))
                {
                    best = definition;
                }
            }
            return best != null ? best.NameToken : "";
        }

        private static void Create(HornDefinition definition, GameObject source)
        {
            var clone = PrefabManager.Instance.CreateClonedPrefab(definition.Prefab, source);
            if (clone == null)
            {
                Jotunn.Logger.LogWarning($"Could not create Hird Horn prefab {definition.Prefab}");
                return;
            }

            var drop = clone.GetComponent<ItemDrop>();
            if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null)
            {
                Jotunn.Logger.LogWarning($"Could not create {definition.Prefab}: source has no ItemDrop data");
                return;
            }

            drop.m_itemData.m_dropPrefab = clone;
            drop.m_itemData.m_quality = 1;
            drop.m_itemData.m_stack = 1;
            drop.m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Misc;
            drop.m_itemData.m_shared.m_maxStackSize = 1;
            drop.m_itemData.m_shared.m_maxQuality = 1;
            drop.m_itemData.m_shared.m_useDurability = false;
            drop.m_itemData.m_shared.m_teleportable = true;

            var config = new ItemConfig
            {
                Name = definition.NameToken,
                Description = definition.DescriptionToken,
                CraftingStation = definition.Station,
                MinStationLevel = definition.StationLevel,
                Weight = 1f,
                StackSize = 1,
                Requirements = definition.Requirements,
            };
            ItemManager.Instance.AddItem(new CustomItem(clone, false, config));
            ByPrefab[definition.Prefab] = definition;
            ByNameToken[definition.NameToken] = definition;
            Jotunn.Logger.LogInfo(
                $"Created {definition.Prefab} with hird capacity {definition.Capacity}");
        }

        private static bool TryGetDefinition(
            ItemDrop.ItemData item,
            out HornDefinition definition)
        {
            definition = null;
            if (item == null)
            {
                return false;
            }
            if (item.m_dropPrefab != null
                && ByPrefab.TryGetValue(item.m_dropPrefab.name, out definition))
            {
                return true;
            }
            return item.m_shared != null
                && ByNameToken.TryGetValue(item.m_shared.m_name, out definition);
        }

        private static GameObject FindVisualSource()
        {
            var candidates = new[] { "TankardAnniversary", "Tankard", "TrophyDeer", "Club" };
            foreach (var candidate in candidates)
            {
                var prefab = PrefabManager.Instance.GetPrefab(candidate);
                if (prefab != null && prefab.GetComponent<ItemDrop>() != null)
                {
                    return prefab;
                }
            }
            return null;
        }

        private static HornDefinition Horn(
            string prefab,
            string name,
            string description,
            int capacity,
            string station,
            int stationLevel,
            params RequirementConfig[] requirements)
        {
            return new HornDefinition
            {
                Prefab = prefab,
                NameToken = name,
                DescriptionToken = description,
                Capacity = capacity,
                Station = station,
                StationLevel = stationLevel,
                Requirements = requirements,
            };
        }

        private static RequirementConfig Req(string item, int amount)
        {
            return new RequirementConfig(item, amount);
        }
    }
}
