using UnityEngine;

namespace VikingSettlements.Npcs
{
    /// <summary>
    /// Gives each settler instance a persistent personal name, derived
    /// deterministically from its network id so every client sees the same
    /// name without any extra synchronization. The name is also written to
    /// the ZDO so it survives the party system's stow/respawn cycle, where
    /// the network id changes.
    /// </summary>
    public class SettlerIdentity : MonoBehaviour
    {
        public const string NameKey = "vs_settlername";

        private static readonly string[] Names =
        {
            "Astrid", "Bjorn", "Dagny", "Eirik", "Freydis", "Gunnar",
            "Helga", "Ingolf", "Jorunn", "Kettil", "Leif", "Magnhild",
            "Njal", "Oddny", "Ragnar", "Signy", "Torstein", "Ulfhild",
            "Vigdis", "Yrsa", "Arnbjorg", "Halvar", "Solveig", "Sten",
        };

        internal static string GenerateName(ZDOID id)
        {
            var index = (int)((uint)id.GetHashCode() % (uint)Names.Length);
            return Names[index];
        }

        private void Start()
        {
            var character = GetComponent<Character>();
            var view = GetComponent<ZNetView>();
            if (character == null || view == null || !view.IsValid())
            {
                return;
            }

            var stored = view.GetZDO().GetString(NameKey);
            if (!string.IsNullOrEmpty(stored))
            {
                character.m_name = stored;
                return;
            }

            character.m_name = GenerateName(view.GetZDO().m_uid);
            if (view.IsOwner())
            {
                view.GetZDO().Set(NameKey, character.m_name);
            }
        }
    }
}
