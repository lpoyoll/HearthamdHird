using System.Collections.Generic;
using HearthAndHird.NPC;
using HearthAndHird.Settlements;
using UnityEngine;
using VikingSettlements;
using VikingSettlements.Npcs;
using VikingSettlements.Settlements;

namespace HearthAndHird.Jobs
{
    /// <summary>
    /// The first fully physical production loop: choose a marked mature tree,
    /// walk to it, visibly swing an axe, process its logs, collect the actual
    /// drops into the NPC inventory, carry them to a timber store and deposit.
    /// </summary>
    internal sealed class PhysicalLumberjackTask : ISettlerTask
    {
        private enum Phase
        {
            FindTree,
            MoveToTree,
            ChopTree,
            WaitForLogs,
            MoveToLog,
            ChopLog,
            Collect,
            MoveToStore,
            Deposit,
        }

        private static readonly HashSet<int> ReservedTrees = new HashSet<int>();
        private static readonly HashSet<int> ReservedLogs = new HashSet<int>();

        private Phase _phase;
        private ForestryZone _zone;
        private TreeBase _tree;
        private TreeLog _log;
        private int _reservedTreeId;
        private int _reservedLogId;
        private TimberStockpile _store;
        private PhysicalCarry _carry;
        private SettlerEquipment _equipment;
        private Vector3 _workPoint;
        private Vector3 _lastProgressPosition;
        private float _lastProgressTime;
        private float _nextSwing;
        private float _waitUntil;
        private float _logSearchDeadline;
        private float _nextSearch;

        public string Id => "lumberjack";

        public bool CanStart(SettlerTaskContext context)
        {
            _carry = context.Actor.GetComponent<PhysicalCarry>();
            _equipment = context.Actor.GetComponent<SettlerEquipment>();
            _zone = ForestryZone.FindFor(context.Settler.Home);
            _store = TimberStockpile.FindNearest(context.Settler.Home,
                context.Actor.transform.position);
            if (_carry == null)
            {
                PhysicalTaskTelemetry.Set(context, "Blocked", "No physical carrying component",
                    context.Actor.transform.position);
                return false;
            }
            if (_carry.Count > 0 && _store == null)
            {
                PhysicalTaskTelemetry.Set(context, "Blocked", "Build a Timber Store",
                    context.Settler.Home);
                return false;
            }
            if (_carry.Count == 0 && _zone == null)
            {
                PhysicalTaskTelemetry.Set(context, "Blocked", "Build an active Forestry Marker",
                    context.Settler.Home);
                return false;
            }
            if (_store == null)
            {
                PhysicalTaskTelemetry.Set(context, "Blocked", "Build a Timber Store",
                    context.Settler.Home);
                return false;
            }
            return true;
        }

        public void Start(SettlerTaskContext context)
        {
            _lastProgressPosition = context.Actor.transform.position;
            _lastProgressTime = Time.time;
            if (_carry.Count > 0)
            {
                _phase = Phase.MoveToStore;
                PhysicalTaskTelemetry.Set(context, "Hauling", "Taking timber to storage",
                    _store.transform.position);
            }
            else
            {
                _phase = Phase.FindTree;
                PhysicalTaskTelemetry.Set(context, "Seeking", "Looking inside the Forestry Marker",
                    _zone.transform.position);
            }
        }

