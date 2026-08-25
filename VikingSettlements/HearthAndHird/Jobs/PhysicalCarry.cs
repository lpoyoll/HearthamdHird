using HearthAndHird.Network;
using HearthAndHird.NPC;
using HearthAndHird.Settlements;
using UnityEngine;
using VikingSettlements;
using VikingSettlements.Npcs;

namespace HearthAndHird.Jobs
{
    /// <summary>
    /// Real NPC inventory backed by a small ZDO manifest. The Humanoid carries
    /// the item while loaded; the ZDO restores it after save/load and prevents
    /// cargo disappearing when combat temporarily interrupts a task.
    /// </summary>
    public sealed class PhysicalCarry : MonoBehaviour
    {
        private ZNetView _nview;
        private Humanoid _humanoid;
        private SettlerProfile _profile;
        private bool _restored;
        private bool _wasOwner;

        internal string PrefabName => _nview != null && _nview.IsValid()
            ? _nview.GetZDO().GetString(HearthZdoKeys.WorkCarryPrefab)
            : "";

        internal int Count => _nview != null && _nview.IsValid()
            ? _nview.GetZDO().GetInt(HearthZdoKeys.WorkCarryCount)
            : 0;

        internal int Capacity
        {
            get
            {
                var strength = _profile != null ? _profile.Strength : 50;
                return ModConfig.PhysicalCarryCapacity.Value + Mathf.Max(0, strength - 50) / 10;
            }
        }

        internal bool IsFull => Count >= Capacity;

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
            _humanoid = GetComponent<Humanoid>();
            _profile = GetComponent<SettlerProfile>();
            var character = GetComponent<Character>();
            if (character != null)
            {
                character.m_onDeath += OnDeath;
            }
        }

        private void Start()
        {
            Restore();
        }

        private void OnDestroy()
        {
            var character = GetComponent<Character>();
            if (character != null)
            {
                character.m_onDeath -= OnDeath;
            }
        }

        private void Update()
        {
            var isOwner = _nview != null && _nview.IsValid() && _nview.IsOwner();
            if (!isOwner)
            {
                _restored = false;
            }
            else if (!_wasOwner)
            {
                _restored = false;
            }
            _wasOwner = isOwner;
            if (!_restored)
            {
                Restore();
            }
        }

        private void Restore()
        {
            if (_restored || _nview == null || !_nview.IsValid() || !_nview.IsOwner()
                || _humanoid == null || ObjectDB.instance == null)
            {
                return;
            }
            _restored = true;
            var prefabName = PrefabName;
            var count = Count;
            if (string.IsNullOrEmpty(prefabName) || count <= 0)
            {
                return;
            }
            RemoveInventoryCargo(prefabName);
            var item = SettlerWork.MakeItem(prefabName, count);
            if (item != null)
            {
                _humanoid.GetInventory().AddItem(item);
            }
        }

        internal bool TryCollect(ItemDrop drop)
        {
            if (drop == null || drop.m_itemData == null || _nview == null
                || !_nview.IsValid() || !_nview.IsOwner() || _humanoid == null)
            {
                return false;
            }
            var prefabName = drop.m_itemData.m_dropPrefab != null
                ? drop.m_itemData.m_dropPrefab.name : drop.gameObject.name.Replace("(Clone)", "");
            if (!IsTimber(prefabName)
                || (!string.IsNullOrEmpty(PrefabName) && PrefabName != prefabName))
            {
                return false;
            }

            var take = Mathf.Min(drop.m_itemData.m_stack, Capacity - Count);
            if (take <= 0)
            {
                return false;
            }
            var item = drop.m_itemData.Clone();
            item.m_stack = take;
            if (!_humanoid.GetInventory().AddItem(item))
            {
                return false;
            }

            _nview.GetZDO().Set(HearthZdoKeys.WorkCarryPrefab, prefabName);
            _nview.GetZDO().Set(HearthZdoKeys.WorkCarryCount, Count + take);
            if (take >= drop.m_itemData.m_stack)
            {
                var view = drop.GetComponent<ZNetView>();
                if (view != null && view.IsValid() && ZNetScene.instance != null)
                {
                    view.ClaimOwnership();
                    ZNetScene.instance.Destroy(drop.gameObject);
                }
                else
                {
                    Destroy(drop.gameObject);
                }
            }
            else
            {
                drop.SetStack(drop.m_itemData.m_stack - take);
            }
            return true;
        }

        internal bool LoadFrom(Container source, string prefabName)
        {
            if (source == null || _nview == null || !_nview.IsValid() || !_nview.IsOwner()
                || _humanoid == null || (!string.IsNullOrEmpty(PrefabName) && PrefabName != prefabName))
            {
                return false;
            }
            var sharedName = SettlerWork.SharedName(prefabName);
            var inventory = source.GetInventory();
            var take = sharedName != null && inventory != null
                ? Mathf.Min(inventory.CountItems(sharedName), Capacity - Count) : 0;
            if (take <= 0)
            {
                return false;
            }
            var item = SettlerWork.MakeItem(prefabName, take);
            if (item == null || !_humanoid.GetInventory().CanAddItem(item))
            {
                return false;
            }
            if (source.m_nview != null && source.m_nview.IsValid())
            {
                source.m_nview.ClaimOwnership();
            }
            inventory.RemoveItem(sharedName, take);
            if (!_humanoid.GetInventory().AddItem(item))
            {
                inventory.AddItem(item);
                return false;
            }
            _nview.GetZDO().Set(HearthZdoKeys.WorkCarryPrefab, prefabName);
            _nview.GetZDO().Set(HearthZdoKeys.WorkCarryCount, Count + take);
            return true;
        }

        internal bool Deposit(TimberStockpile store)
        {
            if (store == null || Count <= 0 || string.IsNullOrEmpty(PrefabName))
            {
                return false;
            }
            var item = SettlerWork.MakeItem(PrefabName, Count);
            if (item == null || !store.Deposit(item))
            {
                return false;
            }
            RemoveInventoryCargo(PrefabName);
            ClearManifest();
            return true;
        }

        internal void DropAll()
        {
            if (Count <= 0 || string.IsNullOrEmpty(PrefabName))
            {
                ClearManifest();
                return;
            }
            var item = SettlerWork.MakeItem(PrefabName, Count);
            if (item != null)
            {
                ItemDrop.DropItem(item, Count, transform.position + Vector3.up * 0.6f,
                    Quaternion.identity);
            }
            RemoveInventoryCargo(PrefabName);
            ClearManifest();
        }

        private void OnDeath()
        {
            if (_nview != null && _nview.IsValid() && _nview.IsOwner())
            {
                DropAll();
            }
        }

        private void ClearManifest()
        {
            if (_nview == null || !_nview.IsValid())
            {
                return;
            }
            _nview.GetZDO().Set(HearthZdoKeys.WorkCarryPrefab, "");
            _nview.GetZDO().Set(HearthZdoKeys.WorkCarryCount, 0);
        }

        private void RemoveInventoryCargo(string prefabName)
        {
            var sharedName = SettlerWork.SharedName(prefabName);
            if (_humanoid == null || sharedName == null)
            {
                return;
            }
            var inventory = _humanoid.GetInventory();
            var count = inventory.CountItems(sharedName);
            if (count > 0)
            {
                inventory.RemoveItem(sharedName, count);
            }
        }

        internal static bool IsTimber(string prefabName)
        {
            return prefabName == "Wood" || prefabName == "FineWood"
                || prefabName == "RoundLog";
        }
    }
}
