using VikingSettlements.Npcs;
using UnityEngine;

namespace VikingSettlements.World
{
    /// <summary>
    /// The concrete settlement blueprints, assembled from vanilla building
    /// pieces. Positions use Valheim's 2m build grid; walls sit on y=0 with
    /// their bottom edge, floors are centered on their tile.
    /// </summary>
    internal static class Layouts
    {
        /// <summary>A 4x4m flat-roofed cabin. Door faces +Z.</summary>
        private static SettlementLayout Cabin(string chest = "piece_chest_wood")
        {
            var l = new SettlementLayout("cabin");

            // Four floor tiles.
            foreach (var x in new[] { -1f, 1f })
            {
                foreach (var z in new[] { -1f, 1f })
                {
                    l.Add("wood_floor", x, 0f, z);
                }
            }

            // Back wall.
            l.Add("wood_wall_full", -1f, 0f, -2f);
            l.Add("wood_wall_full", 1f, 0f, -2f);
            // Side walls.
            l.Add("wood_wall_full", -2f, 0f, -1f, 90f);
            l.Add("wood_wall_full", -2f, 0f, 1f, 90f);
            l.Add("wood_wall_full", 2f, 0f, -1f, 90f);
            l.Add("wood_wall_full", 2f, 0f, 1f, 90f);
            // Front wall with door opening.
            l.Add("wood_wall_full", -1f, 0f, 2f);
            l.Add("wood_door", 1f, 0f, 2f);

            // A real weatherproof roof rather than the old floating floor.
            foreach (var z in new[] { -1f, 1f })
            {
                l.Add("wood_roof_45", -2f, 2f, z, 90f);
                l.Add("wood_roof_45", 2f, 2f, z, 270f);
                l.Add("wood_roof_top_45", 0f, 4f, z, 90f);
            }
            foreach (var zEnd in new[] { -2f, 2f })
            {
                var facing = zEnd < 0f ? 0f : 180f;
                l.Add("wood_wall_roof_45", -1f, 2f, zEnd, facing);
                l.Add("wood_wall_roof_45", 1f, 2f, zEnd, facing);
                l.Add("wood_wall_roof_top_45", 0f, 4f, zEnd, facing);
            }

            l.Add("bed", -1f, 0f, -1f);
            if (chest != null)
            {
                l.Add(chest, 1.2f, 0f, -1.5f);
            }
            return l;
        }

        /// <summary>
        /// A 6x10m hall with a 45 degree roof. Long axis along Z, door at +Z.
        /// Wall material is parameterized so the plains variant can use stone.
        /// </summary>
        private static SettlementLayout Hall(string wall, bool oneMeterCourses, string chest)
        {
            var l = new SettlementLayout("hall");
            var xTiles = new[] { -2f, 0f, 2f };
            var zTiles = new[] { -4f, -2f, 0f, 2f, 4f };

            foreach (var x in xTiles)
            {
                foreach (var z in zTiles)
                {
                    l.Add("wood_floor", x, 0f, z);
                }
            }

            // Stone wall pieces are 2x1m, so stone walls need two courses to
            // reach the 2m eaves height of a wooden wall.
            var courses = oneMeterCourses ? new[] { 0f, 1f } : new[] { 0f };

            foreach (var y in courses)
            {
                // Long side walls.
                foreach (var z in zTiles)
                {
                    l.Add(wall, -3f, y, z, 90f);
                    l.Add(wall, 3f, y, z, 90f);
                }
                // Back gable wall.
                foreach (var x in xTiles)
                {
                    l.Add(wall, x, y, -5f);
                }
                // Front wall leaves a 2m door opening in the middle.
                l.Add(wall, -2f, y, 5f);
                l.Add(wall, 2f, y, 5f);
            }
            l.Add("wood_door", 0f, 0f, 5f);

            // 45 degree roof: eaves on the wall tops, ridge along the Z axis.
            foreach (var z in zTiles)
            {
                l.Add("wood_roof_45", -3f, 2f, z, 90f);
                l.Add("wood_roof_45", 3f, 2f, z, 270f);
                l.Add("wood_roof_top_45", 0f, 4f, z, 90f);
            }
            // Gable in-fills above the short walls.
            foreach (var zEnd in new[] { -5f, 5f })
            {
                var facing = zEnd < 0f ? 0f : 180f;
                l.Add("wood_wall_roof_45", -2f, 2f, zEnd, facing);
                l.Add("wood_wall_roof_45", 2f, 2f, zEnd, facing);
                l.Add("wood_wall_roof_top_45", 0f, 4f, zEnd, facing);
            }

            // Furnishings.
            l.Add("piece_table", 0f, 0f, -1f);
            l.Add("piece_chair", 1.2f, 0f, -1f, 270f);
            l.Add("piece_chair", -1.2f, 0f, -1f, 90f);
            l.Add("bed", -2f, 0f, -4f);
            l.Add("bed", 2f, 0f, -4f);
            if (chest != null)
            {
                l.Add(chest, 2f, 0f, -2f, 90f);
            }
            return l;
        }

