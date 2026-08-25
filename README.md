<p align="center">
  <img src="docs/brand/hearth-and-hird-hero-1600.jpg" width="100%"
       alt="A living Norse settlement and an organised Viking warband overlooking a mountain valley">
</p>

<p align="center">
  <img src="docs/brand/hearth-and-hird-icon-256.png" width="112"
       alt="Hearth & Hird shield emblem">
</p>

<h1 align="center">Hearth & Hird</h1>

<p align="center">
  <strong>A living Viking settlement and warband simulation for Valheim.</strong><br>
  Recruit people, build them a home, put tools in their hands and lead your own hird into battle.
</p>

<p align="center">
  <a href="releases/HearthAndHird-v1.14.0.zip"><strong>Download development build 1.14.0</strong></a>
  · <a href="docs/ROADMAP.md">Roadmap</a>
  · <a href="docs/MULTIPLAYER_TESTING.md">Multiplayer testing</a>
  · <a href="docs/UPSTREAMS.md">Licences and provenance</a>
</p>

> [!IMPORTANT]
> Hearth & Hird is under active development. Version **1.14.0** is a playable
> foundation and test build, not the finished settlement simulation. Back up
> important worlds before testing development releases.

## The idea

Valheim begins with one dead warrior. Hearth & Hird turns that lonely camp
into a community:

**you → followers → a camp → a settlement → a functioning Viking community → a warband**

NPCs are intended to be people in the world, not resource timers in a menu.
The defining goal is that a lumberjack must walk to a tree, equip an axe, fell
it, process the logs, collect the wood and physically carry it to storage.

The same principle applies to hauling, mining, farming, construction, patrols
and production chains. Distant settlements may use lower-frequency simulation
for performance, but work around the player should remain visible and real.

## What works in 1.14.0

### Human settlers

- Persistent Norse names, appearance, equipment, allegiance and level.
- Wild residents belong to one exact village rather than a global NPC faction.
- Villages remember their own relationship with each player.
- Residents defend themselves, return home after danger and are leashed to
  their village rather than wandering across the map.
- Escalating aggression: one unarmed punch causes a short brawl; repeated or
  armed attacks provoke a serious settlement response.
- Village hierarchy: Headmen/Headwomen, Elders, Jarls, Hersirs, Guards,
  Housecarls and Seers.
- Larger settlements contain a deliberate mix of stronger residents and
  better-equipped defenders.

### Your hird

- Recruit settlers into a persistent travelling party.
- Equip them with real weapons, shields, armour, bows, arrows and food.
- Follow, hold, move, attack, defend, retreat, stance and formation orders.
- Hird members defend their leader against genuine settler hostility without
  massacring a village over a single player-instigated fist fight.
- Multiplayer-safe ownership and persistent state through Valheim ZDOs.

### Hearthstones and settlements

| Tier | Population | Work radius | Wild leader |
|---|---:|---:|---|
| Camp | 4 | 35 m | Headman/Headwoman |
| Homestead | 8 | 50 m | Headman/Headwoman |
| Hamlet | 14 | 70 m | Elder |
| Village | 22 | 90 m | Elder |
| Hold | 32 | 120 m | Jarl |
| Great Hold | 48 | 150 m | Jarl |
| Jarl's Seat | 64 | 200 m | Jarl |

- Build a Hearthstone from the hammer's **Misc** category near a workbench.
- Population requires both Hearthstone capacity and actual beds.
- Generated test settlements include grounded halls, cabins, farms, paths,
  fires, defences and watchtowers appropriate to their tier.
- The terrain survey rejects water, existing villages and unsuitable slopes.
- Test settlements can be placed near the host or near the world's first spawn.

### Physical Lumberjack and Hauler proof

- Build a **Forestry Marker** from the hammer's Misc category and cycle its
  15/25/40 metre work radius with E; Shift+E pauses or enables it.
- Build a **Timber Store** as the designated physical destination for wood.
- Assigned Lumberjacks reserve a mature marked tree, walk to it without routine
  teleporting, visibly equip an axe, fell it and process its real fallen logs.
- Workers collect actual Wood, Fine Wood or Core Wood drops into persistent
  carrying inventory, walk them home and deposit them into the Timber Store.
- Strength increases carrying capacity, stuck workers release their target and
  replan, and interrupted work retains cargo across combat and save/load.
- Assigned Haulers physically move timber from ordinary settlement chests to
  the nearest Timber Store.
- The current ZDO owner alone advances task AI and inventory mutation, keeping
  the system suitable for multiplayer testing.

### Development test menu

The host-only **F7 Test Muster** is designed to make iteration possible without
hours of normal progression:

