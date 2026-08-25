using HearthAndHird.Network;
using UnityEngine;

namespace VikingSettlements.Npcs
{
    internal enum VillageResidentRole
    {
        Villager = 0,
        Headman = 1,
        Elder = 2,
        Jarl = 3,
        Hersir = 4,
        Guard = 5,
        Housecarl = 6,
        Seer = 7,
    }

    /// <summary>
    /// Persistent membership of one exact wild settlement. Proximity is used
    /// only once, when an old/world-generated resident has no village record;
    /// relationships, ranks and home behaviour thereafter use the heart ZDO.
    /// </summary>
    public sealed class VillageResident : MonoBehaviour
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
                || _settler == null || _settler.State != SettlerState.Wild)
            {
                return;
            }
            if (Heart == null)
            {
                var heart = VillageHeart.FindNearest(transform.position);
                if (heart != null)
                {
                    Bind(heart, VillageResidentRole.Villager, transform.position);
                }
                else if (!HasHome)
                {
                    _nview.GetZDO().Set(HearthZdoKeys.VillageResidentHome, transform.position);
                }
            }
        }

        internal VillageHeart Heart
        {
            get
            {
                if (_nview == null || !_nview.IsValid())
                {
                    return null;
                }
                var zdo = _nview.GetZDO();
                var user = zdo.GetLong(HearthZdoKeys.VillageResidentUser);
                var id = zdo.GetLong(HearthZdoKeys.VillageResidentId);
                return user != 0L || id != 0L ? VillageHeart.FindById(user, id) : null;
            }
        }

        internal VillageResidentRole Role => _nview != null && _nview.IsValid()
            ? (VillageResidentRole)_nview.GetZDO().GetInt(HearthZdoKeys.VillageResidentRole)
            : VillageResidentRole.Villager;

        internal Vector3 Home => _nview != null && _nview.IsValid()
            ? _nview.GetZDO().GetVec3(HearthZdoKeys.VillageResidentHome, transform.position)
            : transform.position;

        internal bool HasHome => _nview != null && _nview.IsValid()
            && _nview.GetZDO().GetVec3(HearthZdoKeys.VillageResidentHome, Vector3.zero) != Vector3.zero;

        internal bool IsDefender
        {
            get
            {
                switch (Role)
                {
                    case VillageResidentRole.Headman:
                    case VillageResidentRole.Jarl:
                    case VillageResidentRole.Hersir:
                    case VillageResidentRole.Guard:
                    case VillageResidentRole.Housecarl:
                        return true;
                    default:
                        return false;
                }
            }
        }

        internal string Title
        {
            get
            {
                switch (Role)
                {
                    case VillageResidentRole.Headman: return "Headman/Headwoman";
                    case VillageResidentRole.Elder: return "Elder";
                    case VillageResidentRole.Jarl: return "Jarl";
                    case VillageResidentRole.Hersir: return "Hersir";
                    case VillageResidentRole.Guard: return "Guard";
                    case VillageResidentRole.Housecarl: return "Housecarl";
                    case VillageResidentRole.Seer: return "Seer — healer and omen-reader";
                    default: return "Villager";
                }
            }
        }

        internal void Bind(VillageHeart heart, VillageResidentRole role, Vector3 home)
        {
            if (heart == null || _nview == null || !_nview.IsValid())
            {
                return;
            }
            _nview.ClaimOwnership();
            var id = heart.Id;
            var zdo = _nview.GetZDO();
            zdo.Set(HearthZdoKeys.VillageResidentUser, id.UserID);
            zdo.Set(HearthZdoKeys.VillageResidentId, (long)id.ID);
            zdo.Set(HearthZdoKeys.VillageResidentRole, (int)role);
            zdo.Set(HearthZdoKeys.VillageResidentHome, home);
        }

        internal void SetRole(VillageResidentRole role)
        {
            if (_nview == null || !_nview.IsValid())
            {
                return;
            }
            _nview.ClaimOwnership();
            _nview.GetZDO().Set(HearthZdoKeys.VillageResidentRole, (int)role);
        }
    }
}
