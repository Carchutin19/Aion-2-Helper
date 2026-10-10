# Aion 2 Helper

**Customizable utility overlays for Aion 2.**

**Alpha software — actively in development.** Energy Bar and FPS Counter are
available now, but behavior may vary between PCs and game updates. Please report
problems through [GitHub Issues](https://github.com/Carchutin19/Aion-2-Helper/issues).

## Download

Download the Windows ZIP from [Releases](https://github.com/Carchutin19/Aion-2-Helper/releases).
The [v1.2.2 alpha release](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.2.2)
includes a [Windows ZIP download](https://github.com/Carchutin19/Aion-2-Helper/releases/download/v1.2.2/Aion-2-Helper-v1.2.2-windows-x64.zip).
Extract all
files into one folder and run **Aion2Helper.exe**. Npcap must be installed separately;
the game must be running with your character in the world to receive energy updates.

Aion 2 Helper is a community project designed to keep useful information visible
while you play, with movable widgets, a clean interface, and a focus on low overhead.
It starts with an independent dash and sprint energy bar and is intended to grow
into a collection of useful overlays for Aion 2.

## Available now: Energy Bar

Track your character's actual dash and sprint energy without keeping the in-game
nameplate visible. The bar reads energy updates from game traffic rather than
estimating energy by counting key presses.

- A thin, adjustable bar with an emissive filament and soft glow.
- Move it on screen and resize width and thickness independently.
- Choose high, medium, and low energy colors, or use one fixed color.
- Adjust glow, dark track opacity, fades, and energy smoothing.
- Hide it automatically when full, with a configurable delay, or keep it visible.
- Enable or disable the module while retaining all settings.
- Settings with a live preview, integrated color picker, and automatic saving.
- English and Spanish, selectable in **General → Language** without restarting.
- Undo/redo through buttons or **Ctrl+Z / Ctrl+Y**.
- A persistent tray icon and a shared lock for current and future widgets.

Energy readings, hidden-nameplate operation, and visibility with Aion's Fullscreen
setting were verified on the development PC. Overlay visibility on other systems
may vary with the game's presentation mode.

## Available now: FPS Counter

Enable the FPS widget in **Settings → FPS Counter**. It is off by default on
new installations and updates, so you can choose whether to use it.

- A simple number in green by default, with an integrated text color picker.
- Optional soft black background with adjustable opacity and feathering.
- Position, width, height and scale in Settings, or direct drag/resize when unlocked.
- Independent enable/disable switch; disabling it stops measurement and hides it.
- Shared widget lock, undo/redo, automatic saving, English and Spanish.
- Adjustable refresh interval from 100 to 2000 ms; the default stays at 250 ms.

The FPS reader listens to Windows DXGI presentation events and counts successful
Aion 2 presentations over the previous second. Intel PresentMon is bundled as a
fallback for systems where that channel is unavailable. The display refreshes
at the chosen interval (250 ms by default); Windows may deliver event batches
about once per second.
The native reader filters events to the game processes. With Aion closed,
measurement waits without an active FPS trace or PresentMon process. Failed
fallbacks stop after a bounded trial and retry with a delay.
The reader can be activated before starting the game. It does not add
driver-generated frames. If Windows does not provide readings,
**FPS Counter → Start measurement as administrator** requests permission for the
measurement worker only. An em dash means no current reading; it is never
replaced with an invented FPS value.

The black background has soft transparent edges; it does not capture or blur
pixels from the game. The Energy Bar's glow and animations remain independent.

## Planned features

These are development goals, **not features included in the current version**:

- **DPS meter:** combat damage tracking and a configurable on-screen display.
- **Useful notifications:** on-screen reminders, including alerts five minutes
  before supported game events start.
- **Custom status bars:** player health and mana, and target/enemy bars where
  the required data is available.
- **More languages:** further translations beyond English and Spanish.

The availability and scope of future modules depend on what can be reliably read
or measured. They are not included in the current build.

## Getting started

This is a Windows desktop application. **Npcap** is required for Energy Bar
readings; FPS measurement uses Windows presentation events. The application was
developed and tested with Aion 2 Global.

### Install Npcap for Energy Bar

Npcap is a separate requirement and is not bundled in the ZIP. Download the
**Npcap Installer** from [the official Npcap site](https://npcap.com/#download),
run it with its default options, then restart Aion 2 Helper. Install the complete
package, which supplies the library and capture driver. Enter with your character
and dash once to begin receiving energy readings.

When Npcap is missing, **Settings → General** shows **Npcap is required for Energy
Bar** and a **Download Npcap** button opening the official download page. A library
that cannot load produces reinstall guidance. Successful validation clears the
warning. The help is also available with Energy Bar disabled; **FPS Counter works
without Npcap**.

### Run the helper

1. Run **Aion2Helper.exe** or **Aion-2-Helper.cmd**. If FPS measurement needs
   permission on your PC, use the administrator options below.
2. Enter the game with your character and dash once to receive an energy update.
3. Right-click the icon next to the clock and choose **Settings**. Double-clicking
   the icon also opens Settings.
4. Open **Settings → FPS Counter** and enable it if you want an FPS widget.
5. Choose **Unlock**, then drag the widget's center to move it. Drag an edge or
   corner to resize it. Exact dimensions can also be entered in Settings.
6. Choose **Lock** when finished. Locked widgets let clicks pass through to the game.

The tray menu contains **Settings**, **Lock / Unlock**, and **Quit Aion 2 Helper**.
The lock is shared across widgets; there is no F10 shortcut.
The overlay has no taskbar button. Windows may place the tray icon in the
hidden-icons menu.

### Administrator permissions for FPS

**Running as administrator is recommended if FPS readings do not appear on your
PC.** Windows permissions can differ between systems; some PCs receive FPS
without elevation, while others need it.

- In **Settings → FPS Counter**, enable the counter and click **Start measurement
  as administrator** when the button appears. Accept the Windows permission
  prompt. This elevates only the FPS measurement worker.
- Alternatively, close the helper from its tray menu, then right-click
  **Aion2Helper.exe → Run as administrator** and accept the Windows prompt to
  launch the whole helper with administrator permissions.

The button appears when measurement has no current FPS reading; it does not
necessarily mean access was denied. If elevation does not help, check that Aion 2
is running and report the measurement status in GitHub Issues. Administrator
permissions do not replace the separate Npcap installation needed by Energy Bar.

### Settings

**General**, above Energy Bar, includes an **English /
Español** language selector. Changes apply immediately to Settings, the tray menu,
the color picker, and normal energy status messages, and persist across restarts.
English is the default for new installations and preferences from earlier versions.
Low-level diagnostic tools and native error messages remain in English.

Energy Bar has **Behavior**, **Appearance**, and **Position & calibration** tabs,
with a persistent preview above them.

**Enable Energy Bar** controls the whole module. Disabling it hides the widget,
stops capture, and dims and disables its controls and preview. The enable switch,
general controls, and undo/redo remain available.

**Auto-hide** waits 1.5 seconds at full energy by default, then fades out over
200 ms. Using energy cancels the delay or reverses a fade smoothly. Turning off
Auto-hide keeps the bar visible at full energy.

The default palette transitions between green, dark orange, and dark red.
Colors and glow are fully adjustable. The default core size is 320 × 4 pixels;
thickness can be reduced to 3 pixels. Glow margins are separate from core size.

Preferences save automatically to **overlay-settings.json**. Undo/redo keeps up to
100 changes, including colors, effects, position, size, calibration, lock state,
and language.
History survives reopening Settings within the same application session and resets
when the app exits. A new edit after undo replaces the pending redo branch.
Text fields retain their local undo while editing.

To update from v1.0.0, close Aion 2 Helper and extract the new ZIP into the same
folder, replacing the program files. Keep **overlay-settings.json** to preserve
your colors, effects, geometry, calibration, and lock state. The release ZIP does
not contain or replace this file.

**General status** separates the connection/reading message from the widget lock:
green when unlocked and red when locked.

### Energy calibration

The character maximum defines the value corresponding to 100% energy. The value
verified for the development character was **113900**; it is not universal.

Enter the game, dash once to start receiving readings, wait until the in-game
energy is completely full, then click **Use current reading as maximum**.
The button saves the energy received at that moment. **It does not detect your
maximum automatically.** Using it with partial energy would give an incorrect
percentage. Recalibrate only when changing characters or when the bar no longer
matches the game.

With no current game data, the bar shows a grey dashed line and Settings reports
the missing signal. Missing data is never treated as full energy.

## How it works

The current module passively captures Aion TCP traffic through Npcap, reconstructs
the stream, decodes framed/LZ4 messages, and reads character energy updates.
It does not read or write game memory, inject code, send game packets, or automate
character actions.

The normal overlay processes readings in memory. It does not save packet captures,
upload data, or check for updates online. **live-status.json** contains the latest
reading and diagnostic state, written in the background at most twice per second.
The separate diagnostic tool records local captures only when explicitly started.

The reader targets the observed Global protocol, port 13328, and energy field
008D/u32/kind3. Protocol or port changes may require a reader update.
VPNs that hide game traffic can prevent reading.

Rendering reuses native images and glow buffers, avoids unnecessary redraws, and
reduces its timer rate when idle or disabled. Settings has no permanent animation
loop. Measurements and their limits are in **performance/README.md**; these are
development measurements, not an FPS guarantee for every PC.

Aion 2 Helper is an independent community project and is not affiliated with NCSOFT.

## Development

Source files are in **src/**. **Download-Dependencies.ps1** downloads the pinned
PresentMon console into the project and checks its SHA-256 and Intel signature.
**Build.ps1** runs that check automatically. **Build.ps1** uses the installed Windows .NET Framework
compiler and runtime libraries: WinForms for the native overlay, WPF for Settings,
and an embedded XAML theme. No additional SDK or NuGet package was needed on the
development PC.

~~~powershell
.\Build.ps1
~~~

The build produces **Aion2Helper.exe**, **DashProbe.exe**, and **AionDash.exe**.
AionDash is an alternate overlay entry point. DashProbe without arguments opens
the separate signal recorder; **Signal-Test.cmd** launches that recorder.

Use **Package.ps1** to create a Windows release ZIP and its SHA-256 checksum
from an explicit file allowlist. Local settings, traffic captures, and credentials
are never included in the package.

Existing checks:

~~~powershell
.\DashProbe.exe --fps-test
.\DashProbe.exe --settings-test
.\DashProbe.exe --ui-test
.\DashProbe.exe --selftest
.\DashProbe.exe --replay-test captures/20261008-194328-734326/segments.jsonl
~~~

Checks cover settings/history, color input, rendering, animation, native resizing,
TCP/LZ4 reconstruction, capture adapters, and a local replay fixture. The fixture
yielded 79 energy readings with zero decoder errors and matched an independent
Python analysis. Self-test requires Npcap; replay requires the local capture
fixture, which is not needed for normal use.

Settings tests render reference images into **designs/** over a fixed backdrop.
The live window uses a slightly transparent dark surface, rounded corners, and
a thin scrollbar. The logo and multi-resolution Windows icon are in **assets/**.

Internal implementation history and capture fixtures remain local. Attribution and consulted revisions
are in **THIRD-PARTY.md**. Local captures may contain connection addresses or game
identifiers; these development artifacts are not required to run the overlay.

## Support the project ☕

Enjoying Aion 2 Helper? Donations are welcome — coffee is our favorite buff! ☕

Support is completely optional. The app is free to use, and feedback, bug reports,
or a friendly thank-you are always appreciated too.

You can support the project through **Donations** on the
[Aion 2 Helper Nexus page](https://www.nexusmods.com/aion2/mods/9).

## License status

No open-source license has been selected for this project's code. Its publication
does not grant an open-source license. Third-party material keeps its own license;
see **THIRD-PARTY.md** and **protocol/LICENSE-MIT.txt** for the opcode table.

## References

- [Aion DPS Meter](https://github.com/SkeeveAN/Aion-DPS-Meter): protocol documentation
  and the MIT-licensed opcode table used for synchronization.
- [A2Tools](https://github.com/taengu/A2Tools-DPS-Meter): framing/compression reference;
  its Rust code is not included in the application.
- [NotMeter](https://notmeter.com/tutorial.html?lang=en&screen=en): feature research.
