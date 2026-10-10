# Experimental offline combat analysis

`analyze_combat.py` is a development tool, not a released DPS widget. It reads
`frames.jsonl` produced by our existing capture analysis and writes an explicitly
incomplete report. It does not start a capture or touch the running game.

```
python tools/analyze_combat.py captures/<session>/frames.jsonl --self <name> --party <friend> --output captures/<session>/combat.json
python tools/test_analyze_combat.py
```

Fourteen synthetic tests cover malformed/truncated integers and damage blocks,
included additional impacts, variable-length scalar fields, identity conflicts,
companion/self-effect exclusions, common encounter duration, idle reset, filters,
observed global-client party join/leave snapshot replacement, periodic field
order/bounds, structural effect parents, expiry, reused entities, and exclusion
of support/potion/unknown effects from candidate totals.

Reports include direct damage from identified players attacking observed NPCs.
Two observed Sorcerer effect spawn types can be linked through their explicit
parent field. Only the independently checked global spawn shape is accepted;
unknown shapes clear prior ownership. Parent links are applied after spawning,
expire after 120 seconds, and require a known player. No class/proximity guess is
used. Other summons and effect types remain pending.

The observed `0538` envelope distinguishes pending magnitude from current
impact. Its amount fields also carry healing, shields and resources, so field
presence alone does not classify damage. Fire Wall ticks remain candidates
pending exact visual comparison. Default reports exclude them. For research:

```
python tools/analyze_combat.py captures/<session>/frames.jsonl --self <name> --candidate-periodic --output captures/<session>/candidate-combat.json
```

This explicit opt-in accepts only the observed Fire Wall skill/effect/flag
combination and reports `candidatePeriodicDamage` separately. It does not enable
poison, other burns, support effects, healing or arbitrary periodic messages.
`completeDamage` remains false in both modes.

Current global-client `0092` snapshots containing the specified local
character replace party membership; manually supplied names are the fallback
before discovery. An observed leave snapshot contains only the local character.
The observed `0D92` join update carries one member and appends that member to
the existing roster; it must not replace the full snapshot or discard self.

The global view covers players whose events reached this client. It does not
cover an entire server or map. All rows use the same encounter duration; changing
the visible filter does not recalculate its duration or erase global totals.
Additional impacts are already included in the wire total and are not added twice.
NPC HP scaling has not been resolved and is not used to adjust damage amounts.

Default idle gap: 10 seconds (1–120 configurable). Duration has a one-second
minimum. Output retains at most 32 encounters, with 512 actors each, and bounds
identity/NPC records. Parsing streams from disk without retaining packet payloads.
UI refresh settings now exist in a local development widget. A controlled
performance comparison of this new widget has not been completed yet.

## Local live prototype

The development build includes an experimental DPS overlay and Settings page,
using the same decoded packet stream as the energy bar. It supports self, party,
and nearby player filters, refresh interval, idle reset, color/background,
position/size, enable/disable, and the existing shared widget lock and undo/redo.
It is not part of the currently published v1.2.2 release.

### Local combat metrics

The widget has Damage, Healing done and Healing received views. Each uses the
same encounter clock and retained totals; changing view does not reset combat.
Bars, ordering and per-second values follow the selected view. Both critical
columns describe primary direct impacts with recognized normal/critical result
kinds: amount share from critical primary impacts and observed critical event
frequency. Bundled extra impacts have no independently confirmed individual
critical flags and are excluded from these denominators. Periodic ticks are also
excluded. No eligible direct sample displays a dash. This is observed combat
frequency, not the character's theoretical critical chance.

Healing totals are raw restoration announced by the supported direct/periodic
skill families, attributed separately to source and recipient, including self
healing once in each total. They are not effective healing or overhealing.
Unknown players, NPC sources, potions, support/resource buffs and unsupported
families are excluded. Healing critical kind uses the shared direct-result
envelope; the current real samples contain normal healing only, so positive
healing-critical gameplay validation remains pending.

The live widget now includes identified damage-over-time families automatically,
with no experimental tick switch. Five additional skill/effect pairs were
independently correlated with repeated exact NPC HP decreases across several
targets, beyond the earlier Fire Wall family. Only the current tick amount is
added, never the pending amount. Unclassified periodic messages remain excluded;
this is not complete coverage of every poison/burn. The older offline CLI above
retains its separate research opt-in and has not been converted into the live
reader. Local tests cover metrics, exclusions, critical denominators, view
persistence/sorting/undo, automatic tick migration and existing render/resources.

The local widget now supports configurable role colors (damage, healer/support,
tank), bar smoothing, optional idle hiding and fade duration. Role coloring uses
validated class metadata where available, with class skill families as a cosmetic
fallback for already identified damage sources. It does not assign identity,
party membership or summon ownership. Unknown classes retain a fallback color.
Two optional appearance settings show the local player as "You" / "Tú" using
the helper language and keep the local row first regardless of damage. Both
default off. Pinning preserves descending order for the selected metric for other
players and normalizes bars against the highest total, rather than the pinned
first row.
These changes do not reset the encounter or alter identity, filters or totals.
Incoming observed NPC hits also reset the selected player's visibility timer
without entering damage totals. This is based on observed activity, not a fully
decoded game combat-state flag.