        /// <summary>An open 2x2m watch platform on 6m poles with a torch.</summary>
        private static SettlementLayout Watchtower()
        {
            var l = new SettlementLayout("watchtower");
            foreach (var x in new[] { -1f, 1f })
            {
                foreach (var z in new[] { -1f, 1f })
                {
                    l.Add("wood_pole2", x, 0f, z);
                    l.Add("wood_pole2", x, 2f, z);
                    l.Add("wood_pole2", x, 4f, z);
                }
            }
            l.Add("wood_floor", 0f, 6f, 0f);
            l.Add("wood_fence", 0f, 6f, 1f);
            l.Add("wood_fence", 0f, 6f, -1f);
            l.Add("wood_fence", 1f, 6f, 0f, 90f);
            l.Add("wood_fence", -1f, 6f, 0f, 90f);
            l.Add("piece_groundtorch_wood", 0f, 6f, 0f);
            l.Add("wood_ladder", 0f, 0f, 1.15f, 180f);
            return l;
        }

        /// <summary>A fenced 6x4m field with crops. Gate at +Z.</summary>
        private static SettlementLayout Farm(string cropA, string cropB)
        {
            var l = new SettlementLayout("farm");
            foreach (var x in new[] { -2f, 0f, 2f })
            {
                l.Add("wood_fence", x, 0f, -2f);
            }
            l.Add("wood_fence", -2f, 0f, 2f);
            l.Add("wood_fence", 2f, 0f, 2f);
            l.Add("wood_gate", 0f, 0f, 2f);
            foreach (var z in new[] { -1f, 1f })
            {
                l.Add("wood_fence", -3f, 0f, z, 90f);
                l.Add("wood_fence", 3f, 0f, z, 90f);
            }
            foreach (var x in new[] { -2f, 0f, 2f })
            {
                l.Add(cropA, x, 0f, -1f);
                l.Add(cropB, x, 0f, 1f);
            }
            l.Add("piece_beehive", 3.8f, 0f, 2.8f);
            return l;
        }

        /// <summary>A 4x4m open market stall with the village trader.</summary>
        private static SettlementLayout TraderStall()
        {
            var l = new SettlementLayout("stall");
            foreach (var x in new[] { -2f, 2f })
            {
                foreach (var z in new[] { -2f, 2f })
                {
                    l.Add("wood_pole2", x, 0f, z);
                }
            }
            foreach (var x in new[] { -1f, 1f })
            {
                foreach (var z in new[] { -1f, 1f })
                {
                    l.Add("wood_floor", x, 2f, z);
                }
            }
            l.Add(SettlerPrefabs.Trader, 0f, 0f, 0f, 180f);
            l.Add("piece_chest_wood", 1.2f, 0f, -1.2f);
            l.Add("piece_banner01", -2f, 0f, 2f);
            return l;
        }

