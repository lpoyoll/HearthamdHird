using System.Collections.Generic;
using UnityEngine;

namespace HearthAndHird.Settlements
{
    /// <summary>
    /// The fixed biome progression for a Hearthstone. Population is still
    /// limited by available beds; these values are only the tier ceiling.
    /// </summary>
    internal static class HearthstoneProgression
    {
        internal sealed class TierDefinition
        {
            internal int Tier;
            internal string NameToken;
            internal int Population;
            internal float WorkRadius;
            internal string UpgradeItem;
            internal int UpgradeAmount;
            internal string SupportItem;
            internal int SupportAmount;
        }

        private static readonly TierDefinition[] Definitions =
        {
            Tier(1, "$hnh_hearth_camp", 4, 35f),
            Tier(2, "$hnh_hearth_homestead", 8, 50f,
                "Bronze", 5, "FineWood", 10),
            Tier(3, "$hnh_hearth_hamlet", 14, 70f,
                "Iron", 5, "ElderBark", 10),
            Tier(4, "$hnh_hearth_village", 22, 90f,
                "Silver", 5, "WolfPelt", 10),
            Tier(5, "$hnh_hearth_hold", 32, 120f,
                "BlackMetal", 10, "LinenThread", 10),
            Tier(6, "$hnh_hearth_great_hold", 48, 150f,
                "Eitr", 10, "YggdrasilWood", 10),
            Tier(7, "$hnh_hearth_jarl_seat", 64, 200f,
                "FlametalNew", 10),
        };

        internal const int MaxTier = 7;
        internal const float MaxRadius = 200f;

        internal static TierDefinition Get(int tier)
        {
            return Definitions[Mathf.Clamp(tier, 1, MaxTier) - 1];
        }

        internal static TierDefinition Next(int tier)
        {
            return tier >= MaxTier ? null : Get(tier + 1);
        }

        internal static bool MatchesUpgradeItem(TierDefinition tier, ItemDrop.ItemData item)
        {
            if (tier == null || item == null || string.IsNullOrEmpty(tier.UpgradeItem))
            {
                return false;
            }
            if (item.m_dropPrefab != null && item.m_dropPrefab.name == tier.UpgradeItem)
            {
                return true;
            }
            return item.m_shared != null
                && item.m_shared.m_name == SharedName(tier.UpgradeItem);
        }

        internal static bool CanPay(Player player, TierDefinition tier, out string missing)
        {
            missing = "";
            if (player == null || tier == null)
            {
                return false;
            }
            var inventory = player.GetInventory();
            if (!Has(inventory, tier.UpgradeItem, tier.UpgradeAmount))
            {
                missing = RequirementText(tier.UpgradeItem, tier.UpgradeAmount);
                return false;
            }
            if (!Has(inventory, tier.SupportItem, tier.SupportAmount))
            {
                missing = RequirementText(tier.SupportItem, tier.SupportAmount);
                return false;
            }
            return true;
        }

        internal static void Pay(Player player, TierDefinition tier)
        {
            Remove(player.GetInventory(), tier.UpgradeItem, tier.UpgradeAmount);
            Remove(player.GetInventory(), tier.SupportItem, tier.SupportAmount);
        }

        internal static string UpgradeRequirements(TierDefinition tier)
        {
            if (tier == null)
            {
                return "";
            }
            var parts = new List<string>
            {
                RequirementText(tier.UpgradeItem, tier.UpgradeAmount),
            };
            if (!string.IsNullOrEmpty(tier.SupportItem) && tier.SupportAmount > 0)
            {
                parts.Add(RequirementText(tier.SupportItem, tier.SupportAmount));
            }
            return string.Join(", ", parts);
        }

        private static TierDefinition Tier(
            int tier,
            string token,
            int population,
            float radius,
            string upgradeItem = "",
            int upgradeAmount = 0,
            string supportItem = "",
            int supportAmount = 0)
        {
            return new TierDefinition
            {
                Tier = tier,
                NameToken = token,
                Population = population,
                WorkRadius = radius,
                UpgradeItem = upgradeItem,
                UpgradeAmount = upgradeAmount,
                SupportItem = supportItem,
                SupportAmount = supportAmount,
            };
        }

        private static bool Has(Inventory inventory, string prefab, int amount)
        {
            if (string.IsNullOrEmpty(prefab) || amount <= 0)
            {
                return true;
            }
            var sharedName = SharedName(prefab);
            return inventory != null && !string.IsNullOrEmpty(sharedName)
                && inventory.CountItems(sharedName) >= amount;
        }

        private static void Remove(Inventory inventory, string prefab, int amount)
        {
            if (inventory == null || string.IsNullOrEmpty(prefab) || amount <= 0)
            {
                return;
            }
            var sharedName = SharedName(prefab);
            if (!string.IsNullOrEmpty(sharedName))
            {
                inventory.RemoveItem(sharedName, amount);
            }
        }

        private static string RequirementText(string prefab, int amount)
        {
            var sharedName = SharedName(prefab);
            var localized = !string.IsNullOrEmpty(sharedName) && Localization.instance != null
                ? Localization.instance.Localize(sharedName)
                : prefab;
            return $"{localized} x{amount}";
        }

        private static string SharedName(string prefab)
        {
            if (string.IsNullOrEmpty(prefab) || ObjectDB.instance == null)
            {
                return "";
            }
            var item = ObjectDB.instance.GetItemPrefab(prefab);
            var drop = item != null ? item.GetComponent<ItemDrop>() : null;
            return drop != null && drop.m_itemData != null && drop.m_itemData.m_shared != null
                ? drop.m_itemData.m_shared.m_name
                : "";
        }
    }
}
