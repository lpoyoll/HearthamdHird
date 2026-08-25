using System.Linq;
using HearthAndHird.AI;
using HearthAndHird.NPC;
using Jotunn.Entities;
using UnityEngine;
using VikingSettlements.Npcs;

namespace VikingSettlements.Commands
{
    /// <summary>Reports the persistent profile and directive of the nearest settler.</summary>
    internal sealed class InspectSettlerCommand : ConsoleCommand
    {
        private const float Range = 20f;

        public override string Name => "hnh_inspect";

        public override string Help =>
            "Shows the profile and current directive of the nearest settler within 20 metres.";

        public override void Run(string[] args)
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                Console.instance.Print("hnh_inspect: no local player, use this command in-game");
                return;
            }

            var profile = Object.FindObjectsOfType<SettlerProfile>()
                .Where(candidate => Vector3.Distance(player.transform.position, candidate.transform.position) <= Range)
                .OrderBy(candidate => Vector3.Distance(player.transform.position, candidate.transform.position))
                .FirstOrDefault();
            if (profile == null)
            {
                Console.instance.Print("hnh_inspect: no settler within 20 metres");
                return;
            }

            var character = profile.GetComponent<Character>();
            var recruitable = profile.GetComponent<SettlerRecruitable>();
            var directive = profile.GetComponent<SettlerDirectiveState>();
            var distance = Vector3.Distance(player.transform.position, profile.transform.position);
            var name = character != null ? character.m_name : profile.gameObject.name;
            var state = recruitable != null ? recruitable.State.ToString() : "Unknown";
            var job = recruitable != null ? recruitable.Job.ToString() : "Unknown";

            Console.instance.Print($"{name} — {profile.Sex}, {distance:0.0}m, {state}/{job}");
            Console.instance.Print(
                $"Appearance: hair {profile.HairStyle}, beard {profile.BeardStyle}, " +
                $"skin {profile.SkinTone}, hair tone {profile.HairTone}");
            Console.instance.Print(
                $"Aptitudes: health {profile.HealthAptitude}, stamina {profile.StaminaAptitude}, " +
                $"strength {profile.Strength}, agility {profile.Agility}");
            Console.instance.Print(
                $"Temperament: courage {profile.Courage}, work ethic {profile.WorkEthic}, " +
                $"loyalty {profile.Loyalty}");
            Console.instance.Print(directive != null
                ? $"Directive: {directive.Kind} r{directive.Revision}, work '{directive.WorkId}', " +
                  $"target {directive.Target}"
                : "Directive: unavailable");
        }
    }
}
