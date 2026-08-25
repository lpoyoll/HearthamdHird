# The Hird

The travelling hird is deliberately separate from settlement population. A
player must carry a Hird Horn to recruit travelling companions, and the best
horn in their inventory determines the cap.

| Horn | Progression | Followers | Upgrade materials |
| --- | --- | ---: | --- |
| Crude Hird Horn | Meadows | 2 | Wood, leather scraps, deer trophy |
| Bronze-bound Hird Horn | Black Forest | 3 | Previous horn, bronze, fine wood |
| Iron-bound Hird Horn | Swamp | 4 | Previous horn, iron, ancient bark |
| Silver-bound Hird Horn | Mountains | 6 | Previous horn, silver, wolf pelt |
| Blackmetal Hird Horn | Plains | 8 | Previous horn, black metal, linen thread |
| Eitr-carved Hird Horn | Mistlands | 10 | Previous horn, refined eitr, Yggdrasil wood |
| Flametal Hird Horn | Ashlands | 12 | Previous horn, flametal |

## Controls

Put a horn on the hotbar or use it from the inventory.

| Input | Order |
| --- | --- |
| Use horn | Toggle Follow / Hold |
| Block + use horn | Toggle Retreat / Follow |
| Crouch + use horn on an enemy | Focus attack |
| Crouch + use horn on terrain | Move there and hold |
| Block + crouch + use horn | Defend the aimed point |
| J (default) | Cycle formation |
| K (default) | Cycle combat stance |

The legacy G, H and Y party shortcuts continue to mirror these orders while a
horn is carried. Orders update the same persistent ZDO directive used by the
settler AI, so the horn is a control surface rather than a separate follower
implementation.

## Combat stances

| Stance | Behaviour |
| --- | --- |
| Passive | Does not acquire targets; an explicit focus-attack still overrides it |
| Defensive | Uses normal Valheim threat response |
| Aggressive | Actively seeks valid enemies within 35 metres |

Stance and formation are stored per player/world and copied into every hird
member's network state. Portal and boat stowing reapplies them when the NPC is
spawned again.

## Formations

| Formation | Physical layout |
| --- | --- |
| Follow | Vanilla close following |
| Line | Even line behind the player |
| Shield Wall | Tight line in front of the player |
| Wedge | Point-forward triangular advance |
| Loose | Wide grid with extra spacing against area attacks |
| Archers Behind | Bow-equipped settlers behind the melee line |

Formation positions are real moving targets followed by MonsterAI. Move and
Defend orders apply the selected offsets around the aimed destination as well.

An existing party above the current horn cap is retained. No member is deleted,
dismissed or left behind; the player simply cannot recruit another travelling
companion until capacity catches up.
