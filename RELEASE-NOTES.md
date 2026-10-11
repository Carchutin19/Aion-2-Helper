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
