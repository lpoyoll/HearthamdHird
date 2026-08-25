using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;

namespace VikingSettlements
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class VikingSettlements : BaseUnityPlugin
    {
        public const string PluginGUID = "com.abjumb.vikingsettlements";
        public const string PluginName = "Hearth & Hird";
        public const string PluginVersion = "1.13.3";

        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private Harmony _harmony;

        private void Awake()
        {
            ModConfig.Init(Config);
            AddLocalizations();
            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            PrefabManager.OnVanillaPrefabsAvailable += CreatePrefabs;
            ZoneManager.OnVanillaLocationsAvailable += RegisterLocations;
            CommandManager.Instance.AddConsoleCommand(new Commands.SpawnSettlementCommand());
            CommandManager.Instance.AddConsoleCommand(new Commands.FindSettlementCommand());
            CommandManager.Instance.AddConsoleCommand(new Commands.PartyCommand());
            CommandManager.Instance.AddConsoleCommand(new Commands.InspectSettlerCommand());
            CommandManager.Instance.AddConsoleCommand(new Commands.HearthAndHirdTestCommand());

            Jotunn.Logger.LogInfo($"{PluginName} v{PluginVersion} loaded - settlements appear in newly generated world areas");
        }

        private void CreatePrefabs()
        {
            HearthAndHird.Hird.HirdHornItems.CreateAll();
            Npcs.SettlerPrefabs.CreateAll();
            Settlements.SettlementPieces.CreateAll();
            PrefabManager.OnVanillaPrefabsAvailable -= CreatePrefabs;
        }

        private void RegisterLocations()
        {
            World.SettlementLocations.RegisterAll();
        }

        private void Update()
        {
            // The random event system is recreated per game session; keep the
            // bandit raid registered in it.
            Raids.RaidEvents.EnsureRegistered();
            Party.PartySystem.OnUpdate();
            Npcs.SettlerTalkPanel.OnUpdate();
            Development.HearthAndHirdTestPanel.OnUpdate();
        }

        private void AddLocalizations()
        {
            Localization.AddTranslation("English", new Dictionary<string, string>
            {
                { "vs_settler", "Settler" },
                { "vs_seer", "Village Seer" },
                { "vs_trader", "Sigvald the Trader" },
                { "vs_raider", "Clanless Bandit" },
                { "vs_banner", "Hearthstone" },
                { "vs_banner_desc", "Founds a bed-backed settlement with a physical work radius. Upgrade it with biome materials to support a larger community." },
                { "hnh_hearthstone", "Hearthstone" },
                { "hnh_hearthstone_desc", "Founds a camp for up to 4 settlers, provided each has a bed. Use biome metals on it to upgrade its population and work radius." },
                { "hnh_hearth_camp", "Camp" },
                { "hnh_hearth_homestead", "Homestead" },
                { "hnh_hearth_hamlet", "Hamlet" },
                { "hnh_hearth_village", "Village" },
                { "hnh_hearth_hold", "Hold" },
                { "hnh_hearth_great_hold", "Great Hold" },
                { "hnh_hearth_jarl_seat", "Jarl's Seat" },
                { "hnh_beds", "Beds" },
                { "hnh_work_radius", "Work radius" },
                { "hnh_hearth_upgrade", "Upgrade with" },
                { "hnh_hearth_max", "The Hearthstone has reached Jarl's Seat" },
                { "hnh_hearth_need", "Missing upgrade materials:" },
                { "hnh_hearth_not_owner", "Only this Hearthstone's founder can manage it" },
                { "hnh_settler_not_owner", "This warrior belongs to another player" },
                { "hnh_no_owned_hearth", "No Hearthstone founded by you contains this position" },
                { "hnh_hearth_tier_full", "Upgrade the Hearthstone before settling another warrior" },
                { "hnh_hearth_need_bed", "Build another unclaimed bed before settling another warrior" },
                { "hnh_locate", "LOCATION" },
                { "hnh_locate_short", "Map" },
                { "hnh_status_here", "Here" },
                { "hnh_status_away", "Away" },
                { "hnh_location_marked", "Last known location marked on the map:" },
                { "vs_settlers", "Settlers" },
                { "vs_recruit", "Recruit" },
                { "vs_assign", "Assign to settlement" },
                { "vs_dismiss", "Dismiss" },
                { "vs_changejob", "Change job" },
                { "vs_unassign", "Unassign" },
                { "vs_following", "Following" },
                { "vs_joined", "joins you!" },
                { "vs_dismissed", "stays behind" },
                { "vs_assigned", "settles here!" },
                { "vs_needcoins", "Not enough coins" },
                { "vs_nosettlement", "No Hearthstone nearby" },
                { "vs_settlementfull", "This settlement is full" },
                { "vs_job_villager", "Villager" },
                { "vs_job_lumberjack", "Lumberjack" },
                { "vs_job_farmer", "Farmer" },
                { "vs_job_builder", "Builder" },
                { "vs_job_blacksmith", "Blacksmith" },
                { "vs_job_guard", "Guard" },
                { "vs_job_cook", "Cook" },
                { "vs_job_miner", "Miner" },
                { "vs_job_hunter", "Hunter" },
                { "vs_job_brewer", "Brewer" },
                { "vs_hungry", "Hungry" },
                { "vs_veteran", "Veteran" },
                { "vs_elite", "Elite" },
                { "vs_levelup", "has grown stronger!" },
                { "vs_manage", "Manage" },
                { "vs_rep", "Village standing" },
                { "vs_rep_honored", "Honored" },
                { "vs_rep_friendly", "Friendly" },
                { "vs_rep_neutral", "Neutral" },
                { "vs_rep_distrusted", "Distrusted" },
                { "vs_rep_hated", "Hated" },
                { "vs_rep_refuse", "They refuse to deal with you" },
                { "vs_donate", "Donate" },
                { "vs_donated", "The village appreciates your gift" },
                { "vs_rename", "Rename" },
                { "vs_rename_topic", "Name your settlement" },
                { "vs_close", "Close" },
                { "vs_nosettlers", "No settlers assigned yet — recruit warriors, build beds and press E inside the Hearthstone radius" },
                { "vs_col_settler", "SETTLER" },
                { "vs_col_job", "JOB" },
                { "vs_col_status", "STATUS" },
                { "vs_rank_settler", "Settler" },
                { "vs_status_working", "Working" },
                { "vs_status_hungry", "Hungry" },
                { "vs_panel_hint", "The register keeps the last known position of loaded and unloaded settlers" },
                { "vs_raid_start", "The clanless are raiding!" },
                { "vs_raid_end", "The clanless retreat" },
                { "vs_camp_totem", "Clanless War Totem" },
                { "vs_camp_totem_hint", "Destroy it to weaken the clanless raids" },
                { "vs_camp_cleared", "A clanless camp is broken! Their raids weaken" },
                { "vs_party_member", "Party" },
                { "vs_party_full", "Your party is full" },
                { "vs_party_waitcmd", "Wait here" },
                { "vs_party_followcmd", "With me" },
                { "vs_party_waits", "waits here" },
                { "vs_party_comes", "follows you" },
                { "vs_party_stance_follow", "Following" },
                { "vs_party_stance_hold", "Holding" },
                { "vs_party_stance_fallback", "Falling back" },
                { "vs_party_cmd_follow", "Party: with me!" },
                { "vs_party_cmd_hold", "Party: hold here!" },
                { "vs_party_cmd_fallback", "Party: fall back!" },
                { "vs_party_wounded", "is wounded!" },
                { "vs_party_grave", "is gravely wounded!" },
                { "vs_party_retreats", "is gravely wounded and retreats to you!" },
                { "vs_party_fallen", "has fallen" },
                { "vs_party_aboard", "Your party travels with you" },
                { "vs_party_ashore", "Your party regroups around you" },
                { "vs_joined_hint", "They fight at your side — settle them at your banner (E) to put them to work" },
                { "hnh_horn_required", "Craft and carry a Hird Horn before recruiting travelling companions" },
                { "hnh_hird_full", "Your Hird Horn cannot command any more warriors" },
                { "hnh_horn_no_hird", "Your hird is empty — horn capacity" },
                { "hnh_horn_no_target", "No enemy is under your crosshair" },
                { "hnh_horn_no_order_point", "No order point is under your crosshair" },
                { "hnh_order_move", "Hird: move there and hold!" },
                { "hnh_order_defend", "Hird: defend that ground!" },
                { "hnh_combat_stance", "Combat stance" },
                { "hnh_stance_passive", "Passive" },
                { "hnh_stance_defensive", "Defensive" },
                { "hnh_stance_aggressive", "Aggressive" },
                { "hnh_formation", "Formation" },
                { "hnh_formation_follow", "Follow" },
                { "hnh_formation_line", "Line" },
                { "hnh_formation_shieldwall", "Shield Wall" },
                { "hnh_formation_wedge", "Wedge" },
                { "hnh_formation_loose", "Loose" },
                { "hnh_formation_archers", "Archers Behind" },
                { "hnh_horn_crude", "Crude Hird Horn" },
                { "hnh_horn_crude_desc", "A rough horn for a young hird. Commands up to 2 followers. Use with Block/Crouch for battlefield orders; the Hird hotkeys change formation and combat stance." },
                { "hnh_horn_bronze", "Bronze-bound Hird Horn" },
                { "hnh_horn_bronze_desc", "A horn bound in bronze. Commands up to 3 followers. Use with Block/Crouch for battlefield orders; the Hird hotkeys change formation and combat stance." },
                { "hnh_horn_iron", "Iron-bound Hird Horn" },
                { "hnh_horn_iron_desc", "A hard-wearing iron horn. Commands up to 4 followers. Use with Block/Crouch for battlefield orders; the Hird hotkeys change formation and combat stance." },
                { "hnh_horn_silver", "Silver-bound Hird Horn" },
                { "hnh_horn_silver_desc", "A clear-voiced silver horn. Commands up to 6 followers. Use with Block/Crouch for battlefield orders; the Hird hotkeys change formation and combat stance." },
                { "hnh_horn_blackmetal", "Blackmetal Hird Horn" },
                { "hnh_horn_blackmetal_desc", "A war horn fit for the plains. Commands up to 8 followers. Use with Block/Crouch for battlefield orders; the Hird hotkeys change formation and combat stance." },
                { "hnh_horn_eitr", "Eitr-carved Hird Horn" },
                { "hnh_horn_eitr_desc", "A horn traced with refined eitr. Commands up to 10 followers. Use with Block/Crouch for battlefield orders; the Hird hotkeys change formation and combat stance." },
                { "hnh_horn_flametal", "Flametal Hird Horn" },
                { "hnh_horn_flametal_desc", "A jarl's horn bound in flametal. Commands up to 12 followers. Use with Block/Crouch for battlefield orders; the Hird hotkeys change formation and combat stance." },
                { "vs_talk_wild", "A free villager" },
                { "vs_talk_party", "In your party" },
                { "vs_talk_party_hint", "Settle them at your settlement banner to give them a job" },
                { "vs_talk_health", "Health" },
                { "vs_talk_fed", "Well fed" },
                { "vs_talk_hungry", "Hungry — nothing to eat! Put food in a settlement chest" },
                { "vs_talk_nextmeal", "next meal in" },
                { "vs_talk_needs", "What I need" },
                { "vs_talk_villager_none", "No duties — press E on me to assign a job" },
                { "vs_talk_guard_none", "I keep watch. I need nothing more" },
                { "vs_need_chest", "A chest with room in the settlement" },
                { "vs_need_food", "Food in a settlement chest" },
                { "vs_need_workbench", "A workbench in the settlement" },
                { "vs_need_forge", "A forge in the settlement" },
                { "vs_need_cookstation", "A cooking station in the settlement" },
                { "vs_need_fermenter", "A fermenter in the settlement" },
                { "vs_need_beehive", "A beehive, for honey" },
                { "vs_need_ore", "Copper/tin ore or iron scraps in one chest" },
                { "vs_need_rawfood", "Raw meat or fish in one chest" },
                { "vs_need_brewing", "2 honey or 2 barley together in one chest" },
                { "vs_need_damage", "Damaged structures to repair" },
                { "vs_talk_g1", "What do you need, chief?" },
                { "vs_talk_g2", "The fire is warm tonight." },
                { "vs_talk_g3", "Odin watches over us." },
                { "vs_talk_g4", "Good hunting lately, eh?" },
                { "vs_buildchest", "Builders' Supply Chest" },
                { "vs_buildchest_desc", "Builders draw construction materials from this chest for their assigned projects. Lumberjacks and miners refill it when an active project runs low." },
                { "vs_site", "Construction site" },
                { "vs_bp_cabin", "Cabin" },
                { "vs_bp_longhouse", "Longhouse" },
                { "vs_bp_watchtower", "Watchtower" },
                { "vs_bp_started", "The builders mark out a construction site" },
                { "vs_bp_busy", "The settlement already has an active construction site" },
                { "vs_bp_outside", "Too far from the settlement banner — stand where the building should go" },
                { "vs_bp_cancel", "Cancel project" },
                { "vs_bp_canceled", "Construction canceled" },
                { "vs_bp_complete", "The builders have finished" },
                { "vs_bp_needsbuilder", "Waiting for a builder" },
                { "vs_supply_low", "The builders are out of materials! Fill the supply chest" },
                { "vs_talk_build", "What should we build?" },
                { "vs_talk_project", "Current project" },
                { "vs_talk_home", "Has a home" },
                { "vs_talk_homeless", "No home — working slower (talk key on a door)" },
                { "vs_need_supplies", "Materials in the supply chest" },
                { "vs_home_title", "Who lives here?" },
                { "vs_home_occupant", "lives here" },
                { "vs_home_assign", "Move in" },
                { "vs_home_unassign", "Move out" },
                { "vs_home_nosettlement", "This door is not inside a settlement" },
                { "vs_tier1", "Hamlet" },
                { "vs_tier2", "Village" },
                { "vs_tier3", "Town" },
                { "vs_promoted", "has grown into a" },
                { "vs_bp_greathall", "Stone Great-Hall" },
                { "vs_bp_locked", "Higher settlement tiers unlock more blueprints" },
                { "vs_warlord", "Clanless Warlord" },
                { "vs_warlord_comes", "A clanless warlord marches on your settlement!" },
                { "vs_warlord_slain", "The warlord has fallen! The clanless grant this land peace" },
                { "vs_gear", "Equipment" },
                { "vs_gear_give", "Give from your inventory" },
                { "vs_gear_givebtn", "Give" },
                { "vs_gear_return", "Take back" },
                { "vs_gear_none", "—" },
                { "vs_gear_nothing", "Nothing equippable in your inventory" },
                { "vs_inv_full", "No room in your inventory" },
                { "vs_slot_weapon", "Weapon" },
                { "vs_slot_shield", "Shield" },
                { "vs_slot_helmet", "Helmet" },
                { "vs_slot_chest", "Chest" },
                { "vs_slot_legs", "Legs" },
                { "vs_job_courier", "Courier" },
                { "vs_job_herder", "Herder" },
                { "vs_bp_pen", "Livestock Pen" },
                { "vs_talk_mood", "Mood" },
                { "vs_mood_cheerful", "Cheerful — working extra hard" },
                { "vs_mood_content", "Content" },
                { "vs_mood_unhappy", "Unhappy" },
                { "vs_mood_miserable", "Miserable — barely working" },
                { "vs_mood_left", "has had enough and left the settlement!" },
                { "vs_need_dest", "Another settlement within courier range" },
                { "vs_need_surplus", "Surplus goods to haul (more than 10 of something)" },
                { "vs_need_animals", "Tamed animals in the settlement" },
                { "vs_need_feed", "Carrots or turnips in a chest, for feed" },
                { "vs_courier_ambush", "A courier is ambushed on the road!" },
                { "vs_job_engineer", "Engineer" },
                { "vs_ballista", "Settlement Ballista" },
                { "vs_bp_palisade", "Palisade Ring" },
                { "vs_bp_ballista", "Ballista Tower" },
                { "vs_need_ballista", "A ballista tower to keep loaded" },
                { "vs_need_boltwood", "Wood in a chest, for fletching bolts" },
                { "vs_clan0", "The Clanless" },
                { "vs_clan1", "The Ashwolves" },
                { "vs_clan2", "The Saltborn" },
                { "vs_clan3", "The Blood Ravens" },
                { "vs_clan4", "The Grey Hounds" },
                { "vs_clan5", "The Oathbreakers" },
                { "vs_clan6", "The Night Axes" },
                { "vs_clan7", "The Rime Serpents" },
                { "vs_clan8", "The Broken Shields" },
                { "vs_clan_attack", "are attacking the settlement!" },
                { "vs_clan_shattered", "are broken — their raids on this land are over" },
                { "vs_clan_broken_note", "clan broken" },
                { "vs_abducted", "has been carried off by the clanless! Destroy their camp's war totem to free them" },
                { "vs_captive", "Captive" },
                { "vs_captive_days", "days to save them" },
                { "vs_rescued", "is free and comes home!" },
                { "vs_captive_lost", "will never come home. The settlement mourns" },
                { "vs_job_innkeeper", "Innkeeper" },
                { "vs_job_fisher", "Fisher" },
                { "vs_bp_meadhall", "Mead Hall" },
                { "vs_bp_dock", "Fishing Dock" },
                { "vs_hallbanner", "Mead Hall Banner" },
                { "vs_need_meadhall", "A mead hall in the settlement" },
                { "vs_need_mead", "A mead or barley wine in a chest" },
                { "vs_need_water", "Open water at the settlement's edge" },
                { "vs_feast", "The innkeeper pours a round — spirits lift, and you feel rested" },
                { "vs_and", "and" },
                { "vs_wedding", "have married!" },
                { "vs_grief", "mourns the loss of" },
                { "vs_talk_married", "Married to" },
                { "vs_child", "A child of the settlement has come of age and joins you!" },
                { "vs_seer_warning", "The seer senses a war party gathering — they strike tomorrow night!" },
                { "vs_bounty_board", "Village Bounty Board" },
                { "vs_bounty_none", "No postings today" },
                { "vs_bounty_use", "Check the postings" },
                { "vs_bounty_camp_txt", "Break the war totem of" },
                { "vs_bounty_deliver", "Bring the village" },
                { "vs_bounty_reward", "Reward" },
                { "vs_bounty_new", "The village posts a bounty" },
                { "vs_bounty_notdone", "The task is not yet done" },
                { "vs_bounty_done", "The village is grateful!" },
                { "vs_bounty_tomorrow", "Nothing new today — check back tomorrow" },
                { "vs_bounty_needitems", "You don't have the goods with you" },
                { "vs_saga", "Saga" },
                { "vs_saga_title", "The Settlement Saga" },
                { "vs_saga_empty", "No deeds recorded yet — the saga writes itself" },
                { "vs_saga_day", "Day" },
                { "vs_saga_raid", "Raided by" },
                { "vs_saga_warlord", "warlord slain beneath these walls" },
                { "vs_saga_wed", "were wed" },
                { "vs_saga_taken", "was carried off by raiders" },
                { "vs_saga_rescued", "was rescued from the clanless" },
                { "vs_saga_lost", "was lost to the clanless" },
                { "vs_saga_fell", "fell defending the settlement" },
                { "vs_saga_promoted", "The settlement grew into a" },
                { "vs_ep1", "the Unbroken" },
                { "vs_ep2", "the Steadfast" },
                { "vs_ep3", "Shieldheart" },
                { "vs_ep4", "the Grim" },
                { "vs_ep5", "Ravenfriend" },
                { "vs_ep6", "Stormstood" },
                { "vs_rally", "Rally Standard" },
                { "vs_rally_desc", "Plant it where the line must hold. Press E and your war party rallies to the standard and fights there; Shift+E (or your stance key) calls them back to your side." },
                { "vs_rally_order", "Party! To the standard!" },
                { "vs_rally_order_hint", "Rally the party here" },
                { "vs_rally_release_hint", "Release them to your side" },
                { "vs_party_none", "No party members nearby" },
                { "vs_party_focus", "Party: strike the" },
            });
        }
    }
}
