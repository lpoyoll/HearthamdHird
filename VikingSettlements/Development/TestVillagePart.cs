using System.Collections.Generic;
using HearthAndHird.Network;
using UnityEngine;

namespace VikingSettlements.Development
{
    /// <summary>Marks every loaded object belonging to an F7-generated village.</summary>
    internal sealed class TestVillagePart : MonoBehaviour
    {
        private static readonly List<TestVillagePart> Instances = new List<TestVillagePart>();
        private string _batch;

        private void OnEnable()
        {
            Instances.Add(this);
        }

        private void OnDisable()
        {
            Instances.Remove(this);
        }

        internal void Configure(string batch)
        {
            _batch = batch ?? "";
        }

        internal static int DestroyLoaded()
        {
            var objects = new HashSet<GameObject>();
            foreach (var marker in new List<TestVillagePart>(Instances))
            {
                if (marker != null && !string.IsNullOrEmpty(marker._batch))
                {
                    objects.Add(marker.gameObject);
                }
            }
            // Dynamic marker components are not serialized. The ZDO tag makes
            // loaded test villages removable after saving and reloading too.
            foreach (var view in Object.FindObjectsOfType<ZNetView>())
            {
                if (view != null && view.IsValid()
                    && !string.IsNullOrEmpty(
                        view.GetZDO().GetString(HearthZdoKeys.VillageTestBatch)))
                {
                    objects.Add(view.gameObject);
                }
            }
            foreach (var gameObject in objects)
            {
                var view = gameObject != null ? gameObject.GetComponent<ZNetView>() : null;
                if (view != null && view.IsValid() && ZNetScene.instance != null)
                {
                    ZNetScene.instance.Destroy(gameObject);
                }
                else if (gameObject != null)
                {
                    Object.Destroy(gameObject);
                }
            }
            return objects.Count;
        }
    }
}
