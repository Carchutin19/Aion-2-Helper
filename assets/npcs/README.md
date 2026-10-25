# Enemy display names

English and available Spanish NPC names transformed from
[SkeeveAN/Aion-DPS-Meter](https://github.com/SkeeveAN/Aion-DPS-Meter), revision
`ce50cb1c28c95dfde0f776c32ad555c1febfe79b`,
`Client/assets/aion2/npcs/npc_names.json`. The upstream MIT notice is retained
in LICENSE-MIT.txt. The original game names remain NCSOFT content.
Names are display metadata, not a source of level, category, health or target
selection. Health and targeting use passive local capture observations.


`species.json` maps 2,257 NPC IDs with an unambiguous classification to the
game's creature type. It is derived from the same revision's
`Client/assets/aion2/pets/pets.json` (NPC → pet/model classification), with the
MIT notice retained. Gentle Meadow Zaif (`2100146`, Feras) was additionally
confirmed against the user's locked in-game target on 2026-10-14. No type is
inferred from an enemy's display name. Missing classifications stay unknown.
Display labels use the game's EN/ES species names from `species_stats.json`.

Level comes from a bounded supported NPC spawn tail, independently of this
catalogue. The user-confirmed level-45 adjustment applies only to a received lower base
level: level18 can display45(18), while a received54 stays54. Unknown levels
are not replaced with45. Supported status-bearing spawn tails retain shared
bit-packed booleans and require full bounded consumption before accepting the
level; other optional layouts leave it unknown. The original base level can
remain visible in parentheses. Distance uses matching received
actor coordinates and centimetre-to-metre conversion, not a name lookup.

Local catalogue addition: NPC 2980240, Sangre venenosa de Fafnir, was matched to the selected enemy in a controlled Pesadilla entry on 2026-10-15. The Spanish display name was supplied by the player; the English label is our translation, not a verified client-localization extraction. Its actual current/max HP comes from the spawn and stat messages, independently of this label.


Additional display-name coverage (2026-10-17): 4,803 previously missing named
NPC IDs in the already supported 2,900,000–2,999,999 range were added from
`Client/assets/aion2/npcs/npc_names.json` at upstream revision
`2ef71759738ffe902f72535ba8702e9e68d3b105` (same MIT notice retained).
Existing entries, including the locally confirmed Fafnir label, are preserved.
The dungeon capture contained 13 of these IDs, including Vakron's Thorn Root
and Thorn Vines. IDs with no reference name remain unknown; no name is inferred
from combat, HP, nearby actors or the name of a summon owner. The same embedded
lookup supplies the enemy and enemy-target bars; it does not classify allegiance,
boss status, health or player identity.

## Confirmed neutral NPCs

`neutral.json` is a small explicit map-scoped catalogue, not a generic hostility
flag or a display-name classifier. Controlled selection/dialogue tests on
2026-10-25 in map1010 identified Storage Manager (`2930066`), Command Merchant
(`2939438`) and a selected neutral profile (`2989661`) during the warehouse
control. The last profile has no verified localized name; the overlay uses
**Neutral NPC / NPC neutral**, not an invented proper name. The first two use
the existing verified display-name catalogue.

Only those exact template/map pairs receive the neutral-NPC palette. Other NPCs
retain the base palette until their allegiance is established. The same layout
flags occur on merchants and hostile monsters and are not used for this decision.
The metadata is local controlled-test classification; it does not infer health,
level or distance. Raw captures and personal data are not bundled.
