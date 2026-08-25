using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;

namespace HearthAndHird.NPC
{
    /// <summary>
    /// Replaces only a settler prefab's visual hierarchy with a copy of the
    /// vanilla Player visual. The Dvergr-derived Humanoid, MonsterAI,
    /// ZNetView, colliders and combat configuration remain on the root.
    /// </summary>
    internal static class PlayerBodyVisualAdapter
    {
        internal static bool TryApply(GameObject settlerPrefab)
        {
            var playerPrefab = PrefabManager.Instance.GetPrefab("Player");
            var sourceVisual = playerPrefab != null
                ? playerPrefab.transform.Find("Visual")
                : null;
            var targetEquipment = settlerPrefab.GetComponent<VisEquipment>();
            var sourceEquipment = playerPrefab != null
                ? playerPrefab.GetComponent<VisEquipment>()
                : null;

            if (sourceVisual == null || sourceEquipment == null || targetEquipment == null)
            {
                Jotunn.Logger.LogWarning(
                    $"Hearth & Hird could not apply the Player visual to {settlerPrefab.name}; " +
                    "the compatibility visual will be retained");
                return false;
            }

            // PrefabManager stores clones below an inactive container. Clone
            // directly into that inactive hierarchy so the copied Player
            // components cannot run Awake before all anchors are rewired.
            var graft = Object.Instantiate(
                sourceVisual.gameObject, settlerPrefab.transform, false);
            graft.name = "HnH_PlayerVisual";
            graft.transform.localPosition = sourceVisual.localPosition;
            graft.transform.localRotation = sourceVisual.localRotation;
            graft.transform.localScale = sourceVisual.localScale;

            var bodyModel = MapComponent(sourceVisual, graft.transform, sourceEquipment.m_bodyModel);
            var animator = graft.GetComponentInChildren<Animator>(true);
            var leftHand = MapTransform(sourceVisual, graft.transform, sourceEquipment.m_leftHand);
            var rightHand = MapTransform(sourceVisual, graft.transform, sourceEquipment.m_rightHand);
            var helmet = MapTransform(sourceVisual, graft.transform, sourceEquipment.m_helmet);

            if (bodyModel == null || animator == null || leftHand == null
                || rightHand == null || helmet == null)
            {
                Object.DestroyImmediate(graft);
                Jotunn.Logger.LogWarning(
                    $"Hearth & Hird rejected an incomplete Player visual mapping for {settlerPrefab.name}; " +
                    "the compatibility visual will be retained");
                return false;
            }

            RemoveDevelopmentEffects(graft.transform);

            targetEquipment.m_bodyModel = bodyModel;
            targetEquipment.m_leftHand = leftHand;
            targetEquipment.m_rightHand = rightHand;
            targetEquipment.m_helmet = helmet;
            targetEquipment.m_backShield = MapTransform(sourceVisual, graft.transform, sourceEquipment.m_backShield);
            targetEquipment.m_backMelee = MapTransform(sourceVisual, graft.transform, sourceEquipment.m_backMelee);
            targetEquipment.m_backTwohandedMelee = MapTransform(sourceVisual, graft.transform, sourceEquipment.m_backTwohandedMelee);
            targetEquipment.m_backBow = MapTransform(sourceVisual, graft.transform, sourceEquipment.m_backBow);
            targetEquipment.m_backTool = MapTransform(sourceVisual, graft.transform, sourceEquipment.m_backTool);
            targetEquipment.m_backAtgeir = MapTransform(sourceVisual, graft.transform, sourceEquipment.m_backAtgeir);
            targetEquipment.m_clothColliders = MapComponents(
                sourceVisual, graft.transform, sourceEquipment.m_clothColliders);
            targetEquipment.m_models = sourceEquipment.m_models;
            targetEquipment.m_isPlayer = true;
            targetEquipment.m_useAllTrails = sourceEquipment.m_useAllTrails;
            targetEquipment.m_nViewOverride = null;

            var oldVisual = settlerPrefab.transform.Find("Visual");
            if (oldVisual != null && oldVisual != graft.transform)
            {
                Object.DestroyImmediate(oldVisual.gameObject);
            }
            graft.name = "Visual";

            Jotunn.Logger.LogInfo($"Applied vanilla Player visual to settlement NPC prefab {settlerPrefab.name}");
            return true;
        }

        private static void RemoveDevelopmentEffects(Transform graft)
        {
            var developmentEffects = graft.Find("DevEffects");
            if (developmentEffects != null)
            {
                Object.DestroyImmediate(developmentEffects.gameObject);
            }
        }

        private static Transform MapTransform(
            Transform sourceRoot,
            Transform targetRoot,
            Transform source)
        {
            if (source == null)
            {
                return null;
            }
            var path = RelativePath(sourceRoot, source);
            return path == null ? null : path.Length == 0 ? targetRoot : targetRoot.Find(path);
        }

        private static T MapComponent<T>(
            Transform sourceRoot,
            Transform targetRoot,
            T source) where T : Component
        {
            if (source == null)
            {
                return null;
            }
            var mapped = MapTransform(sourceRoot, targetRoot, source.transform);
            return mapped != null ? mapped.GetComponent<T>() : null;
        }

        private static T[] MapComponents<T>(
            Transform sourceRoot,
            Transform targetRoot,
            T[] source) where T : Component
        {
            if (source == null || source.Length == 0)
            {
                return new T[0];
            }
            var mapped = new List<T>(source.Length);
            foreach (var component in source)
            {
                var result = MapComponent(sourceRoot, targetRoot, component);
                if (result != null)
                {
                    mapped.Add(result);
                }
            }
            return mapped.ToArray();
        }

        private static string RelativePath(Transform root, Transform child)
        {
            if (root == null || child == null || (child != root && !child.IsChildOf(root)))
            {
                return null;
            }
            if (child == root)
            {
                return "";
            }

            var parts = new List<string>();
            for (var current = child; current != null && current != root; current = current.parent)
            {
                parts.Add(current.name);
            }
            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
