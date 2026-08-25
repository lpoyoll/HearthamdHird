namespace VikingSettlements.Development
{
    internal static class TestAuthority
    {
        internal static bool IsListenServerHost => ZNet.instance != null
            && ZNet.instance.IsServer()
            && Player.m_localPlayer != null;

        internal static bool IsHost => ModConfig.EnableTestTools != null
            && ModConfig.EnableTestTools.Value
            && IsListenServerHost;

        internal static string FailureReason()
        {
            if (ModConfig.EnableTestTools == null || !ModConfig.EnableTestTools.Value)
            {
                return "Enable Development.EnableTestTools in the server configuration first.";
            }
            return ZNet.instance == null || !ZNet.instance.IsServer()
                ? "The test panel is host-only. Join as the listen-server host or use single-player."
                : "Enter a world before opening the test panel.";
        }
    }
}
