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
