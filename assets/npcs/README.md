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
catalogue. The level-45 display rule was confirmed by the user; the original
base level can remain visible in parentheses. Distance uses matching received
actor coordinates and centimetre-to-metre conversion, not a name lookup.

Local catalogue addition: NPC 2980240, Sangre venenosa de Fafnir, was matched to the selected enemy in a controlled Pesadilla entry on 2026-10-15. The Spanish display name was supplied by the player; the English label is our translation, not a verified client-localization extraction. Its actual current/max HP comes from the spawn and stat messages, independently of this label.
