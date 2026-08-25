using System.Collections.Generic;
using HearthAndHird.Network;
using UnityEngine;
using VikingSettlements;
using VikingSettlements.Npcs;
using VikingSettlements.Settlements;

namespace HearthAndHird.Settlements
{
    /// <summary>
    /// A persistent, visible work-zone marker. Physical lumberjacks only fell
    /// mature TreeBase objects inside an enabled marker that belongs within
    /// their Hearthstone's work radius.
    /// </summary>
    public sealed class ForestryZone : MonoBehaviour, Hoverable, Interactable
    {
        internal static readonly List<ForestryZone> Instances = new List<ForestryZone>();

        private ZNetView _nview;

        internal float Radius => _nview != null && _nview.IsValid()
            ? _nview.GetZDO().GetFloat(HearthZdoKeys.ForestryRadius,
                ModConfig.ForestryZoneRadius.Value)
            : ModConfig.ForestryZoneRadius.Value;

        internal bool IsEnabled => _nview == null || !_nview.IsValid()
            || _nview.GetZDO().GetBool(HearthZdoKeys.ForestryEnabled, true);

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
        }

        private void Start()
        {
            if (_nview == null || !_nview.IsValid() || !_nview.IsOwner())
            {
                return;
            }
            var zdo = _nview.GetZDO();
            if (zdo.GetFloat(HearthZdoKeys.ForestryRadius) <= 0f)
            {
                zdo.Set(HearthZdoKeys.ForestryRadius, ModConfig.ForestryZoneRadius.Value);
                zdo.Set(HearthZdoKeys.ForestryEnabled, true);
            }
        }

        private void OnEnable()
        {
            Instances.Add(this);
        }

        private void OnDisable()
        {
            Instances.Remove(this);
        }

        internal bool Contains(Vector3 position)
        {
            return IsEnabled && Vector3.Distance(transform.position, position) <= Radius;
        }

        internal static ForestryZone FindFor(Vector3 home)
        {
            var workRadius = PlayerSettlement.WorkRadiusAt(home);
            ForestryZone best = null;
            var bestDistance = float.MaxValue;
            foreach (var zone in Instances)
            {
                if (zone == null || !zone.IsEnabled)
                {
                    continue;
                }
                var distance = Vector3.Distance(zone.transform.position, home);
                if (distance <= workRadius && distance < bestDistance)
                {
                    best = zone;
                    bestDistance = distance;
                }
            }
            return best;
        }

        public string GetHoverName()
        {
            return Localization.instance.Localize("$hnh_forestry_marker");
        }

        public string GetHoverText()
        {
            var state = IsEnabled ? "$hnh_forestry_active" : "$hnh_forestry_paused";
            return Localization.instance.Localize(
                $"{GetHoverName()}\n{state} • {Radius:0}m\n[<color=yellow><b>$KEY_Use</b></color>] Change radius"
                + "\n[<color=yellow><b>Shift+$KEY_Use</b></color>] Enable / pause");
        }

        public bool Interact(Humanoid character, bool hold, bool alt)
        {
            if (hold || character is not Player player || _nview == null || !_nview.IsValid())
            {
                return false;
            }
            if (PlayerSettlement.FindOwnedContaining(transform.position, player.GetPlayerID()) == null)
            {
                player.Message(MessageHud.MessageType.Center,
                    "This Forestry Marker must be inside a Hearthstone you founded.");
                return true;
            }

            _nview.ClaimOwnership();
            var zdo = _nview.GetZDO();
            if (alt)
            {
                zdo.Set(HearthZdoKeys.ForestryEnabled, !IsEnabled);
            }
            else
            {
                var next = Radius < 20f ? 25f : Radius < 32f ? 40f : 15f;
                zdo.Set(HearthZdoKeys.ForestryRadius, next);
            }
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }
    }

    /// <summary>Designated physical destination for timber logistics.</summary>
    public sealed class TimberStockpile : MonoBehaviour
    {
        internal static readonly List<TimberStockpile> Instances = new List<TimberStockpile>();

        private Container _container;

        private void Awake()
        {
            _container = GetComponent<Container>();
        }

        private void OnEnable()
        {
            Instances.Add(this);
        }

        private void OnDisable()
        {
            Instances.Remove(this);
        }

        internal Inventory Inventory => _container != null ? _container.GetInventory() : null;

        internal bool CanAccept(ItemDrop.ItemData item)
        {
            var inventory = Inventory;
            return item != null && inventory != null && inventory.CanAddItem(item);
        }

        internal bool Deposit(ItemDrop.ItemData item)
        {
            if (!CanAccept(item))
            {
                return false;
            }
            if (_container.m_nview != null && _container.m_nview.IsValid())
            {
                _container.m_nview.ClaimOwnership();
            }
            return Inventory.AddItem(item);
        }

        internal static TimberStockpile FindNearest(Vector3 home, Vector3 from,
            ItemDrop.ItemData item = null)
        {
            var radius = PlayerSettlement.WorkRadiusAt(home);
            TimberStockpile best = null;
            var bestDistance = float.MaxValue;
            foreach (var store in Instances)
            {
                if (store == null || Vector3.Distance(store.transform.position, home) > radius
                    || (item != null && !store.CanAccept(item)))
                {
                    continue;
                }
                var distance = Vector3.Distance(store.transform.position, from);
                if (distance < bestDistance)
                {
                    best = store;
                    bestDistance = distance;
                }
            }
            return best;
        }
    }
}