        /// <summary>The central fire pit with seating.</summary>
        private static SettlementLayout FirePlaza()
        {
            var l = new SettlementLayout("plaza");
            l.Add("fire_pit", 0f, 0f, 0f);
            l.Add("piece_chair", 1.5f, 0f, 0f, 270f);
            l.Add("piece_chair", -1.5f, 0f, 0f, 90f);
            l.Add("piece_chair", 0f, 0f, 1.5f, 180f);
            l.Add("wood_stack", 2.5f, 0f, 1.8f);
            return l;
        }

        /// <summary>Ring of defensive stakes with entrance gaps.</summary>
        private static void AddStakeRing(SettlementLayout l, float radius, int segments, float gapDegrees)
        {
            for (var i = 0; i < segments; i++)
            {
                var angle = 360f * i / segments;
                // Leave an opening around +Z for the entrance.
                if (angle < gapDegrees / 2f || angle > 360f - gapDegrees / 2f)
                {
                    continue;
                }
                var rad = angle * UnityEngine.Mathf.Deg2Rad;
                var x = radius * UnityEngine.Mathf.Sin(rad);
                var z = radius * UnityEngine.Mathf.Cos(rad);
                l.AddGrounded("sharp_stakes", x, 0f, z, angle + 90f);
            }
        }

        // The meadows structures double as the blueprints players can hand
        // to their builders (no NPCs, no terrain ops, plain storage chests).

        public static SettlementLayout BlueprintCabin() => Cabin();

        public static SettlementLayout BlueprintLonghouse() => Hall("wood_wall_full", false, "piece_chest_wood");

        public static SettlementLayout BlueprintGreatHall() => Hall("stone_wall_2x1", true, "piece_chest_wood");

        /// <summary>A fenced 6x4m livestock pen with two tame boars. Gate at +Z.</summary>
        public static SettlementLayout BlueprintPen()
        {
            var l = new SettlementLayout("pen");
            foreach (var x in new[] { -2f, 0f, 2f })
            {
                l.Add("wood_fence", x, 0f, -2f);
            }
            l.Add("wood_fence", -2f, 0f, 2f);
            l.Add("wood_fence", 2f, 0f, 2f);
            l.Add("wood_gate", 0f, 0f, 2f);
            foreach (var z in new[] { -1f, 1f })
            {
                l.Add("wood_fence", -3f, 0f, z, 90f);
                l.Add("wood_fence", 3f, 0f, z, 90f);
            }
            l.Add(SettlerPrefabs.PenBoar, -0.9f, 0f, 0f, 70f);
            l.Add(SettlerPrefabs.PenBoar, 0.9f, 0f, -0.6f, 250f);
            return l;
        }

        public static SettlementLayout BlueprintWatchtower() => Watchtower();

        /// <summary>
        /// A defensive stake ring, 10m radius, with a gate and torches at the
        /// opening. Placed around whatever the player stands in front of.
        /// </summary>
        public static SettlementLayout BlueprintPalisade()
        {
            var l = new SettlementLayout("palisade");
            AddStakeRing(l, 10f, 22, 26f);
            l.Add("wood_gate", 0f, 0f, 10f);
            l.Add("piece_groundtorch_wood", -1.8f, 0f, 9.4f);
            l.Add("piece_groundtorch_wood", 1.8f, 0f, 9.4f);
            return l;
        }

        /// <summary>
        /// The longhouse dressed as a common room: extra seating, a fermenter
        /// for the brewer, and the hall banner the Innkeeper job gates on.
        /// </summary>
        public static SettlementLayout BlueprintMeadHall()
        {
            var l = Hall("wood_wall_full", false, "piece_chest_wood");
            l.Add("piece_table", 0f, 0f, 2f);
            l.Add("piece_chair", 1.2f, 0f, 2f, 270f);
            l.Add("piece_chair", -1.2f, 0f, 2f, 90f);
            l.Add("fermenter", -2f, 0f, -2f);
            l.Add(SettlerPrefabs.HallBanner, -2.2f, 0f, -3.5f);
            l.Add(SettlerPrefabs.HallBanner, 2.2f, 0f, -3.5f);
            return l;
        }

