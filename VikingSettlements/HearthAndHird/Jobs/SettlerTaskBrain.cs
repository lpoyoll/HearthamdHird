using HearthAndHird.AI;
using UnityEngine;
using VikingSettlements.Npcs;

namespace HearthAndHird.Jobs
{
    /// <summary>
    /// Owner-side runner for modular physical work. It remains dormant until
    /// a task is registered, so the foundation is backwards compatible with
    /// VikingSettlements' existing work implementation.
    /// </summary>
    public sealed class SettlerTaskBrain : MonoBehaviour
    {
        private const float TickInterval = 0.2f;

        private ZNetView _nview;
        private SettlerDirectiveState _directive;
        private SettlerTaskContext _context;
        private ISettlerTask _task;
        private int _seenRevision = -1;
        private float _nextTick;

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
            _directive = GetComponent<SettlerDirectiveState>();
            _context = new SettlerTaskContext
            {
                Actor = gameObject,
                NetworkView = _nview,
                Humanoid = GetComponent<Humanoid>(),
                Ai = GetComponent<MonsterAI>(),
                Settler = GetComponent<SettlerRecruitable>(),
                Directive = _directive,
            };
        }

        private void OnDisable()
        {
            CancelCurrent();
        }

        private void Update()
        {
            if (_nview == null || !_nview.IsValid() || !_nview.IsOwner() || _directive == null)
            {
                return;
            }
            if (Time.time < _nextTick)
            {
                return;
            }
            _nextTick = Time.time + TickInterval;

            if (_seenRevision != _directive.Revision)
            {
                _seenRevision = _directive.Revision;
                CancelCurrent();
            }

            if (_context.Humanoid == null || _context.Humanoid.IsDead()
                || _context.Settler == null || _context.Settler.State != SettlerState.Assigned
                || _context.Settler.IsHungry
                || (_context.Ai != null && _context.Ai.m_targetCreature != null))
            {
                CancelCurrent();
                return;
            }

            if (_directive.Kind != SettlerDirectiveKind.Work
                || !SettlerTaskRegistry.HasHandler(_directive.WorkId))
            {
                CancelCurrent();
                return;
            }

            if (_task == null)
            {
                if (!SettlerTaskRegistry.TryCreate(_directive.WorkId, out _task)
                    || !_task.CanStart(_context))
                {
                    _task = null;
                    return;
                }
                _task.Start(_context);
            }

            var status = _task.Tick(_context, TickInterval);
            if (status != SettlerTaskStatus.Running)
            {
                CancelCurrent();
            }
        }

        private void CancelCurrent()
        {
            if (_task == null)
            {
                return;
            }
            _task.Cancel(_context);
            _task = null;
        }
    }
}
