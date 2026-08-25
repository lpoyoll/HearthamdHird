# Hearthstone settlements

The Hearthstone is the physical and network-persistent centre of a player
settlement. It replaces the old radius-only Settlement Banner without changing
the saved prefab name, so existing worlds continue to load.

## Founding a camp

Build a **Hearthstone** from the hammer's Misc tab at a workbench:

- 10 Wood
- 5 Stone
- 1 Deer Trophy

The player who places it becomes its founder. Only that player may rename,
upgrade or manage the Hearthstone, and only their recruited warriors can be
assigned to it.

## Progression

Equip the first material listed for the next tier, aim at the Hearthstone and
press Use. The full cost is removed only when every material is available.

| Tier | Population ceiling | Work radius | Upgrade cost |
| --- | ---: | ---: | --- |
| Camp | 4 | 35m | Initial piece |
| Homestead | 8 | 50m | 5 Bronze, 10 Fine Wood |
| Hamlet | 14 | 70m | 5 Iron, 10 Elder Bark |
| Village | 22 | 90m | 5 Silver, 10 Wolf Pelt |
| Hold | 32 | 120m | 10 Black Metal, 10 Linen Thread |
| Great Hold | 48 | 150m | 10 Refined Eitr, 10 Yggdrasil Wood |
| Jarl's Seat | 64 | 200m | 10 Flametal |

The work radius is used by jobs, storage, construction sites, housing,
couriers, families, newcomer spawning and settlement raids. Upgrading also
expands the Hearthstone's native Valheim player-base event area.

## Beds and population

The usable population limit is the lower of:

1. the Hearthstone tier ceiling; and
2. the number of unclaimed beds inside its work radius.

A bed belongs to the nearest Hearthstone whose radius contains it, so two
overlapping settlements cannot count the same bed. Player-claimed beds do not
count as settler housing. Existing settlers are never deleted when beds are
removed, but another cannot be assigned until capacity is available again.

## Persistent register

Every assigned settler stores the exact ZDO identity of their Hearthstone.
The Hearthstone register retains each settler's:

- identity and name;
- level and job;
- hunger status; and
- last known world position.

Loaded settlers refresh their record every five seconds. Unloaded settlers
remain in the register at their last known position, preventing unloaded NPCs
from disappearing from population counts.

Press Use on the Hearthstone to open the register. It supports every tier's
full population through pages of ten settlers. The **Map** button places a pin
at that settler's last known location. Job changes remain available while the
settler is loaded; an unloaded row is read-only until its zone loads again.

Legacy three-tier settlements are migrated conservatively: explicitly
promoted Village and Town saves map to Village and Hold. An unmarked legacy
Hamlet starts as a Camp because the old default tier was never written to its
ZDO; no settlers are deleted, but beds and upgrades are required before adding
more.
