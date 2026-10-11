# Aion 2 Helper

**Customizable utility overlays for Aion 2.**

**Alpha software — actively in development.** Energy Bar, FPS Counter and an
experimental DPS Meter and Notifications are available now. **The DPS Meter is incomplete: combat
data is partial, some events are not yet supported, and we are actively working
on broader coverage and validation.** Behavior may vary between PCs and game
updates. Please report
problems through [GitHub Issues](https://github.com/Carchutin19/Aion-2-Helper/issues).

## Download

Download the Windows ZIP from [Releases](https://github.com/Carchutin19/Aion-2-Helper/releases).
The [v1.6.1 alpha release](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.6.1)
includes a [Windows ZIP download](https://github.com/Carchutin19/Aion-2-Helper/releases/download/v1.6.1/Aion-2-Helper-v1.6.1-windows-x64.zip).
Extract all
files into one folder and run **Aion2Helper.exe**. Npcap must be installed separately
for Energy Bar, DPS Meter and Notifications;
the game must be running with your character in the world to receive energy updates.

Aion 2 Helper is a community project designed to keep useful information visible
while you play, with movable widgets, a clean interface, and a focus on low overhead.
It starts with an independent dash and sprint energy bar and is intended to grow
into a collection of useful overlays for Aion 2.

## New in v1.6.1 Alpha

The validated weapon-poison effect now counts towards total DPS and periodic
damage, with existing per-player highest-hit, history and TXT export support.
Amounts come from game events; there is no fixed damage estimate or new toggle.
Paralysis is excluded from damage. Other weapon effects and variants still need
validation. Existing history is retained and is not recalculated.
The open-world boss party-damage issue remains unresolved.

DPS history adds hit tags, each player's highest hit, a minimum archive duration,
and separate fight/player deletion. **Settings → General** shows the update
summary and the unresolved open-world boss limitation in English or Spanish.
Supported directional hits against already-visible targets can also recover
after a helper restart when fresh target HP drops corroborate them. This remains
a conservative, partial fallback and does not fix the open-world boss issue.

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

Maximum energy is detected automatically from server stat updates, independently
of current energy. Changes to capacity update the percentage and full-energy
hiding without manual calibration. Calibration controls have been removed from
Settings. Until a server maximum arrives, the saved reference is used; start the
helper before entering the game, or re-enter with your character if the initial
stats were missed. Glow, smoothing and fades are preserved.

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

## Available now: experimental DPS Meter

**This is an early alpha combat meter, not a complete damage or healing log.**
It counts the event formats currently identified and validated; unsupported or
unidentified events can be missing. Totals and rankings can therefore be incomplete.
We are actively working on additional data, reliability and protocol coverage.

Enable it in **Settings → DPS Meter → Configuration**. It starts disabled on new
installations and on updates without previous DPS settings. Enter with your
character; if identification is pending, the widget asks you to dash once.

- Direct damage, DPS, supported damage over time, healing done, healing received
  and HPS, with **Damage / Heals / Received** tabs on the widget.
- **Self**, **Party** and **Nearby players** filters. Nearby means information
  actually received by your client, not everyone in the world or server.
- Existing-party recovery and roster updates across joining/leaving and tested
  dungeon transitions. Recovery may take time while identity/party data arrives.
- Critical amount share and observed critical impact frequency for each view.
- Custom damage/healer/tank role colors, optional background, adjustable refresh,
  row count, inactivity timeout, auto-hide delay, fades and bar smoothing.
- Move/resize the widget while unlocked or enter its geometry in Settings.
- Optional localized **You / Tú** label and an independent option to pin yourself
  first. Both start off; real names remain in saved records.
- English/Spanish, automatic preference saving and shared configuration undo/redo.

**What the critical columns mean:** critical amount share is the percentage of
eligible primary direct damage/healing attributed to critical results. Critical
impact frequency is the percentage of eligible primary direct events that were
critical, **not your character's theoretical critical chance**. Additional
impacts and periodic ticks stay outside these denominators. A dash means there
are no eligible events. Positive critical-healing results still need live validation.

**Known issue under investigation:** party members' damage can stop updating
during open-world bosses even while they continue attacking nearby. This is
unresolved; totals and rankings may be incomplete. Successful normal-enemy tests
do not establish that this boss scenario is fixed.

**Known limits:** only supported damage/healing and identified periodic families
are counted. Healing is the announced restoration amount; effective healing and
overhealing are not available. Complete buff/debuff tracking, damage/healing
attribution to buffs, complete positional metadata, boss identification, stagger
contributions/gauges, damage during stagger, aggro and zone names are pending.
Item-versus-skill restoration separation is also pending. Settings previews are
sample data. Protocol changes can require updates; tested cases do not establish
complete damage accuracy.

### Combat history by player

Open **Settings → DPS Meter → History** to inspect completed encounters. Each
fight stores a separate row for every identified player with accepted metrics,
independently of the live widget filter or row limit. Filter by player to narrow
summary totals, detail rows and TXT exports, or choose **All players**. Scroll
the detail table horizontally for all statistics.

- Export the selected fight to TXT at a location you choose.
- Each fight row has its own **Delete fight** button, fixed at the start of the
  table. It removes the whole fight even when a player filter is active.
- **Delete player** removes only that player from the displayed fight; removing
  its last player removes the empty fight. Other fights remain unchanged.
- Clear all history at once.
- **Minimum fight duration** defaults to **60 seconds**; set **0** to save every
  fight. Duration runs from the first to last accepted impact, excluding the
  inactivity wait. Short fights still appear live; existing history is retained.
- Per-player **highest hit**, plus supported **Front / Back / Double / Perfect**
  counts and Front/Back Critical, Double Critical and Perfect combinations.
  Counts can overlap on one primary direct impact; unknown metadata and older
  histories show unavailable values. Highest hit is the largest primary damage
  impact or individual supported periodic tick, excluding extra-impact amounts.
  These fields are included in TXT exports.
- Set a maximum from **1 to 1000 fights**; default **100**. New records remove
  the oldest when the limit is reached.
- Optionally remove records older than a chosen number of hours or days.
  Age cleanup starts off, with seven days selected for when it is enabled.
- Inspect/export saved history even when the DPS module is disabled.

A fight ends after the configured gap without accepted combat impacts. Manual
reset, connection/area changes, disabling the meter and helper shutdown also
save an unfinished fight with its end reason. Encounters are not automatically
one record per dungeon or boss; pauses may split them. Same-map boss transitions
preserve the current encounter where supported.

Records persist locally in **combat-history.json**. Deleting records or applying
retention limits cannot be undone; settings undo does not restore erased records.
Names, actor IDs and accepted metrics are stored locally and included in exports.
Normal use adds no raw packet recording or automatic upload. History is saved
in the background on changes, with atomic replacement; no per-hit disk writes.

## Available now: Notifications

Notifications include incoming **party invitations** and two independently
switchable **Shugo Festival** alerts:

- **:55 each hour:** "Shugo Festival starts in 5 minutes."
- **:00:** "You can now sign up for Shugo Festival."

Shugo alerts follow the hourly schedule reported for the game. They use the
clock while Aion 2 is running and share notification sound, appearance and fades.
They do not verify registration availability with the server. Start the helper
before the alert time; it does not replay missed reminders after startup. Each
Shugo alert has its own toggle and test button, independent of party invitations.

When another player invites you to a party, a dark on-screen notice displays
that player's name, with an optional notification sound and smooth fade in/out.
Open **Settings → Notifications** to configure it:

- **Enable all notifications:** turn the entire module on/off without losing
  your individual choices.
- **Notification types → Party invitations:** independently enable/disable
  incoming party invitation alerts. Shugo advance/opening alerts have separate switches.
- **Common settings:** optional sound, volume, display duration, fade toggle
  and fade duration.
- Shared text color and size, background opacity, position, width and height
  for all notification types.
- Move/resize the notification area directly while widgets are unlocked.
  The editing sample is silent; locked notices let clicks pass through.
- A generic sample preview and **Test notification** button for checking the
  appearance and configured sound without receiving a game invitation.
- English/Spanish, automatic saving, shared lock and configuration undo/redo.

Notifications and party invitation sounds are **enabled by default** for new
installations and updates with no previous notification preferences. The
initial display duration is **6 seconds**, with **250 ms** fades and **80%**
sound volume. The sound file is included in **assets/sounds/party-invite.mp3**;
extract the complete ZIP so it remains available.

Receiving an invitation triggers the notice; accepting/joining a party does not.
Repeated copies of the same invitation are deduplicated. Turning off invitations
also dismisses an active invitation notice and stops its sound. Global disable
preserves the individual type selections. A new invitation from the same player
can show again. Notification rendering wakes on the event instead of waiting
for the Energy Bar's refresh or visibility, and it needs no dash to identify the
inviting player.

**Npcap is required for party invitations.** The incoming invitation format was verified on the Global
client with both accepted and declined invitations. Other invitation variants
and alliance requests have not been validated. Additional notification types are planned. Shugo reminders use the clock and need no packet capture.

## Planned features

These are development goals, **not features included in the current version**:

- **Expanded combat data:** more supported event types and validated boss,
  stagger, buff/debuff and healing details for the experimental DPS Meter.
- **Additional notifications:** on-screen reminders, for further supported game events beyond Shugo Festival.
- **Custom status bars:** player health and mana, and target/enemy bars where
  the required data is available.
- **More languages:** further translations beyond English and Spanish.

The availability and scope of future modules depend on what can be reliably read
or measured. They are not included in the current build.

## Getting started

This is a Windows desktop application. **Npcap** is required for Energy Bar,
DPS Meter and Notifications; FPS measurement uses Windows presentation events. The application was
developed and tested with Aion 2 Global.

### Install Npcap for Energy Bar, DPS Meter and Notifications

Npcap is a separate requirement and is not bundled in the ZIP. Download the
**Npcap Installer** from [the official Npcap site](https://npcap.com/#download),
run it with its default options, then restart Aion 2 Helper. Install the complete
package, which supplies the library and capture driver. Enter with your character
and dash once to begin receiving energy readings.

When Npcap is missing, **Settings → General** shows **Npcap is required for Energy
Bar, DPS Meter and Notifications** and a **Download Npcap** button opening the official download page. A library
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
6. Open **Settings → DPS Meter** and enable the experimental combat meter if wanted.
7. Open **Settings → Notifications** to choose alerts and their common appearance.
8. Choose **Lock** when finished. Locked widget bodies let clicks pass through
   to the game; the combat-view tabs remain clickable without activating the helper.

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
permissions do not replace the separate Npcap installation needed by Energy Bar,
DPS Meter and Notifications.

### Settings

**General**, above Energy Bar, includes an **English /
Español** language selector. Changes apply immediately to Settings, the tray menu,
the color picker, and normal energy status messages, and persist across restarts.
English is the default for new installations and preferences from earlier versions.
Low-level diagnostic tools and native error messages remain in English.

Energy Bar has **Behavior**, **Appearance**, and **Position & size** tabs,
with a persistent preview above them.

**Enable Energy Bar** controls the whole module. Disabling it hides the widget,
dims and disables its controls and preview. Capture stays active if DPS Meter
or enabled party notifications still need it. With all capture-dependent modules
off, capture stops; FPS remains independent. The enable switch,
general controls, and undo/redo remain available.

**Auto-hide** waits 1.5 seconds at full energy by default, then fades out over
200 ms. Using energy cancels the delay or reverses a fade smoothly. Turning off
Auto-hide keeps the bar visible at full energy.

The default palette transitions between green, dark orange, and dark red.
Colors and glow are fully adjustable. The default core size is 320 × 4 pixels;
thickness can be reduced to 3 pixels. Glow margins are separate from core size.

Preferences save automatically to **overlay-settings.json**. Undo/redo keeps up to
100 changes, including colors, effects, position, size, lock state,
and language.
Configuration undo history survives reopening Settings within the same application session and resets
when the app exits. A new edit after undo replaces the pending redo branch.
Text fields retain their local undo while editing.

To update from an earlier version, close Aion 2 Helper and extract the new ZIP into the same
folder, replacing the program files. Keep **overlay-settings.json** to preserve
your colors, effects, geometry, saved energy reference, and lock state. The release ZIP does
not contain or replace this file. Keep **combat-history.json** to retain saved
combats when updating from versions that already have history. Neither file is
included in release ZIPs.

**General status** separates the connection/reading message from the widget lock:
green when unlocked and red when locked.

### Automatic energy capacity

The helper reads both current energy and the maximum capacity sent by the game.
It updates capacity independently of the current amount, so gaining more energy
capacity no longer requires a Settings calibration or leaves the bar stuck
visible above 100%. Automatic stat changes do not add undo/redo steps.

The last detected maximum is saved across restarts. Start the helper before
entering the game to receive initial stats. If it was opened after you entered,
re-enter with your character to receive a fresh maximum. Until then, the saved
reference is provisional; it is not a guess based on partial energy. This also
matters after changing characters.

With no current game data, the bar shows a grey dashed line and Settings reports
the missing signal. Missing data is never treated as full energy.

## How it works

The energy, combat and notification modules passively capture Aion TCP traffic through Npcap,
reconstruct the stream, decode framed/LZ4 messages, and read supported energy,
identity, party, incoming invitation and combat updates.
It does not read or write game memory, inject code, send game packets, or automate
character actions.

The normal overlay processes readings in memory and saves completed combat
summaries locally. It does not save packet captures,
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
.\DashProbe.exe --dps-test
.\DashProbe.exe --notifications-test
.\DashProbe.exe --settings-test
.\DashProbe.exe --ui-test
.\DashProbe.exe --selftest
.\DashProbe.exe --replay-test captures/20261008-194328-734326/segments.jsonl
~~~

Checks cover configuration undo, persistent per-player combat history, supported
damage/healing/DoT parsing and exclusions, party recovery, localized views and
clickable locked tabs, color input, rendering, animation, native resizing,
TCP/LZ4 reconstruction, incoming invitation parsing/deduplication, independent
notification fade timers, preview/native image caches, capture adapters, and a
local replay fixture. The fixture
yielded 79 energy readings with zero decoder errors and matched an independent
Python analysis. Self-test requires Npcap; replay requires the local capture
fixture, which is not needed for normal use.

Settings tests render reference images into **designs/** over a fixed backdrop.
The live window uses a slightly transparent dark surface, rounded corners, and
a thin scrollbar. The logo and multi-resolution Windows icon are in **assets/**.

Internal implementation history and capture fixtures remain local. Attribution and consulted revisions
are in **THIRD-PARTY.md**. Local captures may contain connection addresses or game
identifiers; these development artifacts are not required to run the overlay.

Combat coverage and research limitations are documented in
**tools/COMBAT-RESEARCH.md**. Notification microbenchmarks and a brief resource
observation are documented in **performance/README.md** with their scope. Earlier
20-minute observations predate the combat/notification modules; no new long
gameplay comparison or helper-on/off FPS measurement was performed for v1.4.0.

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
