using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HearthAndHird.NPC
{
    /// <summary>Applies a persistent settler profile to a player-body visual.</summary>
    public sealed class SettlerAppearance : MonoBehaviour
    {
        private const float RetrySeconds = 1f;

        private SettlerProfile _profile;
        private VisEquipment _equipment;
        private ZNetView _nview;
        private float _nextAttempt;
        private bool _applied;

        private void Awake()
        {
            _profile = GetComponent<SettlerProfile>();
            _equipment = GetComponent<VisEquipment>();
            _nview = GetComponent<ZNetView>();
        }

        private void Start()
        {
            TryApply();
        }

        private void Update()
        {
            if (_applied || Time.time < _nextAttempt)
            {
                return;
            }
            TryApply();
        }

        private void TryApply()
        {
            _nextAttempt = Time.time + RetrySeconds;
            if (_profile == null || _equipment == null || !_equipment.m_isPlayer
                || _nview == null || !_nview.IsValid() || ObjectDB.instance == null)
            {
                return;
            }

            if (_nview.IsOwner())
            {
                _profile.EnsureCreated();
            }
            if (!_profile.IsReady)
            {
                return;
            }

            var hair = FindCustomization("Hair");
            var beards = FindCustomization("Beard");
            var model = _profile.Sex == SettlerSex.Male ? 0 : 1;

            _equipment.SetModel(model);
            _equipment.SetSkinColor(SkinColor(_profile.SkinTone));
            _equipment.SetHairColor(HairColor(_profile.HairTone));
            _equipment.SetHairItem(PickPrefabName(hair, _profile.HairStyle));
            _equipment.SetBeardItem(_profile.Sex == SettlerSex.Male
                ? PickPrefabName(beards, _profile.BeardStyle)
                : "");
            _applied = true;
        }

        private static List<ItemDrop> FindCustomization(string category)
        {
            return ObjectDB.instance
                .GetAllItems(ItemDrop.ItemData.ItemType.Customization, category)
                .Where(item => item != null && !item.name.Contains("_"))
                .OrderBy(item => item.m_itemData.m_shared.m_name, StringComparer.Ordinal)
                .ToList();
        }

        private static string PickPrefabName(IReadOnlyList<ItemDrop> choices, int index)
        {
            if (choices == null || choices.Count == 0 || index < 0)
            {
                return "";
            }
            return choices[index % choices.Count].gameObject.name;
        }

        private static Vector3 SkinColor(int tone)
        {
            var amount = Mathf.Clamp01(tone / 100f);
            var dark = new Color(0.25f, 0.10f, 0.055f);
            var light = new Color(1f, 0.72f, 0.52f);
            return Utils.ColorToVec3(Color.Lerp(dark, light, amount));
        }

        private static Vector3 HairColor(int tone)
        {
            var amount = Mathf.Clamp01(tone / 100f);
            var dark = new Color(0.06f, 0.035f, 0.02f);
            var light = new Color(0.78f, 0.58f, 0.28f);
            return Utils.ColorToVec3(Color.Lerp(dark, light, amount));
        }
    }
}
