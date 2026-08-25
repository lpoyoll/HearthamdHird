using System;
using System.Collections;
using System.Collections.Generic;
using HearthAndHird.Network;
using UnityEngine;
using VikingSettlements.Npcs;
using VikingSettlements.World;

namespace VikingSettlements.Development
{
    internal sealed class VillageSpawnPlan
    {
        internal WildSettlementTier Tier;
        internal Vector3 Origin;
        internal Quaternion Rotation;
        internal SettlementLayout Layout;
        internal float WorstFoundationDelta;
        internal bool NearWorldStart;

        internal string Summary => $"{VillageHeart.TierDisplay(Tier)} • "
            + $"{VillageHeart.PopulationForTier(Tier)} residents • "
            + $"{Leader(Tier)} • site slope {WorstFoundationDelta:0.0}m";

        private static string Leader(WildSettlementTier tier)
        {
            if (tier >= WildSettlementTier.Hold) return "Jarl";
            if (tier >= WildSettlementTier.Hamlet) return "Elder";
            return "Headman/Headwoman";
        }
    }

    /// <summary>Plans and raises host-only test settlements in two safe passes.</summary>
    internal sealed class TestVillageSpawner : MonoBehaviour
    {
        private VillageSpawnPlan _plan;
        private Player _player;

        internal static bool TryPlan(Player player, WildSettlementTier tier,
            bool nearWorldStart, out VillageSpawnPlan plan, out string reason)
        {
            plan = null;
            reason = "No suitable site was found.";
            if (player == null || ZoneSystem.instance == null)
            {
                return false;
            }

            var anchor = player.transform.position;
            var forward = player.transform.forward;
            if (nearWorldStart)
            {
                if (!ZoneSystem.instance.GetLocationIcon("StartTemple", out anchor))
                {
                    reason = "The first-spawn location is not available yet.";
                    return false;
                }
                if (Vector3.Distance(player.transform.position, anchor) > 350f)
                {
                    reason = "Travel within 350 metres of the first spawn first.";
                    return false;
                }
                forward = Vector3.right;
            }
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.1f) forward = Vector3.forward;
            forward.Normalize();

            var layout = Layouts.DevelopmentSettlement(tier);
            var footprint = VillageHeart.FootprintForTier(tier);
            var distances = nearWorldStart
                ? new[] { Mathf.Max(70f, footprint + 28f), footprint + 48f, footprint + 68f }
                : new[] { footprint + 18f, footprint + 34f, footprint + 50f };
            var bestScore = float.MaxValue;
            foreach (var distance in distances)
            {
                for (var step = 0; step < 16; step++)
                {
                    var yaw = (step == 0 ? 0f : ((step + 1) / 2) * 22.5f * (step % 2 == 0 ? 1f : -1f));
                    var direction = Quaternion.Euler(0f, yaw, 0f) * forward;
                    var candidate = anchor + direction * distance;
                    candidate.y = ZoneSystem.instance.GetGroundHeight(candidate);
                    if (candidate.y < ZoneSystem.instance.m_waterLevel + 1.5f
                        || VillageHeart.FindNearest(candidate, footprint + 24f) != null)
                    {
                        continue;
                    }
                    var facing = anchor - candidate;
                    facing.y = 0f;
                    var rotation = facing.sqrMagnitude > 0.1f
                        ? Quaternion.LookRotation(facing.normalized, Vector3.up)
                        : Quaternion.identity;
                    if (!Survey(candidate, rotation, layout, out var worstDelta,
                        out var waterClear))
                    {
                        continue;
                    }
                    var score = worstDelta * 25f + distance * 0.025f - waterClear * 0.05f;
                    if (score >= bestScore)
                    {
                        continue;
                    }
                    bestScore = score;
                    plan = new VillageSpawnPlan
                    {
                        Tier = tier,
                        Origin = candidate,
                        Rotation = rotation,
                        Layout = layout,
                        WorstFoundationDelta = worstDelta,
                        NearWorldStart = nearWorldStart,
                    };
                }
            }
            if (plan == null)
            {
                reason = "No dry, level site with clear building foundations was found nearby.";
                return false;
            }
            reason = plan.Summary;
            return true;
        }

        private static bool Survey(Vector3 origin, Quaternion rotation,
            SettlementLayout layout, out float worstDelta, out float waterClear)
        {
            worstDelta = 0f;
            waterClear = float.MaxValue;
            var foundations = new Dictionary<Vector3, int>();
            foreach (var part in layout.Parts)
            {
                if (foundations.TryGetValue(part.Foundation, out var count))
                {
                    foundations[part.Foundation] = count + 1;
                }
                else
                {
                    foundations[part.Foundation] = 1;
                }
            }
            foreach (var pair in foundations)
            {
                var point = origin + rotation * pair.Key;
                var centre = ZoneSystem.instance.GetGroundHeight(point);
                waterClear = Mathf.Min(waterClear, centre - ZoneSystem.instance.m_waterLevel);
                if (waterClear < 1.2f)
                {
                    return false;
                }
                // Loose torches, paths, residents and palisade stakes conform
                // individually. Multi-part foundations must actually be flat.
                if (pair.Value < 4)
                {
                    continue;
                }
                var localMin = centre;
                var localMax = centre;
                foreach (var offset in new[]
                {
                    Vector3.left * 3f, Vector3.right * 3f,
                    Vector3.forward * 3f, Vector3.back * 3f,
                })
                {
                    var height = ZoneSystem.instance.GetGroundHeight(point + rotation * offset);
                    localMin = Mathf.Min(localMin, height);
                    localMax = Mathf.Max(localMax, height);
                }
                worstDelta = Mathf.Max(worstDelta, localMax - localMin);
                if (localMax - localMin > 2.2f)
                {
                    return false;
                }
            }
            return true;
        }

        internal static void Begin(Player player, VillageSpawnPlan plan)
        {
            if (!TestAuthority.IsHost || player == null || plan == null)
            {
                return;
            }
            var runnerObject = new GameObject("HearthAndHird_TestVillageSpawner");
            var runner = runnerObject.AddComponent<TestVillageSpawner>();
            runner._player = player;
            runner._plan = plan;
            runner.StartCoroutine(runner.Build());
        }

        private IEnumerator Build()
        {
            var batch = DateTime.UtcNow.Ticks.ToString();
            var terrain = LayoutBuilder.BuildTestAt(_plan.Origin, _plan.Rotation,
                _plan.Layout, batch, true);
            yield return new WaitForSeconds(0.75f);
            var objects = LayoutBuilder.BuildTestAt(_plan.Origin, _plan.Rotation,
                _plan.Layout, batch, false);
            yield return null;

            var heart = VillageHeart.FindNearest(_plan.Origin, 8f);
            if (heart != null)
            {
                heart.ConfigureGenerated(_plan.Tier, batch);
                heart.ConfigureResidents();
            }
            foreach (var settler in SettlerRecruitable.Instances)
            {
                if (settler == null)
                {
                    continue;
                }
                var view = settler.GetComponent<ZNetView>();
                if (view == null || !view.IsValid()
                    || view.GetZDO().GetString(HearthZdoKeys.VillageTestBatch) != batch)
                {
                    continue;
                }
                settler.MarkTestSpawned(settler.Level);
            }
            _player?.Message(MessageHud.MessageType.Center,
                $"Created {_plan.Summary} ({terrain + objects} objects). "
                + "Use DESPAWN TEST OBJECTS to remove it.");
            Destroy(gameObject);
        }
    }
}