- Choose NPC type, allegiance, count, level, job and equipment from dropdowns.
- See exactly what the next **Spawn** action will create.
- Spawn a standalone Hearthstone.
- Spawn any settlement tier from Camp through Jarl's Seat.
- Select placement near you or near the first spawn.
- Spawn Forestry Markers, Timber Stores and mature test trees.
- Assign Lumberjack or Hauler directly and watch live task/cargo diagnostics.
- Inspect and change loaded settlers, combat orders and equipment.
- Reset temporary village hostility, disband the local hird or remove all
  loaded test objects created by the menu.

To enable it in single-player or as the listen-server host:

```text
F5
devcommands
hnh_test enable
```

Then press **F7**. Remote clients can see and interact with the resulting
networked objects but cannot perform development mutations.

## What comes next

The critical **0.4 physical-work foundation is now implemented for testing**.
The immediate work is in-game and two-player hardening: difficult terrain,
ownership hand-off, multiple workers competing for targets, full stores and
combat interruption. Once that loop is reliable, the same framework becomes
the basis for mining, gathering, farming, building and settlement logistics.

| Design milestone | Status |
|---|---|
| 0.1 Human NPC foundation | Implemented; hardening continues |
| 0.2 Recruitment and Hird Horn | Implemented; command/AI tuning continues |
| 0.3 Hearthstone, ownership and beds | Implemented foundation |
| **0.4 Physical Lumberjack and Hauler** | **Implemented; multiplayer/pathing hardening** |
| 0.5 Guard posts, patrol paths and combat orders | Partial foundation |
| 0.6–0.9 Expanded jobs, logistics, progression and builders | Planned |
| 1.0 Morale, raids, balance and multiplayer hardening | Planned |
| 1.1+ Families, caravans, rival Vikings and multiple settlements | Designed |
| 1.3 NPC settlement diplomacy, alliances, trade and war | Design only |

See the full [development roadmap](docs/ROADMAP.md).

## Installation

### Mod manager

1. Create a Valheim profile in r2modman or the Thunderstore app.
2. Install **BepInExPack Valheim 5.4.2333+**.
3. Install **Jötunn 2.29.2+**.
4. Import [`HearthAndHird-v1.14.0.zip`](releases/HearthAndHird-v1.14.0.zip)
   as a local mod, or copy the packaged plugin into the profile manually.
5. Remove older duplicate copies before launching.

### Manual

Copy the plugin DLL from the release package into:

```text
<Valheim>/BepInEx/plugins/HearthAndHird/
```

The internal DLL filename is currently `VikingSettlements.dll`; this is
intentional compatibility naming, not the public mod name.

### Multiplayer

Hearth & Hird uses Jötunn's `EveryoneMustHaveMod` network rule:

- Install it on the dedicated/listen server.
- Install it on every connecting client.
- Keep everybody on the same minor version.
- Perform dev-menu spawning and destructive test actions as the host.

See [MULTIPLAYER_TESTING.md](docs/MULTIPLAYER_TESTING.md) for the host/client
test matrix and expected persistence checks.

## Compatibility identity

Hearth & Hird is its own project and design direction. During this migration,
the internal plugin GUID, config filename, DLL name and existing ZDO keys remain
unchanged so established saves and multiplayer state are not gratuitously
broken. They will be migrated only with an explicit compatibility plan.

The project began from open-source VikingSettlements code and references
selected ideas from Kuku's Village. Required attribution and licence details
are preserved in [UPSTREAMS.md](docs/UPSTREAMS.md) and
[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## Requirements

| Dependency | Minimum |
|---|---:|
| Valheim | 0.221.4 or compatible |
| BepInExPack Valheim | 5.4.2333 |
| Jötunn | 2.29.2 |

## Documentation

- [Design principles](docs/DESIGN.md)
- [Roadmap](docs/ROADMAP.md)
- [Hird controls and progression](docs/HIRD.md)
- [Hearthstone progression](docs/HEARTHSTONE.md)
- [Multiplayer test plan](docs/MULTIPLAYER_TESTING.md)
- [Console commands](docs/commands.md)
- [Original brand artwork and prompts](docs/brand/ARTWORK.md)
- [Licences and upstream provenance](docs/UPSTREAMS.md)

## Building from source

The project targets .NET Framework 4.8 through the installed .NET SDK and
expects Valheim, BepInEx and Jötunn reference assemblies. With the build
environment configured:

```bash
dotnet build VikingSettlements.sln -c Release
```

The solution and source folder retain their compatibility-era names for now;
the built mod identifies itself to players as **Hearth & Hird**.

## Contributing

Useful reports include the exact world seed, player/server role, settlement
tier, reproduction steps and the relevant section of `BepInEx/LogOutput.log`.
For AI problems, describe what the NPC was doing, what order it had and how far
it was from its assigned home.

## Licence

Released under the [MIT Licence](LICENSE). Valheim is a trademark of Iron Gate
AB. Hearth & Hird is an independent community mod and is not affiliated with
or endorsed by Iron Gate.