Only animation uses a faster timer; combat snapshots retain the selected refresh
interval. Text/background images are cached during bar movement, and opacity
transitions reuse the uploaded bitmap. Idle hiding defaults off, with a 15-second
delay when enabled; fade defaults to 0.2 seconds and smoothing to 0.15 seconds.

Unlocked DPS geometry stays inside the monitor's usable area, including reserved
space for an autohide taskbar. Right-click the unlocked DPS widget to access the
shared lock menu. The DPS panel yields to Windows shell UI so it does not cover
the notification icon popup. Native autohide appbar lookup follows the
[Windows API](https://learn.microsoft.com/en-us/windows/win32/shell/abm-getautohidebarex).

Direct damage and the party display were confirmed locally with two players.
The reader now retains a roster received before local-character identification
and can recover an already-created party from the independently observed
ongoing `1B92` member vital envelope. Recovery requires a validated player name,
the same gameplay stream, a structurally valid envelope, and a local character
identified by energy telemetry or a validated local-character appearance. It does not use proximity or shared targets.
Full rosters replace recovered membership; leave notices clear or remove it.
Members recovered only from ongoing telemetry expire after two minutes without
another update. Transport changes and character changes clear all membership.
This layout has been checked with the current two-player global-client captures;
Instance rosters and player appearances have also been checked in five-player
dungeon captures. Origin server and instance realm are distinct roster fields.
Other party and alliance layouts still require validation.

Dungeon transitions retain a bounded pending roster until the new gameplay
connection is identified. Incoming traffic from an untracked game-port flow
triggers a throttled Windows process-ownership lookup, so the initial character
and roster messages can be accepted before the normal two-second maintenance
refresh. It does not accept connections belonging to unrelated processes.

A same-map teleport on the same connection retains enemy identities, party
membership and damage. Genuine map, connection and character changes discard
stale entities. A private replay of a reported final-boss failure now continues
accepting the five players' damage after the teleport. Three further local
dungeon repetitions retained five-member party data and final-boss damage
through the end of each observed fight. One entry missed the initial map/roster
envelopes and recovered membership from ongoing telemetry with a delay.
This validation does not establish complete damage coverage.

Until a first dash identifies the local character, a notice remains visible in
the first player-row position regardless of the idle-hide setting. Normal idle
hiding resumes once identification succeeds. A local replay with join/roster
messages removed recovered the two party players and excluded six observed
nearby players. Unknown targets/actors and unverified effects are still skipped,
so this remains a partial damage meter.

The C# reader has bounded entity/actor collections, no second production packet
capture, cached widget rendering, and synthetic parsing/UI/resource checks.
`--dps-test` runs those checks; `--dps-replay-test <frames.jsonl>` exercises the
reader against a private capture. Passing a replay is not a proof of complete
damage accuracy or a replacement for gameplay performance measurements.

Raw captures and generated reports contain private identities/connection data;
keep them in the ignored `captures/` directory. Tests use synthetic names and
records only. Protocol references and notices are listed in `THIRD-PARTY.md`.

The widget has localized Damage, Heals and Received tabs. They change the same
saved combat view as Settings, retain all accumulated metrics, and support the
shared undo/redo history. Tabs remain clickable while widgets are locked. A
small separate input surface covers only their strip; the locked main widget
remains transparent to mouse input. The strip does not activate over the game,
has no timer of its own, and hides with the widget or while the tray menu is open.

Settings > DPS Meter > History stores completed encounters locally in
`combat-history.json`. Each fight retains every identified player's accepted
damage, supported DoT, primary critical amounts/counts, healing performed and
received, and DPS/HPS. Storage is independent of the widget scope, view and row
limit. Filter by player to inspect or export one player's rows; leave All players
selected for the complete table. TXT export also describes metric limitations.

The configured combat inactivity timeout closes an encounter once. A reset,
connection/area change, disabling the meter, or closing the helper records the
unfinished encounter with that reason. Same-map boss teleports preserve the
encounter. This follows observed accepted combat events, rather than claiming
an independently decoded game combat-state flag.

History survives helper restarts. Default storage limit is 100 fights, adjustable
from 1 to 1000, removing the oldest first. Age cleanup defaults off; configure
hours or days when enabled. It runs at startup, after changes/new records, and
once a minute using the existing maintenance timer. Individual/all deletion and
retention pruning cannot be undone. History remains accessible with DPS disabled.

No raw packet capture is added. Snapshots are copied only at encounter completion;
disk serialization happens on a coalescing background writer with atomic file
replacement. Corrupt history is preserved in a separate local backup. Personal
history, temporary files and backups are ignored by Git and the release package
uses an explicit allowlist. Earlier research captures are not automatically
imported into the user history.
