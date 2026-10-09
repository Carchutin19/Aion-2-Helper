# References and attribution

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
