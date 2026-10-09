# Aion 2 Helper

**Customizable utility overlays for Aion 2.**

## Download

Download the Windows ZIP from [Releases](https://github.com/Carchutin19/Aion-2-Helper/releases).
The [initial public preview](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.0.0)
includes a [Windows ZIP download](https://github.com/Carchutin19/Aion-2-Helper/releases/download/v1.0.0/Aion-2-Helper-v1.0.0-windows-x64.zip).
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
- Undo/redo through buttons or **Ctrl+Z / Ctrl+Y**.
- A persistent tray icon and a shared lock for current and future widgets.

Energy readings, hidden-nameplate operation, and visibility with Aion's Fullscreen
setting were verified on the development PC. Overlay visibility on other systems
may vary with the game's presentation mode.

## Planned features

These are development goals, **not features included in the current version**:

- **DPS meter:** combat damage tracking and a configurable on-screen display.
- **Useful notifications:** on-screen reminders, including alerts five minutes
  before supported game events start.
- **FPS display:** a small performance widget.
- **Custom status bars:** player health and mana, and target/enemy bars where
  the required data is available.
- **Language selection:** English is the current application language;
  additional languages are planned.

The availability and scope of future modules depend on what can be reliably read
or measured. They are not activated by any setting in the current build.

## Getting started

This is a Windows desktop application. The current reader requires **Npcap** and
was developed and tested against Aion 2 Global game traffic.

1. Run **Aion2Helper.exe** or **Aion-2-Helper.cmd**.
2. Enter the game with your character and dash once to receive an energy update.
3. Right-click the icon next to the clock and choose **Settings**. Double-clicking
   the icon also opens Settings.
4. Choose **Unlock**, then drag the widget's center to move it. Drag an edge or
   corner to resize it. Exact dimensions can also be entered in Settings.
5. Choose **Lock** when finished. Locked widgets let clicks pass through to the game.

The tray menu contains **Settings**, **Lock / Unlock**, and **Quit Aion 2 Helper**.
The lock is shared across widgets; there is no F10 shortcut.
The overlay has no taskbar button. Windows may place the tray icon in the
hidden-icons menu.

### Settings

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
100 changes, including colors, effects, position, size, calibration, and lock state.
History survives reopening Settings within the same application session and resets
when the app exits. A new edit after undo replaces the pending redo branch.
Text fields retain their local undo while editing.

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

Source files are in **src/**. **Build.ps1** uses the installed Windows .NET Framework
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