        public SettlerTaskStatus Tick(SettlerTaskContext context, float deltaTime)
        {
            if (_carry == null || context.Ai == null || context.Humanoid == null)
            {
                return SettlerTaskStatus.Failed;
            }

            switch (_phase)
            {
                case Phase.FindTree:
                    FindTree(context);
                    break;
                case Phase.MoveToTree:
                    if (_tree == null)
                    {
                        ReleaseTree();
                        _phase = Phase.WaitForLogs;
                        _waitUntil = Time.time + 1.2f;
                        _logSearchDeadline = Time.time + 8f;
                        break;
                    }
                    if (MoveTo(context, _tree.transform.position, 2.4f, deltaTime))
                    {
                        _phase = Phase.ChopTree;
                        _nextSwing = 0f;
                    }
                    break;
                case Phase.ChopTree:
                    ChopTree(context);
                    break;
                case Phase.WaitForLogs:
                    if (Time.time >= _waitUntil)
                    {
                        if (!FindLog(context))
                        {
                            if (Time.time >= _logSearchDeadline)
                            {
                                _phase = Phase.Collect;
                            }
                            else
                            {
                                _waitUntil = Time.time + 0.5f;
                            }
                        }
                    }
                    break;
                case Phase.MoveToLog:
                    if (_log == null)
                    {
                        ReleaseLog();
                        _phase = Phase.Collect;
                        break;
                    }
                    if (MoveTo(context, _log.transform.position, 2.6f, deltaTime))
                    {
                        _phase = Phase.ChopLog;
                        _nextSwing = 0f;
                    }
                    break;
                case Phase.ChopLog:
                    ChopLog(context);
                    break;
                case Phase.Collect:
                    Collect(context, deltaTime);
                    break;
                case Phase.MoveToStore:
                    if (_store == null || !_store.CanAccept(
                            VikingSettlements.Npcs.SettlerWork.MakeItem(_carry.PrefabName,
                                Mathf.Max(1, _carry.Count))))
                    {
                        _store = TimberStockpile.FindNearest(context.Settler.Home,
                            context.Actor.transform.position,
                            VikingSettlements.Npcs.SettlerWork.MakeItem(_carry.PrefabName,
                                Mathf.Max(1, _carry.Count)));
                    }
                    if (_store == null)
                    {
                        PhysicalTaskTelemetry.Set(context, "Blocked", "Timber Store is full or missing",
                            context.Settler.Home);
                        return SettlerTaskStatus.Running;
                    }
                    if (MoveTo(context, _store.transform.position, 2.4f, deltaTime))
                    {
                        _phase = Phase.Deposit;
                    }
                    break;
                case Phase.Deposit:
                    if (_carry.Deposit(_store))
                    {
                        _phase = Phase.FindTree;
                        PhysicalTaskTelemetry.Set(context, "Seeking",
                            "Timber deposited; looking for another marked tree",
                            _zone != null ? _zone.transform.position : context.Settler.Home);
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
            ReleaseTree();
            ReleaseLog();
            _equipment?.ClearWorkTool();
            context.Ai?.StopMoving();
            PhysicalTaskTelemetry.Set(context, "Paused", "Interrupted; cargo and progress retained",
                context.Actor.transform.position);
        }

        private void FindTree(SettlerTaskContext context)
        {
            if (Time.time < _nextSearch)
            {
                return;
            }
            _nextSearch = Time.time + 2f;
            _zone = ForestryZone.FindFor(context.Settler.Home);
            if (_zone == null)
            {
                PhysicalTaskTelemetry.Set(context, "Blocked", "No active Forestry Marker",
                    context.Settler.Home);
                return;
            }
            TreeBase best = null;
            var bestDistance = float.MaxValue;
            foreach (var candidate in Object.FindObjectsOfType<TreeBase>())
            {
                if (candidate == null || !_zone.Contains(candidate.transform.position)
                    || Vector3.Distance(candidate.transform.position, context.Settler.Home)
                        > PlayerSettlement.WorkRadiusAt(context.Settler.Home)
                    || ReservedTrees.Contains(candidate.GetInstanceID()))
                {
                    continue;
                }
                var distance = Vector3.Distance(context.Actor.transform.position,
                    candidate.transform.position);
                if (distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }
            if (best == null)
            {
                PhysicalTaskTelemetry.Set(context, "Blocked", "No mature trees in the marked zone",
                    _zone.transform.position);
                return;
            }
            _tree = best;
            _reservedTreeId = _tree.GetInstanceID();
            ReservedTrees.Add(_reservedTreeId);
            _workPoint = _tree.transform.position;
            _phase = Phase.MoveToTree;
            ResetProgress(context);
            PhysicalTaskTelemetry.Set(context, "Walking", "Approaching a marked tree", _workPoint);
        }

        private void ChopTree(SettlerTaskContext context)
        {
            if (_tree == null)
            {
                ReleaseTree();
                _phase = Phase.WaitForLogs;
                _waitUntil = Time.time + 1.2f;
                _logSearchDeadline = Time.time + 8f;
                PhysicalTaskTelemetry.Set(context, "Waiting", "Tree felled; locating the trunk",
                    _workPoint);
                return;
            }
            context.Ai.StopMoving();
            context.Ai.LookAt(_tree.transform.position);
            if (Time.time < _nextSwing)
            {
                return;
            }
            _equipment?.EquipWorkTool("AxeBronze");
            context.Humanoid.StartAttack(null, false);
            _tree.Damage(ChopHit(context, _tree.transform.position));
            _nextSwing = Time.time + 1.15f;
            PhysicalTaskTelemetry.Set(context, "Working", "Felling tree with a real axe",
                _tree.transform.position);
        }

        private bool FindLog(SettlerTaskContext context)
        {
            TreeLog best = null;
            var bestDistance = float.MaxValue;
            foreach (var candidate in Object.FindObjectsOfType<TreeLog>())
            {
                if (candidate == null || Vector3.Distance(candidate.transform.position, _workPoint) > 14f
                    || ReservedLogs.Contains(candidate.GetInstanceID()))
                {
                    continue;
                }
                var distance = Vector3.Distance(context.Actor.transform.position,
                    candidate.transform.position);
                if (distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }
            if (best == null)
            {
                return false;
            }
            _log = best;
            _reservedLogId = _log.GetInstanceID();
            ReservedLogs.Add(_reservedLogId);
            _phase = Phase.MoveToLog;
            ResetProgress(context);
            PhysicalTaskTelemetry.Set(context, "Walking", "Approaching the fallen trunk",
                _log.transform.position);
            return true;
        }

        private void ChopLog(SettlerTaskContext context)
        {
            if (_log == null)
            {
                ReleaseLog();
                _waitUntil = Time.time + 0.8f;
                _logSearchDeadline = Mathf.Max(_logSearchDeadline, Time.time + 1.5f);
                _phase = Phase.WaitForLogs;
                return;
            }
            context.Ai.StopMoving();
            context.Ai.LookAt(_log.transform.position);
            if (Time.time < _nextSwing)
            {
                return;
            }
            _equipment?.EquipWorkTool("AxeBronze");
            context.Humanoid.StartAttack(null, false);
            _log.Damage(ChopHit(context, _log.transform.position));
            _nextSwing = Time.time + 1.05f;
            PhysicalTaskTelemetry.Set(context, "Working", "Processing the fallen log",
                _log.transform.position);
        }

        private void Collect(SettlerTaskContext context, float deltaTime)
        {
            ItemDrop best = null;
            var bestDistance = float.MaxValue;
            foreach (var drop in Object.FindObjectsOfType<ItemDrop>())
            {
                if (drop == null || drop.m_itemData == null
                    || Vector3.Distance(drop.transform.position, _workPoint) > 16f)
                {
                    continue;
                }
                var prefabName = drop.m_itemData.m_dropPrefab != null
                    ? drop.m_itemData.m_dropPrefab.name : drop.gameObject.name.Replace("(Clone)", "");
                if (!PhysicalCarry.IsTimber(prefabName)
                    || (!string.IsNullOrEmpty(_carry.PrefabName)
                        && _carry.PrefabName != prefabName))
                {
                    continue;
                }
                var distance = Vector3.Distance(context.Actor.transform.position,
                    drop.transform.position);
                if (distance < bestDistance)
                {
                    best = drop;
                    bestDistance = distance;
                }
            }
            if (best == null || _carry.IsFull)
            {
                if (_carry.Count > 0)
                {
                    _equipment?.ClearWorkTool();
                    _phase = Phase.MoveToStore;
                    ResetProgress(context);
                    PhysicalTaskTelemetry.Set(context, "Hauling", "Carrying actual drops to storage",
                        _store != null ? _store.transform.position : context.Settler.Home);
                }
                else
                {
                    _phase = Phase.FindTree;
                }
                return;
            }
            if (MoveTo(context, best.transform.position, 1.6f, deltaTime)
                && _carry.TryCollect(best))
            {
                PhysicalTaskTelemetry.Set(context, "Collecting",
                    $"Picked up timber ({_carry.Count}/{_carry.Capacity})",
                    context.Actor.transform.position);
            }
        }

        private bool MoveTo(SettlerTaskContext context, Vector3 target, float range, float dt)
        {
            context.Ai.SetFollowTarget(null);
            context.Ai.SetPatrolPoint(target);
            var arrived = context.Ai.MoveTo(dt, target, range, true)
                || Vector3.Distance(context.Actor.transform.position, target) <= range;
            return TrackProgress(context, target) && arrived;
        }

        private bool TrackProgress(SettlerTaskContext context, Vector3 target)
        {
            if (Vector3.Distance(context.Actor.transform.position, _lastProgressPosition) > 0.75f)
            {
                ResetProgress(context);
                return true;
            }
            if (Time.time - _lastProgressTime < ModConfig.PhysicalWorkStuckSeconds.Value)
            {
                return true;
            }
            context.Ai.ResetPatrolPoint();
            context.Ai.StopMoving();
            ReleaseTree();
            ReleaseLog();
            _phase = _carry.Count > 0 ? Phase.MoveToStore : Phase.FindTree;
            ResetProgress(context);
            PhysicalTaskTelemetry.Set(context, "Replanning",
                "Path made no progress; abandoning target without teleporting", target);
            return false;
        }

        private void ResetProgress(SettlerTaskContext context)
        {
            _lastProgressPosition = context.Actor.transform.position;
            _lastProgressTime = Time.time;
        }

        private static HitData ChopHit(SettlerTaskContext context, Vector3 point)
        {
            var profile = context.Actor.GetComponent<SettlerProfile>();
            var strength = profile != null ? profile.Strength : 50;
            var hit = new HitData
            {
                m_point = point,
                m_dir = context.Actor.transform.forward,
                m_toolTier = 2,
                m_skill = Skills.SkillType.WoodCutting,
            };
            hit.m_damage.m_chop = 24f + strength * 0.18f;
            hit.SetAttacker(context.Humanoid);
            return hit;
        }

        private void ReleaseTree()
        {
            if (_reservedTreeId != 0)
            {
                ReservedTrees.Remove(_reservedTreeId);
            }
            _reservedTreeId = 0;
            _tree = null;
        }

        private void ReleaseLog()
        {
            if (_reservedLogId != 0)
            {
                ReservedLogs.Remove(_reservedLogId);
            }
            _reservedLogId = 0;
            _log = null;
        }
    }
}