        /// <summary>A single-file pier running out over the water, torch at the end.</summary>
        public static SettlementLayout BlueprintDock()
        {
            var l = new SettlementLayout("dock");
            foreach (var z in new[] { 1f, 3f, 5f, 7f })
            {
                l.Add("wood_floor", 0f, 0f, z);
                l.Add("wood_fence", -1f, 0f, z, 90f);
                l.Add("wood_fence", 1f, 0f, z, 90f);
            }
            l.Add("wood_pole2", -1f, -2f, 7f);
            l.Add("wood_pole2", 1f, -2f, 7f);
            l.Add("piece_groundtorch_wood", 0f, 0f, 7f);
            l.Add("piece_chest_wood", 0f, 0f, 1f, 90f);
            return l;
        }

        /// <summary>The watchtower platform crowned with a settlement ballista.</summary>
        public static SettlementLayout BlueprintBallistaTower()
        {
            var l = new SettlementLayout("ballistatower");
            foreach (var x in new[] { -1f, 1f })
            {
                foreach (var z in new[] { -1f, 1f })
                {
                    l.Add("wood_pole2", x, 0f, z);
                    l.Add("wood_pole2", x, 2f, z);
                    l.Add("wood_pole2", x, 4f, z);
                }
            }
            l.Add("wood_floor", 0f, 6f, 0f);
            l.Add("wood_fence", 0f, 6f, 1f);
            l.Add("wood_fence", 0f, 6f, -1f);
            l.Add("wood_fence", 1f, 6f, 0f, 90f);
            l.Add("wood_fence", -1f, 6f, 0f, 90f);
            l.Add(SettlerPrefabs.Ballista, 0f, 6f, 0f);
            return l;
        }

        /// <summary>
        /// The large meadows village: fire plaza, three cabins, a longhouse,
        /// a farm, a trader stall, a watchtower and eight villagers.
        /// </summary>
        public static SettlementLayout MeadowsVillage()
        {
            var v = new SettlementLayout(SettlementLocations.MeadowsVillageLocation);
            v.Add(SettlerPrefabs.Heart, 0f, 0f, 0f);
            // A single op sized to the whole footprint: overlapping ops
            // re-slope each other's leveled ground and terrace the site.
            v.Add(SettlerPrefabs.FlattenVillage, 0f, 0f, 0f);

            v.Place(FirePlaza(), 0f, 0f);
            v.Add("piece_maypole", 4f, 0f, -3f);

            // Cabins around the plaza, doors facing the center.
            v.Place(Cabin(), 0f, -10f, 0f);
            v.Place(Cabin(), -10f, 0f, 90f);
            v.Place(Cabin(null), 10f, 0f, 270f);

            // Longhouse north of the plaza, door facing back to the center.
            v.Place(Hall("wood_wall_full", false, "TreasureChest_meadows"), 0f, 13f, 180f);

            v.Place(Farm("Pickable_Carrot", "Pickable_Turnip"), -11f, 10f, 0f);
            v.Place(TraderStall(), 10f, 9f, 225f);
            v.Place(Watchtower(), 10f, -9f);
            // The bounty board hangs on a post by the plaza.
            v.Add("wood_pole2", 5.5f, 0f, 2.5f);
            v.Add(SettlerPrefabs.BountyBoard, 5.5f, 1.4f, 2.25f, 180f);

            foreach (var pos in new[]
            {
                (x: 2.5f, z: 2.5f), (x: -2.5f, z: 3f), (x: -3f, z: -2.5f), (x: 3f, z: -3.5f),
            })
            {
                v.Add("piece_groundtorch_wood", pos.x, 0f, pos.z);
            }

            // The villagers.
            v.Add(SettlerPrefabs.Settler, 1.2f, 0f, -1.2f, 45f);
            v.Add(SettlerPrefabs.Settler, -2f, 0f, 1f, 135f);
            v.Add(SettlerPrefabs.Settler, 0f, 0f, -7f);
            v.Add(SettlerPrefabs.Settler, -7f, 0f, 1f, 90f);
            v.Add(SettlerPrefabs.Settler, 7f, 0f, -7f, 315f);
            v.Add(SettlerPrefabs.Settler, -9f, 0f, 8f);
            v.Add(SettlerPrefabs.Seer, 0f, 0f, 7f, 180f);
            return v;
        }

