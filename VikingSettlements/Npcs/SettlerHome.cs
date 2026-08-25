using UnityEngine;

namespace VikingSettlements.Npcs
{
    /// <summary>
    /// Keeps wild residents within their real home settlement. A soft leash
    /// shapes ordinary wandering; beyond the hard leash the owner-side AI
    /// follows a local home anchor until it is safely back. No visible routine
    /// teleport is used.
    /// </summary>
    public class SettlerHome : MonoBehaviour
    {
        private const float TickInterval = 1f;
        private const float LoneSoftRadius = 22f;
        private const float LoneHardRadius = 36f;

        private ZNetView _nview;
        private MonsterAI _ai;
        private SettlerRecruitable _settler;
        private VillageResident _resident;
        private GameObject _returnAnchor;
        private float _baseMoveRange = -1f;
        private float _nextTick;
        private bool _returning;

        internal bool IsReturning => _returning;

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
            _ai = GetComponent<MonsterAI>();
            _settler = GetComponent<SettlerRecruitable>();
            _resident = GetComponent<VillageResident>();
            if (_ai != null)
            {
                _baseMoveRange = _ai.m_randomMoveRange;
            }
        }

        private void OnDestroy()
        {
            if (_returnAnchor != null)
            {
                Destroy(_returnAnchor);
            }
        }

        private void Update()
        {
            if (Time.time < _nextTick)
            {
                return;
            }
            _nextTick = Time.time + TickInterval;
            if (_nview == null || !_nview.IsValid() || !_nview.IsOwner()
                || _ai == null || _settler == null)
            {
                return;
            }
            if (_settler.State != SettlerState.Wild)
            {
                StopReturning(false);
                RestoreMoveRange();
                return;
            }

            var home = _resident != null ? _resident.Home : transform.position;
            var heart = _resident != null ? _resident.Heart : null;
            var softRadius = heart != null ? heart.ResidentSoftRadius : LoneSoftRadius;
            var hardRadius = heart != null ? softRadius + 16f : LoneHardRadius;
            var distance = Vector3.Distance(transform.position, home);
            var outsideVillage = heart != null
                && Vector3.Distance(transform.position, heart.transform.position)
                    > heart.ResidentHardRadius;

            _ai.m_randomMoveRange = Mathf.Min(Mathf.Max(4f, _baseMoveRange), softRadius);
            if (_ai.m_targetCreature != null && !_ai.m_targetCreature.IsDead())
            {
                return; // finish or escape the immediate fight, then go home
            }
            if (distance > hardRadius || outsideVillage)
            {
                ReturnHome(home);
            }
            else if (_returning && distance <= softRadius * 0.65f)
            {
                StopReturning(true);
            }
        }

        internal void ReturnFromThreat()
        {
            if (_nview == null || !_nview.IsValid() || !_nview.IsOwner()
                || _resident == null || _ai == null)
            {
                return;
            }
            _ai.m_targetCreature = null;
            ReturnHome(_resident.Home);
        }

        private void ReturnHome(Vector3 home)
        {
            if (_returnAnchor == null)
            {
                _returnAnchor = new GameObject("HearthAndHird_HomeAnchor");
            }
            _returnAnchor.transform.position = home;
            _returning = true;
            _ai.m_randomMoveRange = 0f;
            _ai.SetFollowTarget(_returnAnchor);
            _ai.SetPatrolPoint(home);
        }

        private void StopReturning(bool setPatrol)
        {
            if (!_returning)
            {
                return;
            }
            if (_ai != null && _ai.GetFollowTarget() == _returnAnchor)
            {
                _ai.SetFollowTarget(null);
                if (setPatrol && _returnAnchor != null)
                {
                    _ai.SetPatrolPoint(_returnAnchor.transform.position);
                }
            }
            _returning = false;
        }

        private void RestoreMoveRange()
        {
            if (_ai != null && _baseMoveRange >= 0f)
            {
                _ai.m_randomMoveRange = _baseMoveRange;
            }
        }
    }
}
