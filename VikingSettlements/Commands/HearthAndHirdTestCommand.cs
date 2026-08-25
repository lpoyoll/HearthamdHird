using Jotunn.Entities;

namespace VikingSettlements.Commands
{
    internal sealed class HearthAndHirdTestCommand : ConsoleCommand
    {
        public override string Name => "hnh_test";
        public override string Help => "Host-only test panel. Usage: hnh_test [enable|disable]";
        public override bool IsCheat => true;

        public override void Run(string[] args)
        {
            if (args.Length > 0 && args[0].ToLowerInvariant() == "enable")
            {
                if (!Development.TestAuthority.IsListenServerHost)
                {
                    Console.instance.Print("hnh_test: only the listen-server host can enable test tools");
                    return;
                }
                ModConfig.EnableTestTools.Value = true;
                Console.instance.Print("hnh_test: enabled; press F7 or run hnh_test again");
                return;
            }
            if (args.Length > 0 && args[0].ToLowerInvariant() == "disable")
            {
                if (!Development.TestAuthority.IsListenServerHost)
                {
                    Console.instance.Print("hnh_test: only the listen-server host can disable test tools");
                    return;
                }
                ModConfig.EnableTestTools.Value = false;
                Development.HearthAndHirdTestPanel.CloseForCommand();
                Console.instance.Print("hnh_test: disabled");
                return;
            }
            if (!Development.TestAuthority.IsHost)
            {
                Console.instance.Print("hnh_test: " + Development.TestAuthority.FailureReason());
                return;
            }
            Development.HearthAndHirdTestPanel.Toggle();
        }

        public override System.Collections.Generic.List<string> CommandOptionList()
        {
            return new System.Collections.Generic.List<string> { "enable", "disable" };
        }
    }
}
