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
