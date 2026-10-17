# Aion 2 Helper v1.15.1 Alpha — Hotfix: Player Bars & Shields

This hotfix corrects a confirmed HP/MP data-reset issue and adds the shield
layer missing from the custom player health bar. Existing settings, layouts,
combat history and game-tweak backups are preserved.

## 1. Player HP/MP visibility correction

- A late **first map announcement** on a connection no longer clears an already
  confirmed character profile and its explicit HP/MP maxima. This fixes a
  reproduced cause of player bars disappearing even though valid resource
  updates were still arriving.
- Actual changes between known maps, character replacement, connection resets
  and stale data still clear or hide the previous readings. Maxima and identities
  are not copied between connections or replaced with guessed values.
- The correction was reproduced and verified with an earlier captured session.
  The specific entry into the reported Beriutris aerial-assault instance was not
  captured and still needs a live retest when that event returns; this is not a
  claim that every instance-related visibility issue has been resolved.

## 2. Shield support on the player health bar

- Supported shields appear as a **gold/yellow fill over the HP bar**, without a
  separate border. The fill runs left to right and stays inside the configured
  bar length. It represents confirmed remaining shield relative to maximum HP,
  capped visually at the bar width; actual HP numbers and maximum are unchanged.
- Configure **Progress bar → Player health → Appearance → Shield**: enable or
  disable the layer, choose its color, adjust opacity, emissive glow and glow
  intensity. It follows the health widget's position and whole-widget scale.
- A separate **Shield preview** slider lets you inspect 0–100% shield coverage
  independently of the HP preview. These controls support English/Spanish,
  automatic saving and undo/redo. The MP bar is unchanged.
- The fill follows received application, remaining-capacity and removal data,
  including absorption by enemy hits and natural expiration. The received
  duration provides a safety cutoff; it never creates gradual numerical decay.
  Missing remaining capacity is not automatically interpreted as zero.
- A visible supported shield keeps the health bar available at full HP, even
  with full-health auto-hide enabled. Normal auto-hide resumes after removal.
- Exact verified shield effects are excluded from DPS damage/healing parsing,
  so remaining capacity and absorbed damage cannot be counted as healing just
  because the associated skill also has a healing tag.
- Tracking uses the existing incoming receiver and shared timer, with bounded
  state and cached rendering. No additional polling timer, game-memory access
  or outgoing capture is introduced by HP/MP or shield tracking.

## Validation and limits

The author confirmed the new shield layer working in the game. Controlled tests
and production-reader replays cover a skill shield and an attack-triggered
shield: application, damage absorption, depletion and expiration. The replays
also retain the previously checked HP/MP consumption, poison and equipment-max
changes. Parser, rendering, settings and existing combat/progress checks pass.
Raw captures and personal diagnostics remain private and are not bundled.

**Initial support covers the two verified shield effects**, rather than every
class or buff. Simultaneous shield stacking is not yet validated: if supported
shields overlap, the widget shows the **largest confirmed active amount**, not
an unverified sum. Other shield families need further capture-based validation.
A shield already active before the helper receives its status and capacity may
remain unavailable until new supported data arrives.

Missing initial character profiles can still require character selection and
reentry with the helper open. Wider classes, maps, death/respawn and MP-maximum
changes still need testing. The application remains **Alpha**, DPS remains
**Beta**, and enemy/target bars and exact distance remain **experimental**.
Existing open-world boss party-DPS and Energy PvP issues remain unresolved.

## Updating

Use **General → Application updates → Check for updates → Download and update**,
or download the Windows ZIP. Npcap is installed separately. Open the helper
before entering your character; if initial HP/MP data is missing, return to
character selection and enter again. For the optional exact-distance reader,
restart Aion with the updated helper already open and the option enabled.

