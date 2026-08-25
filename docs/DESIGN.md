# Hearth & Hird design

Hearth & Hird is a Valheim settlement and warband simulation built as a
compatible evolution of VikingSettlements. Its defining rule is simple:

> A nearby settler only creates, moves or consumes a world resource by
> physically interacting with that resource in the world.

The migration is incremental. Working VikingSettlements features remain
available until their Hearth & Hird replacement is complete and tested.

## Core invariants

1. **People are persistent.** A settler's name, appearance seed, aptitudes,
   equipment, experience, relationships and history belong to that person and
   survive save/load, multiplayer ownership changes and party travel.
2. **Orders are explicit.** Every loaded settler has one high-level directive:
   idle, follow, hold, fall back, work, guard or attack.
3. **Physical work is authoritative nearby.** A lumberjack finds a valid tree,
   walks to it, equips an axe, damages it, processes its logs, collects the
   actual drops and deposits those items.
4. **Items are conserved.** Job code cannot create an output unless it consumed
   or interacted with the corresponding world input. Abstract simulation is a
   separate unloaded-settlement system and must reconcile its ledger when the
   zone loads.
5. **The ZDO owner makes decisions.** Other clients render replicated state;
   they do not run a competing task brain.
6. **Combat interrupts work.** Threat and survival directives outrank work,
   hauling, rest and social activity.
7. **Old worlds remain loadable.** Existing `vs_*` keys are preserved. New
   Hearth & Hird state uses `hnh_*` keys and imports legacy state once.

## Foundation architecture

The first branch introduces three seams without changing current gameplay:

- `SettlerProfile` creates deterministic persistent sex, appearance choices and
  base aptitudes. The compatibility prefab does not render all those choices
  yet; a later player-body visual adapter will consume them.
- `SettlerDirectiveState` normalises party stance and settlement job state into
  one revisioned network command.
- `SettlerTaskRegistry` and `SettlerTaskBrain` let a physical job claim a stable
  work id. Once registered, the matching timed-production case is suppressed.

This permits jobs to move over individually. The first registered task will be
`lumberjack`; no other production behaviour needs to change at the same time.

## Near-player lumberjack state machine

```text
Acquire zone -> Find mature tree -> Reserve target -> Walk into range
-> Equip axe -> Face and swing -> Track fallen trunk -> Process logs
-> Collect drops -> Choose allowed storage -> Carry -> Deposit -> Repeat
```

Every transition must have a timeout and recovery route. A destroyed target,
blocked path, full chest, ownership loss or combat interruption must release
the reservation and leave the settler in a valid state.

## Simulation levels

| Range | Authority | Behaviour |
| --- | --- | --- |
| Under 100 m | Physical | Full movement, animation, combat and item interaction |
| 100–300 m | Coarse | Low-frequency decisions using loaded-world objects only |
| Unloaded | Ledger | Settlement-level production/consumption with elapsed-time caps |

Abstract results must never appear beside a physically simulated worker for the
same time span. Loading a settlement closes its ledger interval before physical
AI resumes.

