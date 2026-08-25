# Hearth & Hird roadmap

## 0.1.0 — Human settler foundation

- [x] Preserve VikingSettlements save and multiplayer behaviour.
- [x] Add persistent generated profile and aptitude data.
- [x] Add a revisioned, ZDO-backed directive shared by hird and workers.
- [x] Add an opt-in physical-task registry and owner-side runner.
- [x] Replace the Dvergr compatibility visual with a defensive player-body visual adapter.
- [x] Persist and render hair, beard, skin and hair colour choices.
- [x] Add a development inspection command for profile and directive state.

`hnh_inspect` reports the nearest settler within 20 metres. The player-body
adapter deliberately retains the original compatibility visual when required
vanilla Player anchors cannot be mapped after a Valheim update.

## 0.2.0 — Recruitment and Hird commands

- [x] Seven craftable Hird Horn tiers with biome-based follower caps (2–12).
- [x] Horn-issued follow, hold, focused attack and retreat orders.
- [x] Aimed move and defend orders.
- [x] Persistent passive, defensive and aggressive stances.
- [x] Physical formation slots for line, loose, wedge, shield wall and archers behind.

The best horn carried supplies the recruitment cap. A normal use toggles the
hird between following and holding position; block while using it to toggle an
emergency retreat. Crouch-use attacks an aimed enemy or moves to aimed ground;
block+crouch-use defends the aimed point. J cycles formations and K cycles
combat stance while a horn is carried. Existing over-cap parties are
preserved, but cannot recruit additional members until capacity catches up.

## 0.3.0 — Hearthstone

- [x] Hearthstone placement, creator ownership and seven biome upgrades.
- [x] Bed-bound population capacity with nearest-Hearthstone bed ownership.
- [x] Tier work radii used by inherited settlement systems.
- [x] Persistent settlement register and last-known map lookup.

The save-compatible Hearthstone progresses from Camp (4 settlers, 35m) to
Jarl's Seat (64 settlers, 200m). Each settler is bound to its exact Hearthstone
ZDO rather than inferred from an overlapping radius. The management panel
pages through the persistent register and can pin loaded or unloaded settlers'
last known positions on the map.

## 0.4.0 — Physical Lumberjack and Hauler proof

- Forestry work area and tree filters.
- Target reservation and path recovery.
- Physical lumberjack animation and damage.
- Fallen-log tracking and processing.
- Real drop collection, carry capacity and timber storage delivery.
- Hauler task using the same reservations and storage rules.

## Later milestones

| Version | Milestone |
| --- | --- |
| 0.5.0 | Guard posts, patrol paths and combat orders |
| 0.6.0 | Mining, gathering and farming |
| 0.7.0 | Storage rules and production chains |
| 0.8.0 | Equipment, skills and NPC progression |
| 0.9.0 | Blueprint construction and Builder AI |
| 1.0.0 | Morale, raids, balancing and multiplayer hardening |
| 1.1.0 | Hunting, fishing, animal keeping and wilderness specialist jobs |
| 1.2.0 | Multiple player settlements, couriers, carts and physical caravans |
| 1.3.0 | Living NPC clan settlements, independent reputation, trade, alliances, rivalries, tribute, war and joint defence |
| 1.4.0 | Villager friendships, rivalries, partners, spouses, households, grief and social memory |
| 1.5.0 | Children and generations: family trees, inherited traits and configurable growth into adult settlers |
| 1.6.0 | Deeper daily life: home routines, mead-hall gatherings, celebrations, ceremonies and memorials |

The current build now contains the first narrow foundation for 1.3.0: every
wild settlement heart stores a separate relationship for each player, and a
village attacked by that player defends itself without making every other
settlement hostile. Alliances, trade, wars and inter-settlement politics remain
design-only.

Relationships and children deliberately sit after the 1.0.0 stability gate.
They should create memorable households and emergent stories without turning
Valheim into a heavy needs simulator. Children will be configurable and use a
lightweight growth abstraction rather than requiring full adult AI from birth.
