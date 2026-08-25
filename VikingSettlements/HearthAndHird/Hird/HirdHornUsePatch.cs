using HarmonyLib;

namespace HearthAndHird.Hird
{
    /// <summary>
    /// Makes a Hird Horn usable from either the hotbar or inventory without
    /// consuming or equipping it. Vanilla funnels both paths through this
    /// Humanoid method.
    /// </summary>
    [HarmonyPatch(
        typeof(Humanoid),
        nameof(Humanoid.UseItem),
        new[] { typeof(Inventory), typeof(ItemDrop.ItemData), typeof(bool) })]
    internal static class HirdHornUsePatch
    {
        private static bool Prefix(Humanoid __instance, ItemDrop.ItemData item)
        {
            if (!(__instance is Player player) || player != Player.m_localPlayer
                || !HirdHornItems.IsHorn(item))
            {
                return true;
            }

            VikingSettlements.Party.PartySystem.UseHorn(player);
            return false;
        }
    }
}
