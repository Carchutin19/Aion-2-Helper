# References and attribution

Pet catalogue, portraits, offline maps, acquisition records and the MIT protocol
reference are documented separately in [PET-ASSETS.md](https://github.com/Carchutin19/Aion-2-Helper/blob/main/PET-ASSETS.md). The
original game artwork remains the property of NCSOFT and its respective owners;
the source notices and applicable licenses are preserved in the pet asset pack.

The intro placeholder in `assets/startup/blank-intro.bk2` was generated from our
own black image using the existing Unreal Engine Bink encoder. No external mod
asset or script is included; the encoder is not redistributed. Provenance and
generation instructions are in `assets/startup/README.md`.

The FPS module uses Intel's unmodified PresentMon 2.6.0 x64 console:

- https://github.com/GameTechDev/PresentMon/releases/tag/v2.6.0
- MIT license, Intel Corporation.
- Original notices in `tools/presentmon/LICENSE.txt` and
  `tools/presentmon/THIRD_PARTY.txt`, included with packaged binaries.
- Download and verification instructions in `tools/presentmon/README.md`.

`protocol/sync-opcodes.json` comes from:

- https://github.com/SkeeveAN/Aion-DPS-Meter
- Revision: `2ef71759738ffe902f72535ba8702e9e68d3b105`.
- File: `Client/assets/aion2/protocol/opcodes.json`.
- MIT license, copyright (c) 2026 SkeeveTV.
- The complete license text is included in `protocol/LICENSE-MIT.txt`.

Capture and decoding code is implemented in `src/`. Framing, decompression,
and stat format references were consulted in these projects:

- Aion DPS Meter, revision listed above.
- https://github.com/taengu/A2Tools-DPS-Meter,
  revision `82e53c1008ac4c2974446cc703703f473bbbed81` (GPL-3.0).
  Code from this repository is not copied or linked into the executables.

Reference checkouts and their original licenses remain in `research/`.
The specific energy field (`u32`, kind 3 in `008D`) was identified from a local
capture and verified by the user. It was not a stamina field already identified
in the consulted readers.

The experimental offline `tools/analyze_combat.py` also consults the MIT
Aion DPS Meter protocol reference above for direct damage and appearance frames.
Global-client party snapshot layout and additional-impact amounts were checked
against local observations. This tool is not integrated into released binaries.
The local live DPS prototype also uses independently implemented class metadata
reading informed by the MIT reference's wire class identifiers. Class coloring
is cosmetic and does not establish player identity or party membership.

Additional research reference:

- https://github.com/cyberbadger6969/aion2-dps-meter,
  revision `453c1634f325c4e87eec475a6b03844617564d5f` (GPL-3.0).
  Code is not copied or linked into the executables or offline tools.

Notification audio: **assets/sounds/party-invite.mp3** was supplied by the project
author for the party invitation notification and is included with the release.

Direct-impact tag research also consulted the protocol field descriptions in the
A2Tools reference above and the locally installed NotMeter reader. The observed
plotter byte, restoration integer and angle byte were checked against our own
recorded frames. Front/Back, Perfect and Double counters are implemented in our
own bounded reader; no external implementation is copied or linked.

## DPS skill catalogue and adapted parsing

The skill metadata and the adapted catalogue lookup, character skill-list
recognition and family-based combat classification consult/reuse the MIT project:

- https://github.com/SkeeveAN/Aion-DPS-Meter
- Revision: e767bf486d52407d91f5fea698a9fdc5c7d1d5af.
- Client/Aion2/Protocol/Aion2SkillNames.cs and Aion2FrameDecoder.cs;
  Client/assets/aion2/skills data files.
- Copyright (c) 2026 SkeeveTV. Complete notice in assets/skills/LICENSE-MIT.txt,
  included with packaged binaries. The catalogue is embedded in the executable.
- Dataset transformation/provenance: assets/skills/README.md. Names/text are
  Aion 2 game metadata; the code licence does not claim ownership of game content.

No third-party application, backend, external updater, telemetry/upload service or
icon collection is required. The built-in updater contacts this project's GitHub
release API and download endpoints; it does not upload gameplay data. Character levels and actual impacts are read passively
from the existing game stream. The rest of the application's licence status is
unchanged; these referenced/adapted portions retain their applicable MIT notice.

Enemy-health display names use the MIT reference NPC catalogue from SkeeveAN/Aion-DPS-Meter, revision ce50cb1c28c95dfde0f776c32ad555c1febfe79b. Provenance and retained notice are in assets/npcs. Target/aim framing was consulted and independently checked against controlled local Tab, unlocked attacks, area damage and death recordings. No external meter binary is shipped.

NPC creature classifications in `assets/npcs/species.json` are transformed from the same MIT-licensed SkeeveAN/Aion-DPS-Meter NPC/pet catalogue; EN/ES type labels come from its extracted game species labels. The notice remains in `assets/npcs/LICENSE-MIT.txt`. Game data and names remain NCSOFT content. Unmapped types are left unknown.

## Optional exact enemy distance

The read-only session-key locator and client movement/RC4 implementation adapt
the MIT protocol research from https://github.com/lunixxx37/aion2-protocol-research,
revision `fe2122f0e54e1de854c11e91e3c7bf51916a4f52`, specifically
`tools/locate_session_key.py`, `tools/watch_session_key.py`,
`tools/live_opcode_viewer.py` and `tools/aion2_client_crypto.py`.
Copyright (c) 2026 Aion 2 Protocol Research contributors. The complete notice is
retained in `assets/licenses/Aion2ProtocolResearch-MIT.txt` and embedded in the
executable. This optional module is implemented in C#; no external application
or Python runtime is required. Keys are held only in memory and are not logged
or saved. The default mode remains passive server packet reading.


Additional NPC display names in the already supported 2,900,000–2,999,999 range
are transformed from SkeeveAN/Aion-DPS-Meter revision
`2ef71759738ffe902f72535ba8702e9e68d3b105`,
`Client/assets/aion2/npcs/npc_names.json`. English and available Spanish labels
are retained; existing local entries are preserved. The upstream MIT notice is
included in `assets/npcs/LICENSE-MIT.txt`. Game names remain NCSOFT content.

Gathering resource names and broad groups derive from SkeeveAN/Aion-DPS-Meter
revision `14b06ef58611bbe7a670196cbfa39ad868de3ee3`,
`Client/assets/aion2/maps/gather.json` (MIT, SkeeveTV). The retained notice and
precise metadata provenance are in `assets/gathering/`; the source description
is embedded as `GatheringSources.txt`. Game names remain NCSOFT content.
Our gathering protocol reader and segmented renderer are original code validated
against controlled local incoming captures; no upstream application code is copied.

The Windows package retains the identical SkeeveTV MIT copyright and permission
notice for both gathering and NPC metadata in `assets/npcs/LICENSE-MIT.txt`.
