# Multiplayer and test-panel guide

Hearth & Hird requires the mod on every client. The plugin declares Jötunn's
`EveryoneMustHaveMod` network compatibility, while settler identity, state,
job, equipment, directives, Hird settings and Hearthstone membership are kept
in Valheim ZDO state.

## Enabling the test panel

The panel is disabled by default and can only run on the listen-server host or
in single-player. A remote client cannot open it or invoke its development
mutations.

1. Start a single-player or player-hosted world with `-console` enabled.
2. Open the console with F5 and run `devcommands`.
3. Run `hnh_test enable`.
4. Press F7, or run `hnh_test` again.
5. Run `hnh_test disable` when testing is finished.

The hotkey can be changed with `Development.TestPanelHotkey`. Public and
dedicated servers should leave `Development.EnableTestTools` false.

## What the panel exercises

- dropdown configuration for unit type, allegiance, count, level, job and kit;
- a visible summary of exactly what the next Spawn action will create;
- networked wild, assigned and Hird settler/seer spawning;
- loaded-unit selection and host ownership diagnostics;
- Wild, Hird and Hearthstone-assigned state transitions;
- job cycling through every current job;
- normal player-inventory equipment UI;
- bronze, iron, archer and plains development loadouts;
- NPC levels 1–3 (zero to two Valheim stars) and selected-unit level changes;
- selected-unit positioning and gear access;
- selected Follow, Hold and Retreat orders;
- whole-Hird orders, formation cycling and combat-stance cycling.
- one-click local-Hird disband and safe cleanup of panel-created units.

The panel marks every unit it creates. **Despawn test units** deletes only those
marked units, while **Disband all Hird** releases the local player's complete
warband, including stowed members. Ordinary world settlers are never removed
by the despawn action.

Hird followers no longer teleport during ordinary travel. The optional
`Party.EmergencyWarp` recovery is disabled by default; if enabled, it activates
only after a follower remains more than 120 metres away for ten seconds.

Assigned test units deliberately bypass bed and tier limits so large-population
tests can be set up quickly. Normal recruitment continues to enforce both.

## Two-player ownership test

Use a player-hosted world with one remote client and the mod installed on both.

1. Host: place a Hearthstone, enable the panel and spawn an assigned settler.
2. Client: walk to the settler and remain beside it while the host moves more
   than one network zone away.
3. Confirm the settler continues its AI under the peer that owns its ZDO.
4. Host: reopen the Hearthstone register and confirm its name, job and last
   position continue updating through the owner-targeted register RPC.
5. Host: return, select the unit and change its job and equipment. Confirm the
   client sees the same appearance and job after the next ZDO update.
6. Host: make it a Hird follower, use Follow/Hold/Retreat, then cycle formation
   and combat stance. Confirm the client sees the same movement and equipment.
7. Host: assign it again, log out and reload. Confirm name, profile, equipment,
   job, Hearthstone binding and register entry persist.

Also test two separate Hirds, one per player. A player's party registry lives on
that player's character data, and each settler records its recruiter ID, so one
player's orders must not alter the other's warband.

## Authority rules

- The current ZDO owner runs physical AI and writes settler state.
- A Hearthstone owner validates and persists register updates received from a
  settler owned by another peer.
- Register update payloads contain only a settler ZDO ID; the Hearthstone owner
  resolves the live unit and reads its authoritative state rather than trusting
  client-supplied names, jobs or positions.
- Removal requests from a living assigned unit are accepted only from that
  unit's current ZDO owner. Dead, unloaded or already-unassigned entries may be
  removed normally.
- Development mutations are never exposed to remote clients.
