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
| Crouch + use horn | Attack the enemy under the crosshair |

The legacy G, H and Y party shortcuts continue to mirror these orders while a
horn is carried. Orders update the same persistent ZDO directive used by the
settler AI, so the horn is a control surface rather than a separate follower
implementation.

An existing party above the current horn cap is retained. No member is deleted,
dismissed or left behind; the player simply cannot recruit another travelling
companion until capacity catches up.
