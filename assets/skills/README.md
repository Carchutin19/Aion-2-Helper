# Skill metadata

`catalogue.json` is a transformed ID/name and classification dataset from
[SkeeveAN/Aion-DPS-Meter](https://github.com/SkeeveAN/Aion-DPS-Meter), revision
`e767bf486d52407d91f5fea698a9fdc5c7d1d5af`, under `Client/assets/aion2/skills`.
It retains English names, available Spanish text, active/passive annotations,
periodic annotations, heal/damage families and the non-damage watchlist.
This catalogue contains no buff amplification percentages or portrait images.
The Hotbar overlay separately embeds 378 skill portraits and an icon-token map
from the same upstream project, revision 65c55c96996d585331757ecd0e28cb80b1813c24.
Their software/data notice is identical to the included LICENSE-MIT.txt; game
artwork remains NCSOFT content. Provenance and supported layouts are documented
in assets/skill-slots/README.md in the source and embedded in the executable.
No upstream application or network service is required. The catalogue is embedded in the helper; missing display
metadata must never discard an otherwise supported combat event.

The reference names originate from Aion 2 client metadata, as documented in the
upstream skill-data README. Game names/text remain NCSOFT's content; the upstream
MIT code licence does not claim ownership of that content. Adapted decoding and
metadata-resolution code is attributed in `THIRD-PARTY.md`; the upstream MIT
notice is retained in `LICENSE-MIT.txt` and included with downloads.

Character skill levels are separate live observations, not global catalogue
attributes. Names/annotations alone do not establish effective healing, buff
bonuses or a player's ownership of an effect.
