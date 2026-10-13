# Skill metadata

`catalogue.json` is a transformed ID/name and classification dataset from
[SkeeveAN/Aion-DPS-Meter](https://github.com/SkeeveAN/Aion-DPS-Meter), revision
`e767bf486d52407d91f5fea698a9fdc5c7d1d5af`, under `Client/assets/aion2/skills`.
It retains English names, available Spanish text, active/passive annotations,
periodic annotations, heal/damage families and the non-damage watchlist.
No buff amplification percentages, icons, external application or network
service are included. The catalogue is embedded in the helper; missing display
metadata must never discard an otherwise supported combat event.

The reference names originate from Aion 2 client metadata, as documented in the
upstream skill-data README. Game names/text remain NCSOFT's content; the upstream
MIT code licence does not claim ownership of that content. Adapted decoding and
metadata-resolution code is attributed in `THIRD-PARTY.md`; the upstream MIT
notice is retained in `LICENSE-MIT.txt` and included with downloads.

Character skill levels are separate live observations, not global catalogue
attributes. Names/annotations alone do not establish effective healing, buff
bonuses or a player's ownership of an effect.
