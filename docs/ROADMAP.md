# Hearth & Hird roadmap

## 0.1 — Human settler foundation

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

## 0.2 — Hird commands

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

## 0.3 — Hearthstone

- [x] Hearthstone placement, creator ownership and seven biome upgrades.
- [x] Bed-bound population capacity with nearest-Hearthstone bed ownership.
- [x] Tier work radii used by inherited settlement systems.
- [x] Persistent settlement register and last-known map lookup.

The save-compatible Hearthstone progresses from Camp (4 settlers, 35m) to
Jarl's Seat (64 settlers, 200m). Each settler is bound to its exact Hearthstone
ZDO rather than inferred from an overlapping radius. The management panel
pages through the persistent register and can pin loaded or unloaded settlers'
last known positions on the map.

## 0.4 — Physical work proof

- Forestry work area and tree filters.
- Target reservation and path recovery.
- Physical lumberjack animation and damage.
- Fallen-log tracking and processing.
- Real drop collection, carry capacity and timber storage delivery.
- Hauler task using the same reservations and storage rules.

## Later milestones

| Version | Milestone |
| --- | --- |
| 0.5 | Guard posts, patrol paths and combat orders |
| 0.6 | Mining, gathering and farming |
| 0.7 | Storage rules and production chains |
| 0.8 | Equipment, skills and progression |
| 0.9 | Blueprint construction and builder AI |
| 1.0 | Morale, raids, balance and multiplayer hardening |
