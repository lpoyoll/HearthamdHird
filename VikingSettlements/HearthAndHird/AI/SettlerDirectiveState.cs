using System;
using HearthAndHird.Network;
using UnityEngine;
using VikingSettlements.Npcs;
using VikingSettlements.Party;

namespace HearthAndHird.AI
{
    /// <summary>The high-level order a settler is currently obeying.</summary>
    internal enum SettlerDirectiveKind
    {
        Idle = 0,
        Follow = 1,
        Hold = 2,
        Fallback = 3,
        Work = 4,
        Guard = 5,
        Attack = 6,
    }

    /// <summary>
    /// A small, network-persistent command envelope shared by party and work
    /// AI. It mirrors legacy VikingSettlements state during the migration and
    /// gives future physical jobs one stable input instead of reading several
    /// unrelated ZDO keys.
    /// </summary>
    public sealed class SettlerDirectiveState : MonoBehaviour
    {
        private ZNetView _nview;
        private SettlerRecruitable _settler;

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
            _settler = GetComponent<SettlerRecruitable>();
        }

        private void Start()
        {
            if (_nview == null || !_nview.IsValid() || !_nview.IsOwner()
                || _nview.GetZDO().GetInt(HearthZdoKeys.DirectiveRevision) > 0)
            {
                return;
            }
            ImportLegacyState();
        }

        internal SettlerDirectiveKind Kind => _nview != null && _nview.IsValid()
            ? (SettlerDirectiveKind)_nview.GetZDO().GetInt(HearthZdoKeys.Directive)
            : SettlerDirectiveKind.Idle;

        internal int Revision => _nview != null && _nview.IsValid()
            ? _nview.GetZDO().GetInt(HearthZdoKeys.DirectiveRevision)
            : 0;

        internal Vector3 Target => _nview != null && _nview.IsValid()
            ? _nview.GetZDO().GetVec3(HearthZdoKeys.DirectiveTarget, transform.position)
            : transform.position;

        internal string WorkId => _nview != null && _nview.IsValid()
            ? _nview.GetZDO().GetString(HearthZdoKeys.DirectiveWorkId)
            : "";

        internal void ApplyLegacy(
            SettlerDirectiveKind kind,
            Vector3 target,
            string workId = "",
            long issuerId = 0L)
        {
            if (_nview == null || !_nview.IsValid())
            {
                return;
            }

            _nview.ClaimOwnership();
            var zdo = _nview.GetZDO();
            var unchanged = zdo.GetInt(HearthZdoKeys.Directive) == (int)kind
                && zdo.GetString(HearthZdoKeys.DirectiveWorkId) == (workId ?? "")
                && Vector3.Distance(zdo.GetVec3(HearthZdoKeys.DirectiveTarget, target), target) < 0.05f;
            if (unchanged && zdo.GetInt(HearthZdoKeys.DirectiveRevision) > 0)
            {
                return;
            }

            zdo.Set(HearthZdoKeys.Directive, (int)kind);
            zdo.Set(HearthZdoKeys.DirectiveTarget, target);
            zdo.Set(HearthZdoKeys.DirectiveWorkId, workId ?? "");
            zdo.Set(HearthZdoKeys.DirectiveIssuer, issuerId);
            zdo.Set(HearthZdoKeys.DirectiveRevision,
                Math.Max(1, zdo.GetInt(HearthZdoKeys.DirectiveRevision) + 1));
        }

        internal static SettlerDirectiveKind FromPartyStance(PartyStance stance)
        {
            switch (stance)
            {
                case PartyStance.Hold: return SettlerDirectiveKind.Hold;
                case PartyStance.Fallback: return SettlerDirectiveKind.Fallback;
                default: return SettlerDirectiveKind.Follow;
            }
        }

        internal static SettlerDirectiveKind FromJob(SettlerJob job)
        {
            if (job == SettlerJob.Guard)
            {
                return SettlerDirectiveKind.Guard;
            }
            return job == SettlerJob.Villager
                ? SettlerDirectiveKind.Idle
                : SettlerDirectiveKind.Work;
        }

        internal static string WorkIdFor(SettlerJob job)
        {
            return job == SettlerJob.Villager ? "" : job.ToString().ToLowerInvariant();
        }

        private void ImportLegacyState()
        {
            if (_settler == null)
            {
                ApplyLegacy(SettlerDirectiveKind.Idle, transform.position);
                return;
            }

            switch (_settler.State)
            {
                case SettlerState.Following:
                    var member = GetComponent<PartyMember>();
                    var stance = member != null ? member.Stance : PartyStance.Follow;
                    ApplyLegacy(FromPartyStance(stance), transform.position,
                        issuerId: _settler.RecruiterId);
                    break;
                case SettlerState.Assigned:
                    ApplyLegacy(FromJob(_settler.Job), _settler.Home,
                        WorkIdFor(_settler.Job), _settler.RecruiterId);
                    break;
                default:
                    ApplyLegacy(SettlerDirectiveKind.Idle, transform.position);
                    break;
            }
        }
    }
}
