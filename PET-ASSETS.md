# Pet data, artwork and acknowledgements

The Pets feature in Aion 2 Helper uses independently implemented application,
protocol, registry, notification and atlas code. This document records the origin
of the separately licensed reference data and artwork. It is deliberately kept
outside the main project description.

## Asset snapshot

The unmodified data and images in `assets/pets/pets.zip` were obtained from
[keytftw/aion2pettracker](https://github.com/keytftw/aion2pettracker), revision
`8a57ba3c36ce0cccb9c783c35ffba4fd2983c3f6` (reviewed 20 October 2026).

The ZIP is a development/source asset. Published executables embed that archive
as `PetAssets.zip`, including this notice, so the existing updater does not need
to install hundreds of additional files. Portraits and the current map are
decoded on demand; embedding the pack does not load all six maps into memory.

The archive preserves the source paths, original notices and license texts:

- `data/pet-catalog.json`: 207 catalogue identifiers, including seven Special
  entries. This is not a claim that the game has 207 ordinary collection pets.
- `data/pets/*.png`: 207 portraits, published as 64 px PNG conversions of the
  Questlog game-data snapshot in xCROv's tracker.
- `data/maps/spawns.json`: recorded farming locations for 160 pets and map
  associations for 200 catalogue entries.
- `data/maps/*.png` and `*.webp`: six maps, including the original WebP files
  supplied by the source project. The recorded location snapshot is dated
  10 October 2026. Locations are references, not live spawns or guaranteed drops.
- `data/pet-sources.json`: short acquisition facts and per-entry source links
  to [Wikily](https://wikily.gg/aion-2/pets/). Unconfirmed entries remain marked
  as unconfirmed. The source text is in English.
- `data/PET-SOURCES.md`, `THIRD_PARTY_NOTICES.md`, `LICENSE` and the applicable
  files in `licenses/`: retained upstream provenance and notices.

The upstream project traces the catalogue, portraits, maps and location snapshot
to [xCROv/aion2-pet-tracker](https://github.com/xCROv/aion2-pet-tracker), revision
`93da8f0a4d672210eb455c20bbea7dcc9191cd5b`, under GPL-3.0. Its notes also identify
[Questlog](https://questlog.gg/aion-2/en/db/pets) as the underlying game-data source.
Fallback region records are attributed to
[MartinLVila/aion2-pet-overlay](https://github.com/MartinLVila/aion2-pet-overlay),
revision `211ab2aa7216813dd913f4d8521d860e9ccb3e0e`, under MIT, with MetaBot as the
listed source. Their original license texts are retained in the asset archive.

The GPL notices apply to the material distributed under those notices. They do
not assert ownership of NCSOFT's artwork or grant additional rights over it.
The source files are included unchanged in the same archive; the upstream source
links and pinned revisions above identify the corresponding public sources.
No GPL application/parser/UI source from those trackers is copied into the
Helper executable.

## Game artwork and trademarks

**AION 2 names, portraits, maps, game artwork and trademarks belong to NCSOFT
and their respective rights holders.** Aion 2 Helper is an independent fan
project and is not affiliated with, sponsored by or endorsed by NCSOFT,
Questlog, MetaBot, Wikily or the referenced tracker authors.

These assets identify game content in the companion interface. This attribution
does not claim an express NCSOFT redistribution authorization. Rights in the
underlying game artwork remain with their owners.

## Protocol reference and independent implementation

The collection and soul-message layouts were reviewed against the
[MIT protocol documentation from elkaida/AION2-pet-tracker](https://github.com/elkaida/AION2-pet-tracker/blob/main/docs/PROTOCOL.md),
revision `7a8afb636c0516f07cc45918687142d77c94bd5a`. Its MIT notice is preserved
as `licenses/elkaida-pet-tracker-MIT.txt` inside the asset archive.

Helper's readers are original implementations, independently checked against
previously recorded user sessions. The collection baseline, per-character and
server identity, actual numeric Pet ID soul gains, packet ordering and invalid
message handling are validated separately from the visuals. Attacks, kills and
unattributed crystal messages do not increment pet souls.

The stages use 5, 25 and 75 souls within each stage, reaching three stars/MAX
at 105 cumulative souls. Cached historical progress is not silently treated as
a fresh live collection. Collection or protocol gaps can require a new login
snapshot. Recorded coordinates and acquisition sources do not establish drop
rates, fastest routes, live enemy presence or official approval of this tool.
