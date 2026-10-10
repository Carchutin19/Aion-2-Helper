# References and attribution

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