        /// <summary>
        /// Development settlement placed near the world start by the F7
        /// muster. It extends the normal Meadows village to sixteen neutral
        /// residents so combat, reputation and crowd behaviour can be tested.
        /// </summary>
        public static SettlementLayout NeutralStartVillage()
        {
            var village = MeadowsVillage();
            village.Place(Cabin(), -15f, -13f, 45f);
            village.Place(Cabin(), 16f, -15f, 315f);
            village.Place(Cabin(), -17f, 0f, 90f);

            foreach (var settler in new[]
            {
                (x: -14f, z: -9f, rot: 60f),
                (x: -10f, z: -13f, rot: 25f),
                (x: -5f, z: -12f, rot: 350f),
                (x: 5f, z: -13f, rot: 190f),
                (x: 11f, z: -14f, rot: 220f),
                (x: 15f, z: -9f, rot: 270f),
                (x: -14f, z: 5f, rot: 90f),
                (x: 14f, z: 4f, rot: 270f),
                (x: 5f, z: 6f, rot: 180f),
            })
            {
                village.Add(SettlerPrefabs.Settler, settler.x, 0f, settler.z, settler.rot);
            }
            return village;
        }

        /// <summary>
        /// Curated development settlements used by F7. Buildings grow in
        /// irregular rings around a communal centre, face their paths and are
        /// grounded as complete modules by LayoutBuilder.
        /// </summary>
        public static SettlementLayout DevelopmentSettlement(WildSettlementTier tier)
        {
            var l = new SettlementLayout("HearthAndHird_" + VillageHeart.TierDisplay(tier));
            l.AddGrounded(SettlerPrefabs.Heart, 0f, 0f, 0f);
            l.Add(tier <= WildSettlementTier.Homestead
                ? SettlerPrefabs.FlattenOutpost
                : SettlerPrefabs.FlattenVillage, 0f, 0f, 0f);
            l.Place(FirePlaza(), 0f, 0f);
            l.AddGrounded("piece_maypole", 3.8f, 0f, -2.8f);

            var houseAnchors = new[]
            {
                (x: -7f, z: -8f), (x: 8f, z: -7f),
                (x: -11f, z: 2f), (x: 11f, z: 4f),
                (x: -9f, z: 14f), (x: 9f, z: 15f),
                (x: -18f, z: -8f), (x: 18f, z: -6f),
                (x: -19f, z: 7f), (x: 20f, z: 9f),
                (x: -16f, z: 22f), (x: 16f, z: 23f),
                (x: -27f, z: -3f), (x: 28f, z: -1f),
                (x: -25f, z: 16f), (x: 26f, z: 18f),
            };
            var houseCounts = new[] { 2, 3, 4, 6, 8, 12, 16 };
            var houseCount = houseCounts[(int)tier];
            for (var i = 0; i < houseCount; i++)
            {
                var anchor = houseAnchors[i];
                l.Place(Cabin(), anchor.x, anchor.z, FacingCentre(anchor.x, anchor.z));
                AddPath(l, anchor.x, anchor.z);
            }

            if (tier >= WildSettlementTier.Homestead)
            {
                var hallZ = tier >= WildSettlementTier.Hold ? 30f : 23f;
                var hall = tier >= WildSettlementTier.Hold
                    ? Hall("stone_wall_2x1", true, "piece_chest_wood")
                    : Hall("wood_wall_full", false, "piece_chest_wood");
                l.Place(hall, 0f, hallZ, 180f);
                AddPath(l, 0f, hallZ - 5f);
            }
            if (tier >= WildSettlementTier.Hamlet)
            {
                l.Place(Farm("Pickable_Carrot", "Pickable_Turnip"), -17f, 18f, 35f);
                l.AddGrounded("piece_workbench", 15f, 0f, 15f, 225f);
                l.AddGrounded("wood_stack", 17f, 0f, 13f);
            }
            if (tier >= WildSettlementTier.Village)
            {
                l.Place(BlueprintMeadHall(), 13f, 26f, 205f);
                l.Place(TraderStall(), -15f, 28f, 155f);
                l.Place(Watchtower(), -23f, -13f, 35f);
                l.Place(Watchtower(), 23f, -12f, 325f);
                AddStakeRing(l, VillageHeart.FootprintForTier(tier),
                    22 + (int)tier * 5, 34f);
            }
            if (tier >= WildSettlementTier.Hold)
            {
                l.AddGrounded("forge", 22f, 0f, 18f, 240f);
                l.AddGrounded("smelter", 25f, 0f, 15f, 250f);
                l.AddGrounded("charcoal_kiln", 26f, 0f, 21f, 230f);
                l.Place(Farm("Pickable_Carrot", "Pickable_Turnip"), -27f, 23f, 45f);
                l.Place(Watchtower(), 0f, -VillageHeart.FootprintForTier(tier) + 3f);
            }
            if (tier >= WildSettlementTier.GreatHold)
            {
                l.Place(BlueprintMeadHall(), -17f, 34f, 160f);
                l.Place(Watchtower(), -VillageHeart.FootprintForTier(tier) + 3f, 8f, 90f);
                l.Place(Watchtower(), VillageHeart.FootprintForTier(tier) - 3f, 8f, 270f);
            }

            var population = VillageHeart.PopulationForTier(tier);
            var hasSeer = tier >= WildSettlementTier.Hamlet;
            for (var i = 0; i < population; i++)
            {
                var ring = i / 12;
                var slot = i % 12;
                var angle = slot * 30f + ring * 11f;
                var radius = 4.5f + ring * 4.8f;
                var radians = angle * Mathf.Deg2Rad;
                var x = Mathf.Sin(radians) * radius;
                var z = Mathf.Cos(radians) * radius;
                var prefab = hasSeer && i == population - 1
                    ? SettlerPrefabs.Seer
                    : SettlerPrefabs.Settler;
                l.AddGrounded(prefab, x, 0f, z, angle + 180f);
            }
            return l;
        }

