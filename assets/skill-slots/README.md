# Skill slots: sources and limits

The Helper's skill-slot receiver, layout editor and renderer are original code.
Wire-format research: SkeeveAN/Aion-DPS-Meter, documentation at revision
8b08696f9fbd13d6d1fb6e395add3fe75504da84 (skill-frames.md and cooldown-frames.md).
https://github.com/SkeeveAN/Aion-DPS-Meter

The bundled icon-token mapping and 378 PNG skill portraits were obtained from
Client/assets/aion2/skills at revision
65c55c96996d585331757ecd0e28cb80b1813c24. See LICENSE-MIT.txt for the upstream
software/data notice. Skill artwork belongs to NCSOFT and its respective rights
holders; the software MIT licence is not a claim to ownership or an independent
MIT licence for game artwork. This is an unofficial fan project, not endorsed
by NCSOFT. No game archives, encryption keys, extractor or upstream executable
are included or executed. Icons are embedded locally; no runtime requests.

Supported: validated 0056 active-bar entries, 0556 partial changes, 4738 partial
cooldown updates, explicit 1838/1938 chain windows. Items and empty entries are
omitted. A confirmed own 3336 profile binds state to its flow/map. Shared cooldown
groups without a proven mapping remain unmapped. Key labels are manual.

Cooldowns start from the explicit millisecond interval in a complete incoming
0238 message whose source is the confirmed local actor on the active flow.
The own actor is checked before accepting an interval. Positive intervals start
or restart a timeline; a zero-duration multistage component never erases an
already running cooldown. Later 4738 entries rebase that timeline (including
zero resets). Natural expiry of a server-provided interval restores ready.
The prior experimental 2400-unit conversion was incorrect for these messages
and has been removed: six recorded Hellfire casts carry41600ms, matching the
user's native42s display. The first live test reproduced the missing cast-start
path; only reading4738 corrections was insufficient.

Additional wire-layout reference: Kuroukihime/AIon2-Dps-Meter, revision
ada90dd2bfc9eff5d1d8bc0d8e3de846f6e80591,
AionDpsMeter.Services/PacketProcessing/Processors/SkillCastProcessor.cs and
SkillCdUpdateProcessor.cs. That project is GPL-3.0; it is consulted only for
protocol-field research. No implementation, assembly or asset from it is
copied, linked or executed. Our independently written reader validates bounds,
exact layout length, source actor and flow; its interpretation was checked
against all1476 own casts in the private test capture.

Cast messages with charge-specific flags are parsed but do not start a normal
single-use timer until their availability/reload semantics are separately
validated. Contextual macros beyond explicit chains and unmapped shared groups
also remain experimental. Unknown cooldown state is never populated with a
constant or a guessed maximum. Labels are manual; empty labels stay hidden.

Three-stage windows are propagated to the original slot: receiving B->C advances
A->B to A->C. A close for B removes only destinations still showing B; it must
not remove the already-open C. The recorded Flame Arrow -> Burst -> Pyroclasm
sequence (1838 B->C followed by1938 B in the same tick) is covered by a regression
that fails on the earlier implementation. Closing C restores the original icon.
Missing cooldown data has no overlay text; it remains null in diagnostics.

The assigned Bittercold Wind/Blaze pair additionally uses the explicitly selected
target (3138/3438), the exact recorded Fire Mark effect157100071 and the primary
skill's received cooldown. Both conditions are required; other fire effects,
other targets and cooldown alone do not qualify. Mark snapshots/refreshes retain
the absolute server expiry mapped through a recent0036 server-clock sample on
the same flow, rather than assuming server/local UTC clocks coincide or extending
a snapshot by the original duration;
2c38 instance removal, target clear, despawn and flow/map/identity reset revoke it.
The selected alternative renders its own received cooldown. This is a validated
individual conditional pair, not a universal contextual-skill rules engine.
Other effect variants and conditional pairs remain unsupported until verified.

The editor lists only detected assignments, with consecutive numbers, portraits
and catalog names. Internal protocol positions remain private implementation
keys; they are not offered as48 configurable skills or inferred keyboard keys.
Empty custom labels use the consecutive detected number. Existing custom labels
are retained; clearing a custom label restores its automatic number without a
separate renumbering control. Individual hide preserves the assignment, geometry and label
and keeps the editor choice available for restoration. Last detected assignment
metadata is saved for offline editing and clearly labelled as such; it never
provides live identity, cooldowns or availability for a new session.

Test hotbar / Stop test is one toggle. Its simulated12second countdown repeats
after a brief ready state until stopped, using detected slot identities when
available. It does not change the activation preference or live cooldown ledger.
It shares the overlay/settings timers; no new timer is started for the test.

Hotbar character presets are stored separately in hotbar-profiles.json. Identity
uses the same validated own3336 name+server combination as pets, independent of
temporary actor/flow IDs. Names/server are displayed with saved/waiting/live
states. The initial existing layout is retained for the first identified
character; later new characters do not inherit another character's assignments,
custom keys or hidden slots. Returning characters recover their own options.
Unknown server identity is not saved into the previous character. Character
switches rebase hotbar undo snapshots while retaining other modules' history.
Native game-bar visibility is global and excluded from character presets.
An incompatible/corrupt preset store is preserved instead of overwritten.

Native hotbar hiding is a separate local-settings option under Hotbar. It reads
DeviceSetting_V2.dat directly; users do not need to export or import a HUD.
Only QuickSlotSkill1/QuickSlotSkill2/QuickSlotStigma Hide flags in populated saved
presets are changed. The game must be closed, with the Helper left open to apply
a pending change. Nullable/default-unset preferences never modify native files.
Before every actual change, the exact source is backed up under
LocalAppData/Aion2Helper/HotbarBackups. A per-record/preset journal restores the
previous visibility without restoring old chat positions, graphics or other HUD
fields. The writer validates lengths, UTF-8, JSON and exact semantic differences,
preserves unrelated binary headers/text, and rejects unrecognized formats.
Multiple game-settings branches require explicit selection if no selected game
configuration identifies one. No process-memory writes or injected input are used.

HUD-format reference: victoriousian/AION2_HUD_Editor_V2, constants.py/codec.py,
revision ed41d138275e31c46ee8a232a3719e593ddd4e1e:
https://github.com/victoriousian/AION2_HUD_Editor_V2
Consulted for format facts (fixed obfuscation mask and HUD tag names) only; no
upstream code or executable is copied or run. The current UTF-8 V2 reader and
lossless writer are original implementations. Native Hide flags were first
validated through a user-imported copy; the automatic application/restoration
flow still requires the first live close/relaunch check.