Join the [Aion 2 Helper Discord](https://discord.gg/yf7SDZMyd8) for feedback,
community discussion and future updates.

---

# Aion 2 Helper v1.15.0 Alpha — Player HP & MP Bars

This release adds independent custom health and MP bars for your character,
with live received values, full visual settings and a shared widget-lock switch
in General. The application remains Alpha, the DPS Meter remains **Beta**, and
enemy bars and exact distance remain **experimental**.

## Live Player health and Player MP

- **Settings → Progress bar → Player health / Player MP:** two separate widgets,
  each with its own enable switch, position, dimensions and whole-widget scale.
  New installations start with both modules disabled; enable the ones you want.
- Shows actual received current value, explicit maximum and percentage. HP
  damage, periodic damage, recovery and equipment-related maximum changes were
  checked locally; MP consumption and recovery were compared with the game.
- The maximum is read from supported character/resource messages; it is never
  inferred from the highest value seen. Other players' resource updates cannot
  replace your character's readings.
- The bars share the existing incoming Npcap receiver. HP/MP tracking does not
  add game-memory access, outgoing packet capture or a second capture worker.
  Capture remains active when an enabled HP/MP bar is the only module needing it.

**Open the helper before entering your character.** If you enable the bars after
entry or start the helper late and values are missing, keep the helper open,
return to character selection and enter again. Locked bars hide when confirmed
data is missing or stale; edit mode can show an illustrative sample instead.
Preview sample values are never presented as confirmed live readings.

## Appearance, preview and layout

- Green HP and magenta MP capsule designs, with configurable colors, optional
  emissive glow, glow intensity, dark track, opacity, frame and frame thickness.
- Label, current value, maximum and percentage each have independent visibility,
  font size, bold, color, above/below left/center/right position, horizontal and
  vertical offsets, and optional black outline with thickness and opacity.
- The maximum can follow the current value or use its own position. Whole-bar
  scale includes text and outlines, with screen/reference adaptation preserving
  individually authored settings.
- **0–100% preview sliders** sit directly below each preview, letting you inspect
  the fill and configured color transitions. Turning off **Color by level**
  disables high/low color selectors; the medium color remains the fixed color.
  Stored high/low colors are retained for when the option is enabled again.
- Narrow bars reflow colliding labels rather than reducing chosen font sizes or
  overwriting saved positions. Bitmap-DPI text measurement avoids inflated text
  widths on scaled monitors. Free text positions remain configurable.
- Auto-hide at full, hold delay, fade and smoothing use the existing progress-bar
  animation model. Actual numbers update from received data while the fill can
  interpolate smoothly. Static readings reuse cached images; both new bars use
  the existing shared adaptive timer.
- English/Spanish, automatic saving, undo/redo and display-layout migration
  include both new modules. Progress bar now presents six modules in two rows.

## Lock or unlock widgets from General

- **General → Lock widgets** provides the shared lock directly inside Settings.
  Turn it off to move and resize enabled widgets, then turn it on to restore
  normal locked interaction. It is synchronized with the tray-menu lock command.
- Removed the redundant **Show editing sample** and **Lock widgets** action
  buttons from the Player health and Player MP preview cards. Their enable
  switches, preview sliders and General's shared lock cover these actions.
- README order is now description, Download, Discord, optional project support,
  then the latest update notes.

## Validation and current limits

The author confirmed both live bars match the game after character reentry.
A private bounded capture replay reproduced the reported initial HP/MP,
MP consumption/recovery, consecutive poison HP values and the HP maximum
decrease/restore from equipment changes. No raw captures are published or bundled.

Tests cover own-actor/connection isolation, explicit maxima, zero resources,
out-of-order and malformed/truncated input, map/transport resets, stale data,
narrow layouts, DPI, real label values during smoothing, rendering caches and
resource bounds. Existing progress, enemy/target, combat and settings checks
remain part of release verification.

Broader live testing across classes, zones, reconnects and death/respawn is still
needed. A controlled MP-maximum equipment change was not observed: explicit
MP maxima are read from supported character/party-vital messages; unsupported
stat kinds are not guessed. Missing initial profiles can require character reentry.

**Existing issues remain:** open-world boss party DPS can stop updating, Energy
Bar PvP is unresolved, and some player names or target-health profiles may be
late or missing. This release does not add world-boss tracking or Spacetime Rift
alerts and makes no claim of improved in-game FPS.

## Updating

Use **General → Application updates → Check for updates → Download and update**,
or download the Windows ZIP. Personal settings, combat history and tweak backups
are preserved. Older versions before v1.13.0 need a manual ZIP to get the updater.
Npcap is installed separately. For the optional exact-distance reader, restart
Aion with the updated helper already open and exact distance enabled.

Join the [Aion 2 Helper Discord](https://discord.gg/yf7SDZMyd8) for updates,
community discussion and feedback.

---

# Aion 2 Helper v1.14.0 Alpha — Notification Redesign & Performance Improvements

This release refreshes all four notification types and reduces unnecessary work
in FPS fallback parsing, ping bookkeeping and progress-bar drawing. It also
includes targeted DPS identity and experimental distance-session corrections.
The application remains Alpha, the DPS Meter remains **Beta**, and enemy health,
enemy target and exact distance remain **experimental**.

## Redesigned notifications

- Illustrated notifications are now the default, with dedicated artwork for
  normal party invitations, dungeon-party invitations and Shugo Festival.
  Shugo's five-minute reminder and registration-opening notice share an icon,
  while keeping separate messages and independent enable switches.
- Icons, the pure-black transparent gradient and localized text are composed
  separately. The background fits the actual message and wraps long names.
  Dungeon artwork includes the person and stone emblem, without a crescent moon.
- **Simplified notifications**, off by default, restores the compact text-only
  design. Existing sound, volume, duration, fade, color and category preferences
  are retained. The master switch preserves individual notification choices.
- Whole-notification scale grows around the saved center, including resizing.
  Text size can be adjusted independently. Maximum width controls wrapping;
  icon sizes are normalized across categories.
- Optional **Icon glow** with its own intensity control retains the soft
  emissive appearance of the approved designs. Glow stays with the artwork;
  the text background remains a separate black-alpha gradient.
- A **Preview type** selector shows the generic sample, both Shugo messages,
  normal party and dungeon invitations in the selected style. One generic
  **Test notification** button checks the visual and configured sound.
  Invitation examples use Carchutin; live notices show the actual inviter.
- Artwork is embedded in the executable, decoded once per category and cached
  at a reduced working size. Notice holds and fades reuse their uploaded image.
  English/Spanish, automatic saving, undo/redo and click-through behavior remain.

Shugo reminders still use the hourly :55/:00 schedule while Aion is running;
they do not query server registration state or replay missed alerts. This
release does not add Spacetime Rift or world-boss notifications.

## FPS, ping and progress-bar optimization

- The **PresentMon fallback** extracts only the four fields needed for FPS
  instead of splitting every CSV column. Quoted/escaped fields, reordered
  headers and BOM headers remain supported. Native DXGI counting and requested
  refresh rates are unchanged.
- Ping reuses a stable endpoint key per connection instead of rebuilding its
  string during discovery and cleanup. Sampling intervals, expiration,
  ownership and error handling are unchanged.
- Locked Energy, Stagger, Enemy health and Enemy target widgets draw their
  shared bar core directly into the final emissive bitmap, avoiding a temporary
  bitmap/Graphics pair. Edit mode retains the previous compositing path.
  A 256-case before/after comparison produced identical pixels.

Five-run median synthetic measurements on the development PC:

| Isolated workload | Before | After |
| --- | ---: | ---: |
| 1,500 locked bar renders | 63.368 ms | 50.919 ms |
| 20,000 wide fallback CSV records | 67.770 ms | 7.098 ms |
| Managed allocation for that CSV workload | 130.72 MB | 9.22 MB |

These are isolated implementation benchmarks, **not** measured whole-helper
CPU savings or an increase in in-game FPS. The CSV improvement applies to the
fallback path, not native DXGI collection. Unchanged widgets were already cached.

## DPS identity handling

Validated remote-player profiles that arrive before the gameplay connection is
selected are now retained in a small, expiring, connection-specific staging
buffer. This prevents that particular ordering from discarding explicit names.
Conflicting, expired and future-dated profiles are rejected; transport reset or
disabling DPS clears the buffer. Names and party membership are never guessed
from class/order, and damage is not replayed or counted twice.

Five offline production-parser comparisons covered **931,921 decoded frames**
and **8,498 aligned snapshots**, with unchanged per-player combat rows. This
does **not** prove that all temporary Player + ID rows are fixed: a profile that
the server sends late can still leave a temporary anonymous row. First-entry
party-name behavior still needs repeated live checks.

## Experimental distance-session recovery

- A new encrypted setup cancels obsolete lookup work and clears the previous
  session's retry delay. An obsolete result/error cannot overwrite the active
  session's state or restore its old backoff.
- Candidate validation is tied to the session requesting the lookup, so an old
  pending connection's key cannot satisfy a new connection's request.
- Before a wider bounded search, the reader may recheck the one previously
  successful 64-KiB allocation. Its current memory attributes, supported
  structure and actual new setup must all validate. It is only a search hint;
  process restart/disposal clears it and nothing is persisted to disk.

Synthetic regression tests reproduced these failures before the corrections
and pass afterwards. Repeated real instance transitions and recovery time still
need live validation; this is not a guarantee that missing metres are eliminated.
The optional memory-read setting remains off by default and its scope is unchanged.

## Updating and validation

Use **General → Application updates → Check for updates → Download and update**,
or download the Windows ZIP. Settings, combat history and game-tweak backups are
preserved. Versions before v1.13.0 need a manual download to obtain the updater.
The previous v1.13.1 in-app upgrade was confirmed working by the author and a
second user; this release also checks staging, preservation and rollback locally.

**Open the helper before Aion 2.** For the optional exact-distance reader, restart
Aion with the updated helper already running and the option enabled. Missing
handshakes, initial profiles or supported coordinates can still hide data.

Release verification covers notification layouts/modes/EN-ES/Unicode, native
category gates/fades/caching and resource bounds; shared Settings/undo/geometry;
FPS/ping, DPS, synthetic session lookup, enemy/target/details and progress bars;
updater staging/rollback and the packaged executable/resources. The notification
designs were reviewed by the author locally. No new live dungeon/session-key
test is claimed for the optimization/recovery changes in this release.

**Known issues remain:** open-world boss party damage may stop updating; Energy
Bar PvP is unresolved; some player names/target-health profiles can arrive late
or be missing. Unknown combat effects, effective healing, causal buff bonuses,
per-player stagger and universal boss identification are not added or inferred.

Join the [Aion 2 Helper Discord](https://discord.gg/yf7SDZMyd8) for updates,
community discussion and feedback.

---

# Aion 2 Helper v1.13.1 Alpha — Experimental Distance Recovery

This patch improves the **optional experimental enemy-distance reader** and is
the first published update offered to v1.13.0's new in-app updater. Enemy health,
enemy target and exact distance remain **experimental**; DPS remains Beta.

## What caused the missing metres

The earlier session-key locator assumed the supported game objects stayed in a
particular allocation arena. A game restart or different allocation layout could
move them outside that arena. Repeating the same search then failed even though
the game continued sending movement. In addition, an old connection could keep
requesting its missing key and leave a stale error after another connection had
already started producing valid distances. Opening the helper late can still
miss the initial handshake; this separate startup limitation remains.

## Reader changes

- Retains a fast, bounded lookup for the previously observed layout, then can
  locate supported state objects in other committed writable private **64-KiB**
  allocations. It does not fall back to an unrestricted full-memory dump.
- Broader searches process at most **128 MiB / 750 ms per chunk**, stop at the
  first reached bound, and resume from the next region instead of repeatedly
  scanning the same prefix. The initial fast arena attempt is separately bounded
  to 64 MiB / 500 ms. Work runs on the existing optional background worker.
- While a bounded search is incomplete, a later worker tick continues it. A
  completed unsuccessful pass backs off for 30 seconds. Resolved connections do
  not keep searching for their keys.
- A candidate must match the supported state/header and permutation checks and
  **successfully validate the actual captured encrypted setup frame** before
  production accepts it. A memory object or plausible pointer alone is not proof
  that the key belongs to the active connection.
- Cached state is revalidated; process exit/restart invalidates the native handle
  and lookup state. Disabling the option still cancels work and clears keys.
- Waits for encrypted setup before searching, and skips pending connections with
  no recently observed outgoing traffic. An old connection error does not take
  precedence over an active ready session's status.
- Reuses a 64-KiB block buffer during each search instead of allocating a new
  block for every region. Keys remain in RAM only; no game memory is written,
  no code is injected and no game packets are sent.

## Updating from v1.13.0

1. Open **General → Application updates → Check for updates**.
2. Choose **Download and update**. The helper verifies the download, closes,
   installs through its separate updater and restarts. Your settings/history and
   game-tweak backups are preserved.
3. **Fully restart Aion after updating the helper**, with the helper already open.
   Keep **Enemy health → Details → Exact distance (reads game memory)** enabled
   if you want metres. This option remains **off by default** for new users.
4. Enter your character, walk and select/attack an enemy. Missing or stale
   coordinates still hide the distance; they are not replaced with a false zero.

After an updater restart, General refreshes the latest-release status in the
background so it can show the result without another startup offer. Versions
older than v1.13.0 still need a manual ZIP download to get the updater.

The first real v1.13.0 → v1.13.1 upgrade is being tested with the author and Edeln;
publication is not a claim that this two-PC end-to-end test has already passed.

## Validation and remaining limits

- Automated synthetic-memory tests cover a valid state beyond the former arena
  and beyond the first chunk, continued searches without rereading the prefix,
  rejection of another session's key, cached-state invalidation, malformed
  permutations and cancellation. No real game memory is opened by these tests.
- Native read-only validation on the test PC found a supported state in 319 ms;
  the cached check completed below the one-millisecond timer resolution.
- A separate **180-second live diagnostic using the corrected lookup** validated
  the captured session, decoded 1,497 frames and received 258 position readings.
  It calculated enemy distances during movement, ending at approximately 13.3 m.
  No raw packets, keys or decrypted bodies were saved. The existing v1.13.0 helper
  also recovered visible metres after a fresh game restart; this single run does
  not prove every reconnect/instance case is resolved by the new code.
- Combat/targeting, Settings, distance/position, updater staging/rollback and
  package checks remain part of release validation. Wider PCs and repeated
  instance transitions still need testing.

**Still experimental:** unsupported game builds/state layouts, missing initial
handshakes, missing enemy/player profiles and unknown NPC metadata can still
prevent data from appearing. Read-only memory access is optional and does not
guarantee anti-cheat compatibility or permission. The known open-world boss
party-DPS freeze and Energy Bar PvP issue remain unresolved.

---

# Aion 2 Helper v1.13.0 Alpha — Experimental Enemy Bars and Built-in Updates

This release adds two **experimental** Progress bar widgets and an integrated
updater. The application remains Alpha and the DPS Meter remains Beta.
**Enemy bars and distance do not have complete coverage.** The known limitations
below are part of this release, rather than a claim that every instance is fixed.

## Experimental enemy health

- New **Progress bar → Enemy health**: current HP, actual received maximum HP,
  percentage and name, with optional level, known creature type and metres.
- Follows a received locked target, or the primary enemy of an attack when no
  lock exists. In area attacks it retains the primary target rather than cycling
  through each secondary hit. This is observed protocol behavior, not a direct
  read of the game's unlocked targeting icon or UI target pointer.
- Hides on death or missing supported health data; does not fabricate maximum HP.
- Name/level, current/max/percentage, distance, creature type and lock status
  have position, size and color controls. Distance/type/lock text have their own
  offsets; black outlines have an enable switch, thickness and opacity.
- Locked/unlocked text is independently rendered so metres cannot suppress it.
- The observed level-45 scaling display can show the received base level in
  parentheses. Missing level/type metadata is left unknown.

## Experimental enemy target

- New **Progress bar → Enemy target**: a simple blue health bar and adjustable
  name for the player the tracked enemy is targeting, when focus and actual
  character-health data are available. Nearby players are supported too.
- Separate optional bar color and name color when the enemy targets **you**.
- Independent name positioning, text outline, opacity, geometry and appearance.
- Each enemy bar has its own **show only with a locked enemy** setting,
  **off by default**; normal unlocked attacks remain supported.

## Shared progress-bar and visibility improvements

- Whole-widget scale on each of the four bars: Energy, Stagger, Enemy health
  and Enemy target. Bar geometry, labels, offsets and outline sizes scale together;
  original per-widget preferences are retained until changed.
- Sharper enemy text rendered at its final pixel size with cached rasterization;
  reusable drawing buffers avoid adding a capture process or per-text timers.
- The character-identification recovery notice is positioned above the DPS widget
  so it does not cover the combat rows. Other existing visibility behavior remains.

## Built-in application updates

- On normal startup, checks the **latest published stable GitHub release** and
  offers **Download and update** or **Not now / Open helper** when newer.
- **General → Application updates** shows installed/latest versions, the real
  check status, a manual **Check for updates** button and an update button when
  available. Startup checking can be disabled independently.
- A download/progress window supports cancellation. No connection or a GitHub
  rate limit does not prevent opening the installed helper and is not reported
  as "up to date".
- Downloads the exact official Windows asset over HTTPS, verifies its SHA-256,
  checks ZIP paths and an explicit program-file allowlist, and validates the
  executable's product/version before replacing files.
- A separate updater waits for the helper to exit, backs up existing program
  files, replaces/verifies them and checks the updated executable before restart.
  Copy/startup-check failures attempt to restore the previous program files;
  restoration failures identify the retained backup folder.
- Overlay preferences, combat history, game-tweak settings, update preferences
  and original game-tweak backups are **not replaced by package defaults**.
  The updater does not apply any game tweak or modify game files.
- GitHub requests are for release metadata/downloads. No combat data, character
  information, settings or session keys are uploaded. Staged program backups stay
  locally under **LocalAppData/Aion2Helper/Updates**.
- Users on v1.12.1 or older must download this version manually **once**. Future
  published stable releases can then be offered inside the helper. The first
  real upgrade from v1.13.0 will be validated with the next published version;
  local installation, rollback and official-download tests have passed.

## Fixes and known experimental limitations

- **Pesadilla:** the reported Sangre venenosa de Fafnir used an NPC identifier
  outside the previous reader range. The bounded reader now accepts that NPC
  namespace, with actual received HP/max validation unchanged. Its Spanish name
  was confirmed in game; the English name is our translation. This fixes the
  captured case, not every untested NPC or instance layout.
- **Missing initial data:** opening/restarting the helper after character entry
  can miss enemy spawns or character-health profiles. Start the helper before
  the game. Re-entering the character/zone can provide fresh profiles; some bars
  may otherwise remain hidden until the required data arrives.
- **Metres are experimental and may be absent**, including after reconnects or
  instance changes. The session-key lookup limitation has been reproduced and
  further correction is **pending for a following release**. This release does
  not claim to resolve it. Missing/stale positions hide the distance rather than
  displaying a false zero or an invented value.
- Exact distance is an **optional, default-off read-only game-memory feature**
  that obtains the session key to decode movement. Enable it before launching
  Aion; if already connected, fully restart Aion. Administrator access may be
  required. Keys/decrypted bodies are never persisted. This changes the access
  method; anti-cheat compatibility or permission is not guaranteed.
- Unknown names/types/levels, target changes during special mechanics and wider
  instance coverage still need live validation. Enemy-target health may be
  unavailable if that player's profile was not received. No HP is synthesized.
- Existing **open-world boss party-DPS freezes** and **Energy Bar PvP** remain
  unresolved. DPS totals/rankings can still be partial.

Validation includes supported targeting/death/HP and Pesadilla replay cases,
malformed inputs, target-of-target changes, text outlines/scaling, independent
preferences and undo, existing Settings/combat/widget regressions, verified
official GitHub download/staging, protected user files and rollback fixtures.
Live updater-to-next-release and wider enemy/distance validation remain pending.

---

# Aion 2 Helper v1.12.1 Alpha — Intro Status Recovery and Diagnostics

This hotfix addresses the reported intro backup/status warning and adds the
details needed to investigate it if it returns. In the local retest, the helper
correctly recognized the active intro skip and its verified original backup;
**the installed video and original backup remained unchanged**. The precise
cause of the previous live-menu rejection was not captured, so this is not a
claim that every possible intro warning has been resolved.

- Specific errors now distinguish unreadable JSON/records, unsupported formats,
  a different replacement video, a different game folder and an invalid original
  hash instead of grouping them into one generic warning.
- Added **Copy intro diagnostics** under Game Tweaks → Startup. Reports include
  the helper version, selected/backup paths, record state and error details.
- Diagnostic output is local in **intro-status.txt**; nothing is uploaded
  automatically. Reports contain local paths/user information, so review them
  before posting publicly. Diagnostics are excluded from Git and the release ZIP.
- Refreshing status rebuilds the report and clears previous diagnostic detail.
  Reading status never applies the intro tweak or changes the backup/video.
- Removed the small Startup caption and introductory paragraph below the title.
  The controls, backup guidance and separate Visuals tab remain available.
- Strict record/hash checks, verified backups and game-closed apply/restore
  requirements remain in place; invalid metadata does not enable restoration.

Validated with intro/video preservation, specific record-error cases, successful
refresh after restoring valid metadata, restoration/update/migration fixtures,
visual-tweak regressions and general Settings checks. The user confirmed that
the intro is now recognized correctly in the locally updated helper.

The application remains Alpha, the DPS Meter remains Beta, and existing combat,
Energy PvP and visual-effect validation limits are unchanged.

---

# Aion 2 Helper v1.12.0 Alpha — Combat UI, Adaptive Notifications and Visual Tweaks

The application remains Alpha and the DPS Meter remains **Beta**. This update
improves the interface and adds optional visual settings; it does not claim new
combat-event coverage or fixes for the known open-world boss party-damage issue.

## Combat and History

- Reorganized DPS settings into **Combat / History / Configuration**. The separate
  Skills tab is now integrated into Combat and selected history entries.
- Added role-colored **average DPS/HPS comparison charts** with selectable players,
  summary statistics and a compact skill breakdown. These are fight averages,
  not instantaneous time-series graphs.
- Switch between damage, healing done and healing received, select a player and
  inspect contribution, supported critical statistics, highest hits and more.
  Additional statistics expand when needed instead of overwhelming the table.
- Removed skill and target IDs from the visible tables. Saved details and TXT
  exports retain their identifiers; skills combine across targets without merging
  different skill IDs that happen to share a name.
- Improved responsive tables and scrolling throughout Settings. Mouse-wheel
  input works over the menu content, including nested areas and scroll boundaries.
- Retained fight deletion, player deletion, TXT export and history retention.
  Older records remain readable without invented missing skill statistics.

## Adaptive notifications

- Notification backgrounds fit each message. Longer messages and names wrap;
  shorter messages use smaller backgrounds.
- Retained independent notification switches, position, colors, opacity, fades,
  timing and sound. **Maximum width** controls wrapping and **Notification scale**
  controls text and padding. Existing saved reference geometry is preserved.
- Replaced the two Shugo test buttons with one generic **Test notification**.
  The unlocked positioning sample remains silent.

## Game Tweaks → Visuals

- Independent **Depth of Field, Chromatic Aberration, Film Grain and Motion Blur**
  switches. An unmodified INI defaults to all four enabled. Existing zero overrides
  are reflected in the menu rather than silently reset.
- Finds Engine.ini beneath **LocalAppData/AION2** for Steam and other launchers;
  select the correct profile if several exist, or browse to the file manually.
- **Apply visual changes** edits only the four supported renderer keys. Disabling
  writes zero; enabling removes the disable override and uses game settings rather
  than forcing an arbitrary quality level. Unrelated paths, DLSS/FSR settings,
  sections, comments, encoding and line endings are preserved.
- Verified backups and **Restore previous visual change** undo the last change.
  Unrelated later edits are retained; conflicting visual edits or damaged backups
  are rejected. Engine.ini remains writable; the helper does not set read-only.
- **Changes with the game open are not guaranteed to take effect. Close Aion 2
  before applying, then relaunch to check them.** Writes are blocked while Aion
  runs. The helper does not inject console commands or repeatedly rewrite the INI.
- Existing Startup intro skipping remains available in its own tab.

## Validation and known limitations

Local checks cover combat summaries/history, responsive layouts, scroll routing,
notification fitting/fades/resources, INI editing/restore/discovery/conflicts,
settings persistence, existing intro handling and FPS/Ping/progress regressions.
Visual configuration writing and its backup were checked locally. **Actual visual
effects still need testing after a game restart** and may depend on the game's
supported Unreal variables and graphics settings. No performance claim for every
PC is made.

The DPS Meter is still Beta and incomplete. Live group/healing validation is
ongoing; **party damage may stop updating at open-world bosses**, and **Energy Bar
in PvP** remains pending. Effective healing/overhealing, causal buff bonuses,
per-player stagger and unsupported combat events are not claimed as resolved.

---

# Aion 2 Helper v1.11.0 Alpha — DPS Meter Beta, Visibility and Stagger Animation

The **DPS Meter is now in Beta**. We have made substantial progress on skill
identification, combat coverage and per-skill breakdowns, and are continuing
live tests. Beta does **not** mean that every damage or healing event is captured.
The application as a whole remains alpha software.

## DPS Meter Beta

- Added an embedded catalogue of **16,899 skill definitions**, loaded before
  combat, with English names and Spanish labels where available. No separate
  catalogue download is needed. Unknown IDs remain visible as numeric fallbacks;
  missing names do not discard otherwise validated impacts.
- Separated character skill/level information from global skill definitions.
  Supported login skill-list packets populate the character list before combat;
  names and attribution do not depend on the last skill cast.
- Added **Settings → DPS Meter → Skills** for live player/skill/target breakdowns:
  damage, periodic damage, healing done/received, impacts, ticks, highest hits
  and supported critical statistics. Completed histories retain immutable
  skill/target details, show them for the selected player and include a skill
  breakdown in TXT exports. Older records remain readable without invented detail.
- Expanded supported damage-over-time and healing-over-time families using
  validated event shapes and catalogue metadata. Periodic ticks keep their own
  announced skill/source rather than being assigned to the last cast.
- Added bounded recovery for valid impacts whose player identity arrives shortly
  afterwards. Small capture/UI clock differences no longer discard pending hits.
  Unconfirmed ownership or unknown periodic effects are not guessed into totals.
- Retained existing filters, widget tabs, role colors, critical columns, positional
  hit counters, highest hits and history controls. Skill detail storage is bounded;
  player totals continue if the detail cap is reached.
- Changed the combat widget label to **Beta** and removed the partial-damage/raw-
  healing footer from all three views. Coverage explanations remain in Settings.

## Character identification and visibility

- Locked DPS hides while the local character is unidentified. Energy hides when
  its data is unavailable, instead of leaving a stale bar or white dotted track
  on screen during loading, instance transitions or cinematics.
- Added a **separate small recovery notice** at the saved DPS position after
  **60 continuous seconds** without the required character data while the game
  is connected: change zones, dash once or log in again. It follows the selected
  language, uses the dark overlay style and passes clicks through to the game.
- The notice disappears automatically when identification recovers. It does not
  appear while the game is closed; brief transitions reset the grace period.
  Unlocked widgets remain accessible for positioning.
- Actual area changes discard old energy samples; same-map boss teleports and
  other connections do not unnecessarily reset them. New real readings restore
  the bar. Automatic maximum-energy detection and full-energy auto-hide remain.

## Stagger countdown animation

- During the observed stagger countdown, the bar now **fills visually from empty
  to full**, synchronized with the configured reference duration (**5 seconds by
  default**). The fill animates even if the countdown text is disabled.
- This is an estimated visual timer. Numeric current/max/percentage labels retain
  the actual game gauge. Actual positive updates or inactive phase updates take
  priority; the animation does not manufacture gauge data or bypass visibility.
- Repeated zero updates preserve the countdown origin. An actual full gauge takes
  over without a jump backwards. Existing phase hiding and the five-second stale-
  zero guard remain in place; the Energy Bar does not use this animation.

## Validation and remaining work

Solo live tests confirmed skill names and increasing per-skill damage, character
selection/recovery notices, automatic energy capacity and repeated zone changes.
Offline comparisons covered **415,262 captured frames** and recovered additional
supported damage/healing. Local parser, history/persistence, settings, display,
notification, FPS/Ping, energy and stagger animation checks passed. This is not a
claim of complete accuracy or a performance result for every PC.

Group and healing live tests are continuing. The reported **party damage freezing
at open-world bosses** remains under investigation, and **Energy Bar in PvP** is
still pending. Complete buff/debuff effects, effective healing/overhealing,
per-player stagger contribution, aggro and other unsupported combat data remain
outside the validated meter. Please report issues with version, module status
and the circumstances of the fight. Private captures and reports are not included.

Skill metadata/adapted parsing attribution and the required MIT notice are
included in THIRD-PARTY.md and assets/skills. Existing project licence status
is unchanged.

---

# Aion 2 Helper v1.10.0 Alpha — Progress Bars and Stagger Phase Handling

The v1.10.0 Windows ZIP includes these changes and retains the independent
FPS/Ping widgets introduced in v1.9.0. Start the helper before launching Aion 2;
Npcap remains a separate requirement for the progress bars and packet-based modules.

- Replaced the Energy Bar settings section with **Progress bar**, containing
  independent Energy bar and Stagger tabs. Energy's existing auto-hide is retained.
- Added a configurable enemy Stagger widget: enable/disable, colors, glow, track
  opacity, fade, smoothing, size and position, including direct movement/resizing
  through the shared lock. Settings save and support undo/redo and English/Spanish.
- Added optional current, maximum and percentage labels on both bars, with
  independent above/below left/center/right anchors and vertical offsets.
- Added an optional stagger countdown with independent appearance/placement,
  glow and shrinking text. Its default five seconds is an estimate; actual
  network updates determine the gauge, with no simulated refill.
- Removed full-gauge auto-hide only for Stagger. Supported inactive-phase
  updates now replace stale values and hide the bar; actual active-phase updates
  restore it. Zero left continuously for five seconds hides until eligible
  positive data returns. Editing remains accessible while unlocked.
- Fixed a freshness check that could briefly hide valid data when a capture
  timestamp was slightly ahead of the UI clock. Unrelated area hits without a
  known gauge no longer replace the tracked enemy.
- Preserved Ping preferences when editing progress settings, included Stagger
  in display adaptation and configuration history, and removed a duplicate
  translation key that could cause a settings initialization exception.
- Shared capture, bounded flow/entity storage, cached native rendering and
  adaptive timers keep this module from adding another packet-capture session.

Local Progress, Settings and native bar checks passed. A private replay of the
reported multi-phase encounter accepted its actual active/inactive updates and
replaced the retained old value. Further live phase validation remains pending.
Selection follows the last directly attacked enemy with known gauge data, not
the game's UI target pointer. Unknown phase semantics and per-player stagger
contribution remain unverified.

New periodic damage/healing candidates remain research only. DPS coverage is
partial, the open-world boss party-damage issue remains unresolved, and no PvP
energy fix is claimed. Private capture/report data is not published.

---

# Aion 2 Helper v1.9.0 Alpha — Independent FPS/Ping Widgets

- Renamed the settings section to FPS/Ping, with separate FPS and Ping tabs.
- Added an independent ping widget with the same appearance and geometry
  controls, shared lock, undo/redo and display adaptation as FPS.
- Ping reads smoothed Windows TCP RTT for the game's current server connection,
  without extra probes or Npcap. Defaults to 1000 ms refresh, configurable from
  250 to 5000 ms. Disabled by default; fresh readings require connection traffic.
- Added EN/ES status, permission/error explanations and Copy Ping diagnostics.
  If Windows denies collection access, restart the helper as administrator.
- Added bounded background sampling, connection recovery, stale-sample expiry
  and cleanup that preserves statistics enabled by other applications.
- Local live IPv4 validation received real RTT samples. This is a network RTT
  estimate, not a claim to reproduce the game's internal ping.
- Known issue recorded: Energy Bar may not receive readings during PvP. Cause
  and scope remain unconfirmed; investigation is pending.

Existing DPS coverage limitations and the open-world boss party-damage issue
remain unresolved. No Energy Bar PvP fix is claimed.

# Aion 2 Helper v1.8.1 Alpha — Adaptive Widget Layout & Auto-hide Guidance

Widgets now keep a proportional layout when your Windows display resolution or
monitor arrangement changes. This update also makes missing maximum-energy
stats visible and explains what to check when the Energy Bar does not auto-hide.

**Recommended startup order: always open Aion 2 Helper before launching Aion 2.**
This helps the helper receive your character's initial maximum-stamina stat when
you enter the world. Opening it after character entry can miss that stat and leave
a saved/default reference that may affect auto-hide. If this happens, return to
character selection and enter again with the helper running. Maximum-stamina
changes during play are still updated automatically when the game sends them.

## What changed

- **Automatic layout adaptation for all four widgets:** Energy Bar, FPS Counter,
  DPS Meter and Notifications scale their position and size relative to the
  display they were placed on. Matching monitors retain their widgets when
  rearranged; if a monitor is removed, its widgets move to the primary display.
  Widget limits and the taskbar working area keep controls accessible.
- The last user-edited layout and its display reference are saved separately.
  Returning to the original display restores that reference without repeatedly
  rounding the layout. Undo/redo entries are adapted to the current displays.
  This works while the helper is open and across restarts. Older settings have
  no display reference, so inaccessible widgets are recovered safely; their old
  resolution is not guessed. Future changes use the new saved reference.
- Layout work runs on Windows display-change events and settings changes,
  not every frame. Existing FPS measurement, glow, fades and smoothing are retained.
- **Settings → FPS Counter → Geometry → Reset FPS position** moves the counter
  to a visible position without resetting its colors or measurement interval.
  FPS diagnostics now include widget visibility and bounds. A counter that was
  receiving FPS could previously remain outside the screen after a monitor swap.
- **Energy Bar → Behavior** explains auto-hide and missing maximum-energy stats
  in English and Spanish. A waiting-for-maximum message now appears even when
  the saved reference displays 100% or less, instead of only above 100%.

## If your Energy Bar does not auto-hide

1. Enable **Auto-hide** in **Settings → Energy Bar → Behavior**.
2. **Lock** the widgets from the tray menu. Unlocked widgets intentionally stay
   visible so they can be moved and resized.
3. Wait until energy is actually full, then allow the configured **Delay at 100%**
   and fade duration to finish.
4. If the status says **waiting for maximum energy stats**, the helper has not
   received your character's current capacity. Start the helper before entering
   your character, or return to character selection and enter again with the
   helper running. Until that stat arrives, a saved/default reference can be wrong,
   particularly after changing characters or energy capacity. A dash starts current
   energy readings but does not guarantee the missing maximum will be resent.

This improves diagnosis and explains an existing detection limit; it does not
invent maximum energy from partial readings or promise that every auto-hide report
has the same cause. If it persists, report the version, status and lock state.

## Validation and limits

Local checks covered proportional geometry for all widgets, negative monitor
coordinates, monitor rearrangement/removal, original-layout restoration,
reference persistence through actual startup/shutdown, invalid reference fallback,
and undo/redo on a changed display. Settings, energy rendering/auto-hide, protocol,
FPS, DPS and Notifications regression checks passed. The FPS visibility report on
the development PC was traced to an off-screen saved X coordinate while live FPS
readings were arriving. Other PCs' measurement failures still need their diagnostics.

**This remains an alpha. The DPS Meter is incomplete, and party-member damage
may stop updating during open-world bosses. That issue remains unresolved.**
The reversible own intro skip and automatic game-folder detection remain included.

## Updating

Close the helper and extract **Aion-2-Helper-v1.8.1-windows-x64.zip** with all its
subfolders into the same helper folder. Keep **overlay-settings.json**,
**combat-history.json** and **game-tweaks.json**. Personal settings, display layouts,
backups, logs and captures are excluded from release packages. Npcap remains a
separate requirement for energy, combat and incoming party notifications.

---

# Aion 2 Helper v1.8.0 Alpha — Game Tweaks & Automatic Game Detection

**Settings → Game Tweaks → Startup** adds an optional, reversible way to skip
the NCSOFT intro. It uses our own independently generated one-frame black video,
embedded in the helper. No ZIP, scripts or video assets from another mod are
needed, and no encoder needs to be installed by users.

## What changed

- **Automatic game-folder detection:** when opening Game Tweaks without a valid
  saved folder, the helper checks the running game, Windows installation records,
  Steam's libraries and manifests (including additional drives), and common
  NCSOFT installation locations. It validates the actual game layout before
  using a candidate. If no valid installation is found, choose **Browse folder**;
  **Detect game folder** retries the search. A valid saved folder is reused.
- Discovery runs in a background task only when needed or explicitly requested.
  It does not scan entire drives, continuously poll game files, or add work to
  the overlay's rendering loop. Metadata reads and Steam manifest scans are bounded.
- **Skip intro** is off by default. With Aion closed, enable it and click
  **Apply change**. The helper verifies and saves a copy of the original intro,
  then atomically installs its own short video while keeping the expected
  filename present. This changes the startup video, not game memory or gameplay.
- **Restore original** restores the exact verified original. Backups are retained
  outside the game folder and versioned if a game update changes the video.
  Unexpected or changed files are preserved instead of overwritten by restoration.
- **Refresh status** only checks the installed video and backup; it does not
  apply or restore anything. Game updates can restore the intro, so check the
  status before applying again. The tweak remains applied after the helper closes.
- English and Spanish menus explain automatic detection, manual fallback,
  application, restoration and status checks. Settings undo/redo does not alter
  game files. Earlier development rename installations can be restored.

## Validation and current limits

Local tests covered Steam library formats, paths with spaces, stale and missing
folders, manual fallback, invalid metadata, backup verification, exact restore,
updates and concurrent file changes. FPS, Notifications, Settings and DPS
regression checks passed. A real Aion launch confirmed intro skipping and normal
mouse input on the development PC. This is a startup convenience, not an FPS boost.

**This remains an alpha. The DPS Meter is incomplete, and party-member damage
may stop updating during open-world bosses. That known issue is unresolved.**
FPS diagnostics and recovery from v1.7.1 remain included; a failure on another PC
still requires that PC's diagnostic report rather than assuming its cause.

## Updating and using the feature

Close the helper and extract **Aion-2-Helper-v1.8.0-windows-x64.zip**, including
all subfolders, into your helper folder. Keep **overlay-settings.json**,
**combat-history.json** and **game-tweaks.json** to retain preferences, history
and the selected installation. Personal files, backups and raw captures are
not included in the download. Npcap remains separate; it is not needed for FPS
or Game Tweaks.

Open **Settings → Game Tweaks**, let detection finish, close Aion before applying
or restoring, and click the appropriate action. File changes need write access
to the game folder; Windows may require running the helper as administrator.
Report problems through [GitHub Issues](https://github.com/Carchutin19/Aion-2-Helper/issues).

---

# Aion 2 Helper v1.7.1 Alpha — FPS Diagnostics & Recovery

The FPS Counter now explains why readings are unavailable when a specific error
is detected, and provides a diagnostic report for failures on other PCs.
**Accepting an administrator prompt does not guarantee that game FPS events arrive.**

## Changes

- **Settings → FPS Counter**, below the preview, shows localized explanations
  for denied permissions, a missing PresentMon file, Windows/native trace or
  process-start errors, a timed-out fallback trial, unexpected PresentMon exits
  and measurement-reader connection failures. If no samples arrive and the cause
  is unknown, the message says so rather than assuming administrator rights fix it.
- **Measurement → Copy FPS diagnostics** copies app/Windows/process information,
  helper and worker permission information, measurement state, dependency presence,
  connection state and available error/exit codes. It excludes IPC authentication
  tokens/session identifiers, packet captures and personal configuration/history.
- The administrator action hides when the helper is already elevated or an
  explicitly elevated reader is running; it no longer remains solely because
  the counter still has no reading. UAC cancellation is distinguished from a
  reader launch failure.
- A running game whose main-window handle is unavailable can now reach the
  bounded PresentMon fallback after warmup instead of waiting indefinitely.
- Unexpected reader loss releases its pipe/process references and retries with
  a bounded delay. Elevation is only requested by the user, never by automatic retries.
- Native startup failures retain their Windows error codes. Diagnostics retain
  the last collector details when the reader disconnects.
- No FPS logs are written continuously; detail messages are sent only when
  changed. Reader trials, retry delays and resource use remain bounded.
- **Settings → General** shows this update in English and Spanish. Existing
  preferences, the FPS font/rendering and all other widget visuals are retained.

## Validation and limitations

FPS classification, permission-button behavior, English/Spanish explanations,
render/resource caching, settings persistence and undo/redo passed local checks.
Standby enable/disable and forced reader-loss recovery were tested without a
running game; Settings, Notifications and DPS regression checks also passed.
**This does not yet confirm that Edeln's FPS issue is fixed.** The new report is
intended to identify his PC's actual failure; live confirmation there is pending.

**Alpha software. DPS remains incomplete, and party-member damage may still stop
updating during open-world bosses. That known issue is unresolved.**

## Updating and reporting an FPS problem

Close the helper and extract **Aion-2-Helper-v1.7.1-windows-x64.zip**, including all
subfolders, into the installation folder. Keep **overlay-settings.json** and
**combat-history.json** to retain preferences and history. Npcap is separate and
is not required for FPS measurement.

Enable FPS, keep Aion in the foreground, then open **Settings → FPS Counter**.
If it still shows **— FPS**, read the visible explanation and choose
**Measurement → Copy FPS diagnostics**. Paste the report into
[GitHub Issues](https://github.com/Carchutin19/Aion-2-Helper/issues).

---

# Aion 2 Helper v1.7.0 Alpha — Dungeon Party Notifications

Notifications now recognize invitations to a dungeon group, including the new
invitation sent when a normal party is converted into a dungeon group.

## Changes

- Dungeon invitations show the inviting player's name and a distinct message:
  **“{Player} is inviting you to a dungeon party.”** English and Spanish are supported.
- **Settings → Notifications → Notification Types** has separate switches for
  normal party invitations and dungeon-party invitations. Each can be enabled or
  disabled independently; dungeon invitations start enabled.
- Both invitation types use the shared notification sound, volume, duration,
  fade, appearance and position. Shugo's advance and opening notices remain independent.
- Turning off one invitation category clears only its queued/active notices.
  The other category keeps working, including when it is the only module that
  needs game-traffic capture.
- Normal and dungeon requests are deduplicated separately, so the dungeon
  re-invitation is not suppressed by an earlier normal invitation.
- Preferences save automatically and participate in undo/redo. Turning all
  notifications off preserves individual choices. The preview remains generic.
- **Settings → General** shows the update in the selected language.

## Validation and limitations

The new dungeon invitation was recorded, replayed through the production decoder
and confirmed in game with the sender name and immediate sound. Automated checks
cover malformed/truncated frames, UTF8 names, independent switches, duplicate
suppression, queued/active notices, persistence, undo, rendering and capture demand.
Notification, Settings and DPS checks passed. Supported formats are conservative;
future game updates or other invitation variants may need additional validation.

**This remains alpha software. The DPS Meter is incomplete, and the known issue
where party members' damage can stop updating during open-world bosses remains
unresolved.** Existing energy, FPS and combat features are retained.

## Updating

Close the helper and extract **Aion-2-Helper-v1.7.0-windows-x64.zip** into your
installation folder. Keep **overlay-settings.json** and **combat-history.json**
to preserve preferences and records. Personal data and raw research captures are
excluded from the download. Npcap is installed separately; run as administrator
if capture or FPS measurement permissions require it.

Report issues through [GitHub Issues](https://github.com/Carchutin19/Aion-2-Helper/issues).

---

# Aion 2 Helper v1.6.1 Alpha — Weapon Poison Damage

The DPS Meter now counts the weapon-poison effect validated in our dungeon test.
**DPS coverage remains partial and experimental. The known issue where party
members' damage can stop updating during open-world bosses is still unresolved.**

## Changes

- Supported weapon-poison ticks contribute to total damage, DPS and periodic
  damage, with existing highest-hit, per-player history and TXT export support.
- Values are read from the game's announced impacts; damage and tick frequency
  are not hardcoded. No new setting is needed when the DPS module is enabled.
- Only the exact validated periodic format is accepted. Identified-source and
  known-NPC-target checks remain required; unknown weapon effects and variants
  are excluded until validated.
- Paralysis is a status effect and is excluded from damage. This update does
  not add a general debuff tracker or infer damage from status duration.
- Periodic poison does not invent critical or Front/Back metadata and stays
  outside the primary direct-impact critical percentages.
- Settings → General shows the update and the unresolved world-boss warning
  in English or Spanish. Existing records are retained and are not recalculated.

## Validation

The recorded-session production replay added exactly **73 poison impacts** of
**300**, totaling **21,900 damage** for the affected player. Other player totals,
primary critical/tag counters and healing remained unchanged. These numbers are
test observations, not fixed gameplay values or a complete-damage guarantee.

Checks cover the captured format, truncation, unknown variants, paralysis,
amount-less/wrong flags, invalid amounts, source/target exclusions and history
snapshots. Other damage-over-time and weapon-proc families remain under study.
Successful dungeon tests do not resolve the open-world boss issue.

## Updating

Close the helper and extract **Aion-2-Helper-v1.6.1-windows-x64.zip** into your
installation folder. Keep **overlay-settings.json** and **combat-history.json**
to preserve preferences and records. Personal data and raw research captures
are excluded from the package. Npcap is installed separately; run as administrator
if capture or FPS measurement permissions require it.

Report issues through [GitHub Issues](https://github.com/Carchutin19/Aion-2-Helper/issues).

---

# Aion 2 Helper v1.6.0 Alpha — DPS Hit Details & History Controls

**The DPS Meter is still incomplete and experimental. We have identified an
unresolved issue where party members' damage can stop updating during open-world
bosses, even while they continue attacking nearby. Totals and rankings may be
incomplete. This release does not claim to fix that issue.**

## New per-player history details

- Front, Back, Double and Perfect tags for supported primary direct impacts.
- Front/Back Critical, Front/Back Double Critical and Front/Back Perfect counts.
  Counts overlap when the same impact has several tags; do not add them as hits.
- Highest hit for each player: largest primary direct damage impact or supported
  individual periodic tick, excluding added extra-impact amounts.
- Tagged direct impacts indicates how many events supplied recognized metadata.
  Missing metadata and older records show unavailable values instead of invented
  zeroes. New fields are also included in TXT exports.

## History controls

- Minimum fight duration defaults to **60 seconds**. Set **0** to save every
  encounter. It measures first-to-last accepted impact and excludes the inactivity
  wait. Short fights still appear in the live meter; existing history is retained.
- Each fight row has a fixed **Delete fight** button scoped to that exact fight,
  including when a player filter is active.
- **Delete player** remains separate and removes only that player in that fight.
  Removing its last player removes the empty fight. Other fights are preserved.
- English/Spanish settings, automatic saving and preference undo/redo remain
  supported. Deleting history cannot be undone.

## Reader improvement and in-app update notice

After opening the helper while an enemy/dummy is already visible, supported
direction-tagged direct hits can recover if fresh target HP drops corroborate
them. The reader records the announced damage amount, without applying an HP
multiplier or guessing boss identity. Untagged or unsupported events may still
be missing; this does not establish complete damage coverage.

**Settings → General** now shows these changes and the known open-world boss
issue in the selected language. Energy glow, fades and FPS presentation remain
unchanged.

## Updating

Close the helper and extract **Aion-2-Helper-v1.6.0-windows-x64.zip** into your
installation folder. Keep **overlay-settings.json** and **combat-history.json**
to preserve preferences and records. Personal data and research captures are
excluded from the package. Npcap is installed separately; run as administrator
if capture or FPS permissions require it.

Regression checks cover hit tags/combinations, largest hits, legacy records,
duration boundaries, persistence and scoped deletion. Production replay retained
previously accepted party totals in the tested dungeon corpus, and the dummy
recovery was confirmed in game. These checks do not resolve the world-boss issue.

Report issues through [GitHub Issues](https://github.com/Carchutin19/Aion-2-Helper/issues).

---

# Aion 2 Helper v1.5.0 Alpha — Shugo Festival Alerts & Automatic Energy Capacity

This update adds two Shugo Festival reminders and makes maximum energy automatic.
The DPS Meter also accepts confirmed party members before their names arrive.
**The application remains alpha software, and the DPS Meter is incomplete.**

## Shugo Festival notifications

- **Five minutes before each hour (:55):** "Shugo Festival starts in 5 minutes."
- **On the hour (:00):** "You can now sign up for Shugo Festival."
- Separate switches for the advance reminder, registration reminder and party
  invitations. Disable any one without disabling the others.
- Shared optional sound, volume, text color/size, dark background, position,
  duration and fade settings. Each Shugo reminder has its own test button.
- English and Spanish messages follow the selected application language.
- Scheduled reminders run while Aion 2 is open. They follow the hourly clock
  schedule; they do not query the server or confirm that an event is available.
  Missed alerts are not replayed after starting the helper.

The timer checks at minute boundaries, checks the game process only when an
alert is due, and stops when both Shugo reminders or all notifications are off.
Clock reminders do not need Npcap; party invitation detection still does.

## Energy Bar improvement

Previously, increasing energy capacity could leave the saved maximum too low:
Settings could report over 100%, and the bar could remain visible when full.
The helper now reads the maximum sent by the game independently of current
energy and automatically applies capacity changes, including decreases.

**Manual calibration controls have been removed.** The Energy Bar tabs are
**Behavior**, **Appearance**, and **Position & size**. Emissive glow, colors,
smoothing, fade transitions and the configurable full-energy hiding delay stay
available. Automatic capacity updates do not create undo/redo entries.

The last detected capacity is saved for the next launch. Start the helper before
entering with your character to receive initial stats. If you open it after
entering, re-enter with your character to receive a fresh maximum. Until the
server maximum arrives, the saved reference is provisional, especially when
switching characters; partial energy is never taken as an inferred maximum.

Validated in game: a server maximum of 115700 produced 100% at full energy and
the expected automatic hide, without Settings calibration.

## DPS party recovery

When party vital updates arrived before character names, early damage/healing
from those members could be skipped and the widget could initially show only
yourself. Confirmed party members can now contribute before their name arrives.
A temporary "Player <ID>" label changes to the received name on the same row,
without losing or duplicating accepted totals. English/Spanish labels are supported.
Membership remains scoped to the current connection and expires or clears on
leave/reset; an unconfirmed nearby actor is not inferred to be a party member.

**DPS coverage is still partial and experimental.** Unsupported combat events
and data missed before identification can still be absent. This release does
not claim complete boss, stagger, aggro, buff/debuff or healing coverage.

## Updating and validation

Download **Aion-2-Helper-v1.5.0-windows-x64.zip**, close the helper, and extract all
files into the existing installation folder. Keep **overlay-settings.json** and
**combat-history.json** to preserve settings and fight history. These personal
files and raw research captures are not included in the ZIP.

Npcap is installed separately for energy, combat and party invitation capture.
If FPS measurement needs permission, run the helper as administrator or use
**Start measurement as administrator** in FPS settings.

Protocol, settings, rendering/resource, notifications and DPS regression checks
passed, including maximum-only stat updates, partial energy, membership before
names, late identity updates and membership expiry. Existing graphics and
notification fade/sound behavior are preserved.

Please report problems through [GitHub Issues](https://github.com/Carchutin19/Aion-2-Helper/issues).
Optional donations are welcome through [Nexus Mods](https://www.nexusmods.com/aion2/mods/9)
— coffee is still our favorite buff! ☕

---

# Aion 2 Helper v1.4.0 Alpha — Party Invitation Notifications

**Notifications are now active! For this release, party invitations are the
only supported notification type.** More useful alerts are planned.

An incoming party invitation now displays the inviting player's name in a
movable dark overlay, with optional sound and smooth fade in/out. You do not
need to accept the invitation or dash to identify the inviting player.

## Notifications settings

- **Enable all notifications** turns the entire module on/off while preserving
  your individual notification choices.
- **Notification types → Party invitations** independently enables/disables
  this alert. Disabling it also hides an active invitation and stops its sound.
- **Common settings** control optional sound, volume, display duration, fade
  enable/disable and fade duration.
- Text color/size, dark background opacity, position, width and height are shared
  across all notification types, ready for the additional types planned later.
- Move and resize directly using the existing shared **Unlock / Lock** control,
  or enter geometry in Settings. Locked notices do not intercept game clicks.
- Generic sample preview: **This is a sample notification.** Use **Test
  notification** to check the appearance and configured sound. The editing
  sample shown while unlocked stays silent.
- English/Spanish, automatic saving and configuration undo/redo.

Party invitations and sound are **enabled by default** when there are no prior
notification preferences. Defaults: **6-second display**, **250 ms fades** and
**80% volume**. The notification MP3 is included in the ZIP.

The reader triggers on an incoming invitation and deduplicates repeated copies.
Accepting or joining a party does not generate a separate invitation notice.
New invitations from the same player can appear again. The notification window
works independently of the Energy Bar's visibility.

## Performance and visual quality

- Idle notices avoid queue locks, clock/shell lookups and duplicate work from
  the shared energy timer. Their animation timer stops when no notice is active.
- Fades reuse the native bitmap; moves, volume and duration changes do not
  regenerate text. A single timer deadline handles the stable display period.
- Settings reuses the sample image and rendering workspace, and skips rebuilding
  unchanged controls. Preview resources are released when Settings closes.
- Muting/disabling notifications releases the sound decoder; it opens on demand.
- All **24** compared notification images remain pixel-identical across tested
  sizes, languages, sample/real text and editing states.

Isolated benchmarks and a short whole-helper CPU/RAM observation are documented
in **performance/README.md**. These are development measurements, not a game FPS
improvement claim or a guarantee for every PC.

## Scope and existing alpha limitations

Incoming party invitations were confirmed on Aion 2 Global, including accepted
and declined cases, with live visual/sound confirmation. Replaying two private
fixtures produced exactly **5 invitations from 253,679 decoded frames** and no
notices from the other frames. Other invitation variants, alliance requests,
event reminders and other notification types are not included yet.

**The DPS Meter remains incomplete and experimental.** It still reads partial
combat data; unsupported events can be missing from damage/healing totals.
We are actively working on broader coverage and validation. This release does
not add boss/stagger/aggro tracking, complete buff/debuff coverage or other
previously pending combat research. Energy Bar, FPS Counter, combat history and
your existing preferences remain available.

## Installation and updating

Download **Aion-2-Helper-v1.4.0-windows-x64.zip** and extract every file together,
including **assets/sounds** and **tools/presentmon**. Run **Aion2Helper.exe**.

**Npcap is required for Energy Bar, DPS Meter and Notifications** and is installed
separately. General Settings links to its official download and reports missing
capture support. **FPS Counter works without Npcap.** If FPS readings need
permission, use **Start measurement as administrator** or run the helper as
administrator; elevation does not replace Npcap.

To update, close the helper and replace its program files in the existing folder.
Keep **overlay-settings.json** and **combat-history.json** to retain preferences
and saved fights. These personal files are not included in the release ZIP.

Automated notification, settings, rendering/resource and DPS regression checks
passed. The application remains **alpha software**; report problems through
[GitHub Issues](https://github.com/Carchutin19/Aion-2-Helper/issues).

Donations are optional and welcome through [our Nexus page](https://www.nexusmods.com/aion2/mods/9)
— coffee is still our favorite buff! ☕

---

# Aion 2 Helper v1.3.0 Alpha — Experimental DPS Meter & Combat History

**The DPS Meter is not complete. It currently reads partial combat data, and
unsupported events can be missing from damage and healing totals. We are
actively working on identifying more data, validating it in game and improving
reliability. Please treat this as an experimental preview.**

This release adds an independently configurable combat widget alongside Energy
Bar and FPS Counter, plus persistent records with statistics for each player.

## Combat widget

- Direct damage and DPS, supported damage over time, healing done/received and
  HPS. Switch views using **Damage / Heals / Received** tabs directly on the
  widget, including while it is locked.
- Choose **Self**, **Party** or **Nearby players**. Nearby includes only players
  whose supported combat information reaches your client, not the whole server.
- Recover a party that was already formed when the helper starts and process
  roster changes. Recovery can take time as identity and party data arrive.
  If the widget asks for identification, dash once with your character.
- Choose separate colors for damage, healer/support and tank roles, or use one
  color. Configure background opacity, refresh interval, displayed row count,
  encounter inactivity timeout, auto-hide delay, fade and bar smoothing.
- Move and resize using Settings or the unlocked widget. Taskbar/tray handling
  keeps Windows controls accessible in the tested configuration.
- Optionally show your name as **You / Tú** based on the selected app language,
  and optionally keep yourself first regardless of ranking. Both are off by
  default. Recorded player names retain their actual identity.
- Shared lock, automatic saving, undo/redo and English/Spanish support.

DPS Meter is **off by default** for new installations and older preferences
without a DPS section. Enable it under **Settings → DPS Meter → Configuration**.
Base refresh is **500 ms**; the default encounter timeout is **10 seconds**.
Auto-hide starts off. Existing Energy Bar visuals and FPS preferences are retained.

## Critical statistics: what is measured

- **Critical amount share:** the portion of eligible primary direct damage or
  healing attributed to critical results.
- **Critical impact frequency:** the percentage of eligible primary direct
  events that were critical. This is an observed frequency, **not your character's
  theoretical critical chance**.

Additional impacts and periodic ticks are excluded from those denominators.
An em dash means there are no eligible events. Positive critical-healing results
still need live validation; the shared result mapping has synthetic test coverage.

## Persistent combat history, with one row per player

The new **Settings → DPS Meter → History** tab keeps completed encounters in a
styled table. Each fight retains every identified player's accepted statistics,
independently of the widget filter, current view or displayed row limit.

- Inspect damage/DPS, supported periodic damage, critical amounts and frequencies,
  healing/HPS, healing received and event counts in a horizontally scrolling
  player table. Choose a player-name filter or view everyone together.
- Export the selected fight as **TXT** to any location chosen in the save dialog.
  The export follows the player filter and describes the metric limitations.
- Delete a selected fight or use the per-row delete button; clear all history.
- Set a maximum of **1–1000 fights** (**100** by default). New records evict the
  oldest when the limit is reached.
- Optionally remove old records after a chosen number of hours or days.
  Automatic age cleanup is off by default; the initial age value is seven days.
- History stays available for viewing/export when DPS Meter is disabled and
  survives helper restarts in local **combat-history.json**.

The configured inactivity timeout ends a fight after a gap without accepted
combat impacts. Resetting, disabling the meter, connection/area changes or
closing the helper also save an unfinished fight with its end reason. A dungeon
or boss is not guaranteed to equal one record; pauses can split encounters.
Supported same-map boss transitions retain the current encounter.

Deleting records or applying retention limits is irreversible; settings undo
does not restore erased history. Records and exports contain player names and
IDs. Normal use does not record raw packets or upload this information.

## What is still missing

**This release does not provide a complete or guaranteed accurate combat log.**
Only supported event layouts and identified periodic families are counted.
Unknown skills/effects and unresolved identities can leave gaps. Some DoT and
HoT are supported; this does not mean every poison, burn or periodic heal is known.

Healing is the announced restoration amount, not effective healing. Overhealing,
full buff/debuff tracking, buff-related damage/healing attribution, front/back
classifications, reliable boss identification, stagger contributions and gauges,
damage during stagger, aggro, item-versus-skill restoration separation and zone
names remain pending. These are research goals, not enabled features.

Development testing includes open-world combat and dungeon runs on the Global
client. Coverage can vary by skill, encounter, client update and system. We are
continuing to collect and validate formats rather than treating missing data as
zero. Reports describing the skill, encounter and missing behavior are welcome
through [GitHub Issues](https://github.com/Carchutin19/Aion-2-Helper/issues).

## Requirements and updating

Download **Aion-2-Helper-v1.3.0-windows-x64.zip**, extract every file together,
including **tools/presentmon**, and run **Aion2Helper.exe**.

**Npcap is required for Energy Bar and DPS Meter** and remains a separate
installation. General Settings links to the official download and reports missing
or unloadable capture support. **FPS Counter works without Npcap**. If FPS readings
need permission, use **Start measurement as administrator** or run the helper as
administrator; elevation does not replace Npcap.

To update, close the helper and replace its program files in the existing folder.
Keep **overlay-settings.json** and, when present, **combat-history.json** to retain
preferences and saved fights. Neither personal file is included in the ZIP.

## Validation and performance scope

- Automated checks cover accepted damage/healing/DoT and exclusions, critical
  denominators, party updates/startup recovery, settings persistence, undo/redo,
  smooth/fade behavior, native clickable locked tabs and rendering/resource reuse.
- History checks cover one-time completion, immutable per-player snapshots,
  retaining more rows than the live widget, persistent/atomic saves, safe corrupt
  file backup, retention, deletion and filtered localized TXT export.
- A local replay processed **231,303 frames**, retained the eleven distinct party
  players across the tested runs, reached parties of five, and produced **23
  encounter records**. This verifies those cases, not complete damage accuracy.
- History uses background writes on changes rather than writes per hit. Rendering
  caches are reused; fades/smoothing run faster only while transitioning.

Earlier published CPU/RAM observations predate the combat module and should not
be interpreted as resource measurements of v1.3.0. The app remains in alpha.

Support is optional; feedback and bug reports help the project grow. Donations
are welcome through [our Nexus page](https://www.nexusmods.com/aion2/mods/9) —
coffee remains our favorite buff! ☕

---

# Aion 2 Helper v1.2.2 — Npcap setup guidance

**Alpha release — actively in development.** Behavior may vary between PCs and
game updates. Please report problems through GitHub Issues.

This alpha release makes the separate Energy Bar requirement clearer for new
users and replaces missing-library errors with useful setup guidance.

## What's new

- Detect missing Npcap before entering the game.
- Show **Npcap is required for Energy Bar** in the energy status.
- Add a requirement card in **Settings → General** with a **Download Npcap**
  button opening the official download page in your browser.
- Show reinstall guidance if the library or its dependencies cannot load.
- Clear the warning after successful dependency validation.
- Translate the new guidance and button into English and Spanish.
- Keep FPS and General settings usable without Npcap, including when Energy Bar
  is disabled.

Npcap remains a separate installation. No installer, driver or DLL is bundled;
the button opens its official site. The installed library is validated once and
cached; file/native checks stay off the rendering timer. Energy Bar visuals,
FPS measurement and existing preferences are preserved.

## Updating

### FPS permissions

If FPS readings do not appear, running measurement as administrator is
recommended. In **Settings → FPS Counter**, click **Start measurement as
administrator** when shown and accept the Windows prompt. This elevates only the
reader. Alternatively, close the helper from its tray menu and right-click
**Aion2Helper.exe → Run as administrator** to elevate the whole helper.
Some PCs can measure FPS without elevation. The button indicates no current
reading, rather than confirming a permission error. Npcap is still a separate
Energy Bar requirement.

### Existing settings

Close Aion 2 Helper and extract all contents of
**Aion-2-Helper-v1.2.2-windows-x64.zip** into your existing folder. Keep
**overlay-settings.json** to preserve your preferences.

If Npcap is missing, open **Settings → General → Download Npcap**, install it with
the default options, restart the helper, then dash once with your character.

## Validation

- Simulated missing libraries, failed loading, architecture/entry-point errors,
  recovery and cached validation.
- EN/ES requirement-card rendering, button labels/accessibility, official URL,
  and continued access to FPS/General with Energy Bar disabled.
- Packaged Settings, FPS, Energy Bar and native capture self-tests.
- A subsequent 20-minute gameplay observation recorded 2,839 energy readings
  with zero decoder errors and valid FPS in all 1,188 observations. Mean combined
  CPU was 0.046% of total capacity on a 32-logical-processor PC; resident memory
  stayed around 275 MiB. The FPS refresh interval was 2,000 ms. See
  [performance details and limits](performance/README.md) for the configuration
  and method. This is an observation of the existing build, without a change to
  its rendering or a claim of improved game FPS.

---

# Aion 2 Helper v1.2.1 — Performance improvements

This public preview reduces recurring work in FPS measurement, rendering and
energy decoding while preserving the widgets' appearance and settings.

## What's improved

- Native FPS events are filtered to Aion's process IDs before delivery.
- Game discovery runs on the measurement worker instead of the overlay UI.
- No FPS trace or PresentMon process runs while the game is closed.
- PresentMon fallback trials stop after 15 seconds without readings and retry
  with increasing delays, rather than running indefinitely.
- Reuse FPS glyph masks, fonts and pixel buffers. Unchanged readings and moves
  reuse the existing native image.
- Avoid rebuilding FPS Settings controls and previews for unchanged values.
- Reuse bounded energy decompression buffers, including nested containers.
- Avoid scheduling capture maintenance while Energy Bar is disabled.
- Release the FPS pipe and process resources after unexpected worker exits.

The sharp regular-weight FPS text, Energy Bar glow, smoothing, fades, colors,
geometry, language, calibration and default 250 ms FPS interval are preserved.

## Validation

- FPS rendering allocated 82% less temporary managed memory for a small widget
  and 99% less for a large widget in the development benchmark.
- Energy decoding allocated 88% less temporary managed memory in 30 fixture
  replays; all 79 readings per replay passed with zero errors.
- All 48 FPS and 30 Energy Bar comparison images matched pixel for pixel.
- Real game FPS and live interval changes passed with the process-filtered reader.
- Packaged FPS, Settings, Energy Bar and protocol checks pass, including bounded
  graphics resources, nested/reused decompression, and worker shutdown.
- Two enable/disable cycles with no game passed without creating an FPS trace.

These are development measurements of temporary allocations, not equivalent
reductions in total RAM or a guarantee of higher game FPS. Details and a
standalone FPS benchmark are in **performance/README.md**.

## Updating

Close Aion 2 Helper and extract all contents of
**Aion-2-Helper-v1.2.1-windows-x64.zip** into your existing folder, replacing
program files. Keep **overlay-settings.json** to retain your preferences.
Npcap remains a separate requirement for Energy Bar.

---

# Aion 2 Helper v1.2.0 — FPS Counter

This public preview adds an independent FPS widget alongside Energy Bar.

## What's new

- Enable or disable **FPS Counter** independently in Settings.
- Sharp, lightweight text with a configurable color (green by default).
- Optional soft black background with adjustable opacity and edge softness.
- Adjust position, width, height and scale in Settings, or move/resize the
  widget directly after unlocking it from the tray menu.
- Choose a refresh interval from **100 to 2000 ms**. The default remains
  **250 ms** (four display updates per second).
- Automatic saving, shared widget lock, undo/redo and English/Spanish support.
- Native Windows DXGI presentation reader, with Intel PresentMon as a fallback.
- Measurement can begin before the game starts and continue across restarts.
- Permission button elevates the measurement worker only when needed.

FPS Counter starts disabled for new installations and existing preferences
without an FPS section. Open **Settings → FPS Counter → Enable FPS counter**
to turn it on. Existing Energy Bar appearance, glow, animations, geometry,
calibration and language preferences are preserved.

## Updating

Close Aion 2 Helper, extract **Aion-2-Helper-v1.2.0-windows-x64.zip** into your
existing folder and replace the program files. Extract all files together,
including the **tools/presentmon** folder. Keep **overlay-settings.json** to
retain your preferences; personal settings are not included in the ZIP.

Npcap remains a separate requirement for Energy Bar. FPS uses Windows events.
The counter measures successful game presentations over the previous second;
Windows may deliver new samples about once per second even when a shorter
display interval is selected. Driver-generated frames are not added. A dash
means no current reading is available.

## Validation

- Real Aion 2 readings and in-game widget visibility confirmed on the development PC.
- Refresh changed from 2000 to 100 ms without restarting the measurement worker.
- Packaged-app checks cover FPS parsing, native event pairing/exclusions,
  preferences, migration, resizing, disabled controls, undo/redo and EN/ES.
- Existing settings and Energy Bar rendering/resource checks remain passing.

This is a public preview tested on the development PC. FPS event availability and
fullscreen overlay visibility can vary by system.

---

# Aion 2 Helper v1.1.0 — English & Spanish

This public preview adds language selection and a dedicated General settings page.

## What's new

- Added **General** above **Energy Bar** in Settings.
- Choose **English** or **Español** from the language dropdown.
- Apply language changes immediately to Settings, the tray menu, the color picker,
  and normal energy status messages. No restart is required.
- Save the selected language automatically and restore it on the next launch.
- Undo and redo language changes using the buttons or **Ctrl+Z / Ctrl+Y**.
- General settings remain available when Energy Bar is disabled.

Existing Energy Bar colors, glow, animations, position, size, calibration, and lock
preferences are preserved. English is the default for new installations and older
settings files. Low-level diagnostic tools and native errors remain in English.

## Updating from v1.0.0

1. Close Aion 2 Helper using its tray menu.
2. Extract **Aion-2-Helper-v1.1.0-windows-x64.zip** into your existing Helper folder,
   replacing the program files.
3. Keep **overlay-settings.json** to retain your preferences. It is not included
   in the release ZIP.
4. Run **Aion2Helper.exe** and open **Settings → General → Language**.

For a fresh installation, extract all ZIP contents together. **Npcap must be
installed separately** to receive game energy readings. Enter the game with your
character and dash once to start receiving updates.

## Validation

- Packaged-app settings tests: live EN/ES selection, translated controls/tray/picker,
  language history, persistence after restart, and legacy/invalid language fallback.
- Overlay tests: emissive rendering, animation, resizing, disabled-state behavior,
  bitmap reuse, and GDI resource checks.

## Current scope

Energy Bar is the available module. DPS tracking, event notifications, FPS, and
custom HP/MP bars remain planned features. This preview has been tested on the
development PC; game protocol and fullscreen presentation can vary by system.

The ZIP excludes personal settings, traffic captures, credentials, and reference
checkouts. Npcap and the game are not bundled. No open-source license has been
selected for the project code; the third-party opcode table retains its MIT license.