        private static float FacingCentre(float x, float z)
        {
            return Mathf.Atan2(-x, -z) * Mathf.Rad2Deg;
        }

        private static void AddPath(SettlementLayout layout, float x, float z)
        {
            foreach (var fraction in new[] { 0.35f, 0.65f, 0.9f })
            {
                layout.AddGrounded("path_v2", x * fraction, 0f, z * fraction);
            }
        }

        /// <summary>A small fortified black forest outpost with three settlers.</summary>
        public static SettlementLayout ForestOutpost()
        {
            var o = new SettlementLayout(SettlementLocations.ForestOutpostLocation);
            o.Add(SettlerPrefabs.Heart, 0f, 0f, 0f);
            o.Add(SettlerPrefabs.FlattenOutpost, 0f, 0f, 0f);

            o.Place(Watchtower(), 0f, 0f);
            o.Place(Cabin("TreasureChest_blackforest"), -6f, 2f, 90f);
            o.Add("fire_pit", 3f, 0f, 2f);
            o.Add("wood_stack", 4.5f, 0f, 0.5f);
            o.Add("piece_workbench", 3.5f, 0f, -2f, 180f);
            o.Add("piece_groundtorch_wood", -1.8f, 0f, 1.8f);
            o.Add("piece_groundtorch_wood", 1.8f, 0f, -1.8f);
            AddStakeRing(o, 10f, 14, 40f);

            o.Add(SettlerPrefabs.Settler, 2f, 0f, 2.5f, 225f);
            o.Add(SettlerPrefabs.Settler, -2.5f, 0f, -1f, 90f);
            o.Add(SettlerPrefabs.Settler, 0f, 0f, 5f, 180f);
            return o;
        }

