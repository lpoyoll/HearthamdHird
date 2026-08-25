# Hearth & Hird

![Hearth & Hird — a living Norse settlement and organised warband](https://raw.githubusercontent.com/lpoyoll/HearthamdHird/master/docs/brand/hearth-and-hird-hero-1600.jpg)

**A living Viking settlement and warband simulation for Valheim.** Recruit
human settlers, build them a home, equip and command a persistent hird, and
develop camps into functioning communities.

> Version **1.13.5** is an active development build. Back up important worlds
> before testing it.

## Current highlights

- Human player-body settlers with persistent names, appearance, equipment,
  allegiance, level and village membership.
- A controllable Hird with follow, hold, move, attack, defend, retreat, stance
  and formation orders.
- Seven Hearthstone tiers from a four-person Camp to a 64-person Jarl's Seat;
  beds remain the real population requirement.
- Per-village player relationships and graduated aggression rather than one
  global settler faction.
- Wild hierarchy and strength scaling: Headmen/Headwomen, Elders, Jarls,
  Hersirs, Guards, Housecarls and Seers.
- Host-only F7 Test Muster for spawning, equipping, ordering and cleaning up
  settlers, Hearthstones and complete settlements of any tier.
- Multiplayer ownership and persistence through Valheim ZDO state.

The central development goal is **physical work**. NPCs should walk to real
targets, use tools and animations, collect actual drops and carry them to
rule-based storage. The next milestone builds the first reusable version of
that loop through Lumberjack and Hauler jobs.

## Installation

Install BepInExPack Valheim 5.4.2333+ and Jötunn 2.29.2+, then install the
Hearth & Hird release package on the server and every client.

Manual installs place the included DLL in:

```text
<Valheim>/BepInEx/plugins/HearthAndHird/
```

The DLL and config currently retain compatibility-era `VikingSettlements`
names so existing saves and ZDO state are not broken. The public mod and all
new development are **Hearth & Hird**.

## Development menu

As the single-player/listen-server host, open F5 and enter:

```text
devcommands
hnh_test enable
```

Press **F7**, select what you want from the dropdowns and then press the
relevant Spawn button. Complete settlements can be created as a Camp,
Homestead, Hamlet, Village, Hold, Great Hold or Jarl's Seat, either near you
or near the first spawn.

The same panel can reset temporary village hostility, disband the local hird
and remove loaded objects it created. Remote clients can interact with the
networked results but cannot perform development mutations.

## Links

- [Source and current download](https://github.com/lpoyoll/HearthamdHird)
- [Roadmap](https://github.com/lpoyoll/HearthamdHird/blob/master/docs/ROADMAP.md)
- [Multiplayer testing](https://github.com/lpoyoll/HearthamdHird/blob/master/docs/MULTIPLAYER_TESTING.md)
- [Hird guide](https://github.com/lpoyoll/HearthamdHird/blob/master/docs/HIRD.md)
- [Hearthstone guide](https://github.com/lpoyoll/HearthamdHird/blob/master/docs/HEARTHSTONE.md)
- [Licences and provenance](https://github.com/lpoyoll/HearthamdHird/blob/master/docs/UPSTREAMS.md)

## Licence

Released under the MIT Licence. Hearth & Hird is an independent community mod
and is not affiliated with or endorsed by Iron Gate AB.
