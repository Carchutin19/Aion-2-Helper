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
