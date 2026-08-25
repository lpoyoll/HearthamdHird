using HearthAndHird.Network;
using UnityEngine;
using VikingSettlements;

namespace HearthAndHird.Jobs
{
    internal static class PhysicalJobBootstrap
    {
        private static bool _registered;

        internal static void Register()
        {
            if (_registered || ModConfig.PhysicalJobsEnabled == null
                || !ModConfig.PhysicalJobsEnabled.Value)
            {
                return;
            }
            _registered = true;
            SettlerTaskRegistry.Register("lumberjack", () => new PhysicalLumberjackTask());
            SettlerTaskRegistry.Register("hauler", () => new PhysicalHaulerTask());
            Jotunn.Logger.LogInfo(
                "Registered physical Lumberjack and Hauler tasks; timer production is disabled for Lumberjack.");
        }
    }

    internal static class PhysicalTaskTelemetry
    {
        internal static void Set(SettlerTaskContext context, string state,
            string detail, Vector3 target)
        {
            if (context?.NetworkView == null || !context.NetworkView.IsValid())
            {
                return;
            }
            var zdo = context.NetworkView.GetZDO();
            zdo.Set(HearthZdoKeys.TaskState, state ?? "");
            zdo.Set(HearthZdoKeys.TaskDetail, detail ?? "");
            zdo.Set(HearthZdoKeys.TaskTarget, target);
            zdo.Set(HearthZdoKeys.TaskUpdated,
                ZNet.instance != null ? (long)ZNet.instance.GetTimeSeconds() : 0L);
        }

        internal static void Clear(SettlerTaskContext context, string detail = "Idle")
        {
            Set(context, "Idle", detail,
                context?.Actor != null ? context.Actor.transform.position : Vector3.zero);
        }

        internal static string Describe(ZNetView view)
        {
            if (view == null || !view.IsValid())
            {
                return "Task unavailable";
            }
            var zdo = view.GetZDO();
            var state = zdo.GetString(HearthZdoKeys.TaskState, "Idle");
            var detail = zdo.GetString(HearthZdoKeys.TaskDetail, "");
            var carry = zdo.GetInt(HearthZdoKeys.WorkCarryCount);
            var prefab = zdo.GetString(HearthZdoKeys.WorkCarryPrefab);
            return string.IsNullOrEmpty(prefab)
                ? $"{state}: {detail}"
                : $"{state}: {detail} • carrying {carry} {prefab}";
        }
    }
}