        /// <summary>
        /// A clanless bandit camp: crude shelters, loot, raiders, and the war
        /// totem whose destruction clears the camp.
        /// </summary>
        public static SettlementLayout ClanlessCamp()
        {
            var c = new SettlementLayout(SettlementLocations.ClanlessCampLocation);
            c.Add(SettlerPrefabs.FlattenCamp, 0f, 0f, 0f);

            c.Add("fire_pit", 0f, 0f, 0f);
            c.Add(SettlerPrefabs.CampTotem, 2.5f, 0f, 0.5f);
            c.Add("wood_stack", 3f, 0f, -2f);

            // Two crude open shelters: poles carrying a flat roof.
            foreach (var (sx, sz, rot) in new[] { (-4.5f, 3.5f, 30f), (4f, 4.5f, 300f) })
            {
                var shelter = new SettlementLayout("shelter");
                shelter.Add("wood_pole2", -1.8f, 0f, -0.8f);
                shelter.Add("wood_pole2", 1.8f, 0f, -0.8f);
                shelter.Add("wood_pole2", -1.8f, 0f, 0.8f);
                shelter.Add("wood_pole2", 1.8f, 0f, 0.8f);
                shelter.Add("wood_floor", -1f, 2f, 0f);
                shelter.Add("wood_floor", 1f, 2f, 0f);
                c.Place(shelter, sx, sz, rot);
            }

            c.Add("TreasureChest_blackforest", -4.5f, 0f, 2.5f, 30f);
            AddStakeRing(c, 9f, 10, 70f);

            c.Add(SettlerPrefabs.Raider, 1.5f, 0f, 1.8f, 200f);
            c.Add(SettlerPrefabs.Raider, -2f, 0f, -1.2f, 80f);
            c.Add(SettlerPrefabs.Raider, -1f, 0f, 4f, 160f);
            c.Add(SettlerPrefabs.Raider, 3.2f, 0f, -3.2f, 340f);
            return c;
        }

        /// <summary>A stone-walled plains steading with a barley farm.</summary>
        public static SettlementLayout PlainsSteading()
        {
            var s = new SettlementLayout(SettlementLocations.PlainsSteadingLocation);
            s.Add(SettlerPrefabs.Heart, 0f, 0f, 0f);
            s.Add(SettlerPrefabs.FlattenSteading, 0f, 0f, 0f);

            s.Place(Hall("stone_wall_2x1", true, "TreasureChest_heath"), 0f, 4f, 180f);
            s.Place(Farm("Pickable_Barley", "Pickable_Flax"), -10f, -3f, 0f);
            s.Place(FirePlaza(), 6f, -5f);
            s.Place(Watchtower(), 11f, 2f);
            s.Add("piece_groundtorch_wood", 2.5f, 0f, -3f);
            s.Add("piece_groundtorch_wood", -2.5f, 0f, -3f);
            AddStakeRing(s, 15f, 20, 36f);

            s.Add(SettlerPrefabs.Settler, 5f, 0f, -4f, 270f);
            s.Add(SettlerPrefabs.Settler, -8f, 0f, -4f, 90f);
            s.Add(SettlerPrefabs.Settler, 0f, 0f, -2f, 180f);
            s.Add(SettlerPrefabs.Seer, 1.5f, 0f, -6f);
            return s;
        }
    }
}
