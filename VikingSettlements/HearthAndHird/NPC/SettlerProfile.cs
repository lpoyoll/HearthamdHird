using System;
using System.Collections.Generic;
using HearthAndHird.Network;
using UnityEngine;
using VikingSettlements.Npcs;

namespace HearthAndHird.NPC
{
    internal enum SettlerSex
    {
        Female = 0,
        Male = 1,
    }

    /// <summary>
    /// Persistent, deterministic identity and base aptitudes for a settler.
    /// Appearance values are stored now even though the compatibility prefab
    /// still uses a Dvergr visual; the later player-body renderer consumes the
    /// same values without changing a settler's identity.
    /// </summary>
    public sealed class SettlerProfile : MonoBehaviour
    {
        internal const int CurrentVersion = 1;

        private ZNetView _nview;

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
        }

        private void Start()
        {
            EnsureCreated();
        }

        internal SettlerSex Sex => (SettlerSex)GetInt(HearthZdoKeys.Sex, (int)SettlerSex.Female);
        internal int HairStyle => GetInt(HearthZdoKeys.HairStyle);
        internal int BeardStyle => GetInt(HearthZdoKeys.BeardStyle, -1);
        internal int SkinTone => GetInt(HearthZdoKeys.SkinTone, 50);
        internal int HairTone => GetInt(HearthZdoKeys.HairTone, 50);
        internal int HealthAptitude => GetInt(HearthZdoKeys.Health, 50);
        internal int StaminaAptitude => GetInt(HearthZdoKeys.Stamina, 50);
        internal int Strength => GetInt(HearthZdoKeys.Strength, 50);
        internal int Agility => GetInt(HearthZdoKeys.Agility, 50);
        internal int Courage => GetInt(HearthZdoKeys.Courage, 50);
        internal int WorkEthic => GetInt(HearthZdoKeys.WorkEthic, 50);
        internal int Loyalty => GetInt(HearthZdoKeys.Loyalty, 50);

        private int GetInt(string key, int fallback = 0)
        {
            return _nview != null && _nview.IsValid()
                ? _nview.GetZDO().GetInt(key, fallback)
                : fallback;
        }

        private void EnsureCreated()
        {
            if (_nview == null || !_nview.IsValid() || !_nview.IsOwner())
            {
                return;
            }

            var zdo = _nview.GetZDO();
            if (zdo.GetInt(HearthZdoKeys.ProfileVersion) >= CurrentVersion)
            {
                return;
            }

            var random = new System.Random(zdo.m_uid.GetHashCode());
            var sex = random.Next(0, 2) == 0 ? SettlerSex.Female : SettlerSex.Male;

            if (string.IsNullOrEmpty(zdo.GetString(SettlerIdentity.NameKey)))
            {
                zdo.Set(SettlerIdentity.NameKey, SettlerIdentity.GenerateName(zdo.m_uid));
            }

            zdo.Set(HearthZdoKeys.Sex, (int)sex);
            zdo.Set(HearthZdoKeys.HairStyle, random.Next(0, 12));
            zdo.Set(HearthZdoKeys.BeardStyle, sex == SettlerSex.Male ? random.Next(0, 9) : -1);
            zdo.Set(HearthZdoKeys.SkinTone, random.Next(20, 86));
            zdo.Set(HearthZdoKeys.HairTone, random.Next(10, 91));
            zdo.Set(HearthZdoKeys.Health, RollAptitude(random));
            zdo.Set(HearthZdoKeys.Stamina, RollAptitude(random));
            zdo.Set(HearthZdoKeys.Strength, RollAptitude(random));
            zdo.Set(HearthZdoKeys.Agility, RollAptitude(random));
            zdo.Set(HearthZdoKeys.Courage, RollAptitude(random));
            zdo.Set(HearthZdoKeys.WorkEthic, RollAptitude(random));
            zdo.Set(HearthZdoKeys.Loyalty, RollAptitude(random));
            zdo.Set(HearthZdoKeys.ProfileVersion, CurrentVersion);
        }

        private static int RollAptitude(System.Random random)
        {
            // Two rolls create a useful bell curve while retaining rare
            // unusually gifted or weak settlers.
            return Math.Min(100, Math.Max(1, 20 + random.Next(0, 41) + random.Next(0, 41)));
        }

        /// <summary>Adds a versioned profile payload to the party stow record.</summary>
        internal void AppendStowFields(List<string> fields)
        {
            fields.Add("P1");
            fields.Add(((int)Sex).ToString());
            fields.Add(HairStyle.ToString());
            fields.Add(BeardStyle.ToString());
            fields.Add(SkinTone.ToString());
            fields.Add(HairTone.ToString());
            fields.Add(HealthAptitude.ToString());
            fields.Add(StaminaAptitude.ToString());
            fields.Add(Strength.ToString());
            fields.Add(Agility.ToString());
            fields.Add(Courage.ToString());
            fields.Add(WorkEthic.ToString());
            fields.Add(Loyalty.ToString());
        }

        /// <summary>Restores a profile after party travel creates a new ZDO.</summary>
        internal static void RestoreStowFields(ZDO zdo, string[] fields, int offset)
        {
            if (zdo == null || fields == null || fields.Length < offset + 13
                || fields[offset] != "P1")
            {
                return; // records from before the profile foundation generate once on spawn
            }

            var keys = new[]
            {
                HearthZdoKeys.Sex,
                HearthZdoKeys.HairStyle,
                HearthZdoKeys.BeardStyle,
                HearthZdoKeys.SkinTone,
                HearthZdoKeys.HairTone,
                HearthZdoKeys.Health,
                HearthZdoKeys.Stamina,
                HearthZdoKeys.Strength,
                HearthZdoKeys.Agility,
                HearthZdoKeys.Courage,
                HearthZdoKeys.WorkEthic,
                HearthZdoKeys.Loyalty,
            };
            for (var i = 0; i < keys.Length; i++)
            {
                if (int.TryParse(fields[offset + 1 + i], out var value))
                {
                    zdo.Set(keys[i], value);
                }
            }
            zdo.Set(HearthZdoKeys.ProfileVersion, CurrentVersion);
        }
    }
}
