using HearthAndHird.Settlements;
using UnityEngine;
using VikingSettlements;
using VikingSettlements.Npcs;
using VikingSettlements.Settlements;

namespace HearthAndHird.Jobs
{
    /// <summary>
    /// First storage-logistics worker: walks to an ordinary source chest,
    /// loads a real timber stack into NPC inventory, walks to the designated
    /// Timber Store and deposits it. No inventory is moved at a distance.
    /// </summary>
    internal sealed class PhysicalHaulerTask : ISettlerTask
    {
        private enum Phase { FindSource, MoveToSource, Load, MoveToStore, Deposit }

        private static readonly string[] Timber = { "Wood", "FineWood", "RoundLog" };

        private Phase _phase;
        private PhysicalCarry _carry;
        private Container _source;
        private TimberStockpile _store;
        private string _prefabName;
        private Vector3 _lastPosition;
        private float _lastProgress;

        public string Id => "hauler";

        public bool CanStart(SettlerTaskContext context)
        {
            _carry = context.Actor.GetComponent<PhysicalCarry>();
            _store = TimberStockpile.FindNearest(context.Settler.Home,
                context.Actor.transform.position);
            if (_carry == null || _store == null)
            {
                PhysicalTaskTelemetry.Set(context, "Blocked",
                    _carry == null ? "No physical carrying component" : "Build a Timber Store",
                    context.Settler.Home);
                return false;
            }
            return true;
        }

        public void Start(SettlerTaskContext context)
        {
            ResetProgress(context);
            _phase = _carry.Count > 0 ? Phase.MoveToStore : Phase.FindSource;
            PhysicalTaskTelemetry.Set(context,
                _phase == Phase.MoveToStore ? "Hauling" : "Seeking",
                _phase == Phase.MoveToStore
                    ? "Resuming an interrupted delivery"
                    : "Looking for timber in an ordinary chest",
                context.Settler.Home);
        }

        public SettlerTaskStatus Tick(SettlerTaskContext context, float deltaTime)
        {
            if (context.Ai == null || _carry == null)
            {
                return SettlerTaskStatus.Failed;
            }
            switch (_phase)
            {
                case Phase.FindSource:
                    FindSource(context);
                    break;
                case Phase.MoveToSource:
                    if (_source == null)
                    {
                        _phase = Phase.FindSource;
                    }
                    else if (MoveTo(context, _source.transform.position, 2.2f, deltaTime))
                    {
                        _phase = Phase.Load;
                    }
                    break;
                case Phase.Load:
                    if (_source != null && _carry.LoadFrom(_source, _prefabName))
                    {
                        _phase = Phase.MoveToStore;
                        ResetProgress(context);
                        PhysicalTaskTelemetry.Set(context, "Hauling",
                            $"Loaded {_carry.Count} {_carry.PrefabName}; walking to Timber Store",
                            _store.transform.position);
                    }
                    else
                    {
                        _phase = Phase.FindSource;
                    }
                    break;
                case Phase.MoveToStore:
                    if (_store == null)
                    {
                        _store = TimberStockpile.FindNearest(context.Settler.Home,
                            context.Actor.transform.position,
                            SettlerWork.MakeItem(_carry.PrefabName, Mathf.Max(1, _carry.Count)));
                    }
                    if (_store == null)
                    {
                        PhysicalTaskTelemetry.Set(context, "Blocked",
                            "Timber Store is full or missing", context.Settler.Home);
                    }
                    else if (MoveTo(context, _store.transform.position, 2.2f, deltaTime))
                    {
                        _phase = Phase.Deposit;
                    }
                    break;
                case Phase.Deposit:
                    if (_carry.Deposit(_store))
                    {
                        _phase = Phase.FindSource;
                        PhysicalTaskTelemetry.Set(context, "Seeking",
                            "Delivery complete; looking for another source chest",
                            context.Settler.Home);
                    }
                    else
                    {
                        _phase = Phase.MoveToStore;
                    }
                    break;
            }
            return SettlerTaskStatus.Running;
        }

        public void Cancel(SettlerTaskContext context)
        {
            context.Ai?.StopMoving();
            PhysicalTaskTelemetry.Set(context, "Paused",
                "Interrupted; carried cargo retained", context.Actor.transform.position);
        }

        private void FindSource(SettlerTaskContext context)
        {
            var radius = PlayerSettlement.WorkRadiusAt(context.Settler.Home);
            _source = null;
            _prefabName = null;
            var bestDistance = float.MaxValue;
            foreach (var container in Object.FindObjectsOfType<Container>())
            {
                if (container == null || container.GetComponent<TimberStockpile>() != null
                    || Vector3.Distance(container.transform.position, context.Settler.Home) > radius)
                {
                    continue;
                }
                var inventory = container.GetInventory();
                if (inventory == null)
                {
                    continue;
                }
                foreach (var prefabName in Timber)
                {
                    var shared = SettlerWork.SharedName(prefabName);
                    if (shared == null || inventory.CountItems(shared) <= 0)
                    {
                        continue;
                    }
                    var distance = Vector3.Distance(context.Actor.transform.position,
                        container.transform.position);
                    if (distance < bestDistance)
                    {
                        _source = container;
                        _prefabName = prefabName;
                        bestDistance = distance;
                    }
                }
            }
            if (_source == null)
            {
                PhysicalTaskTelemetry.Set(context, "Blocked",
                    "No timber found in an ordinary source chest", context.Settler.Home);
                return;
            }
            _phase = Phase.MoveToSource;
            ResetProgress(context);
            PhysicalTaskTelemetry.Set(context, "Walking",
                $"Going to collect {_prefabName}", _source.transform.position);
        }

        private bool MoveTo(SettlerTaskContext context, Vector3 target, float range, float dt)
        {
            context.Ai.SetFollowTarget(null);
            context.Ai.SetPatrolPoint(target);
            var arrived = context.Ai.MoveTo(dt, target, range, true)
                || Vector3.Distance(context.Actor.transform.position, target) <= range;
            if (Vector3.Distance(context.Actor.transform.position, _lastPosition) > 0.75f)
            {
                ResetProgress(context);
            }
            else if (Time.time - _lastProgress >= ModConfig.PhysicalWorkStuckSeconds.Value)
            {
                context.Ai.ResetPatrolPoint();
                context.Ai.StopMoving();
                _phase = _carry.Count > 0 ? Phase.MoveToStore : Phase.FindSource;
                ResetProgress(context);
                PhysicalTaskTelemetry.Set(context, "Replanning",
                    "Path made no progress; abandoning target without teleporting", target);
                return false;
            }
            return arrived;
        }

        private void ResetProgress(SettlerTaskContext context)
        {
            _lastPosition = context.Actor.transform.position;
            _lastProgress = Time.time;
        }
    }
}
