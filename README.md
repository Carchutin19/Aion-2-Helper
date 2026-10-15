# Aion 2 Helper

**Customizable utility overlays for Aion 2.**

**Alpha software — actively in development.** Energy/Stagger and experimental Enemy/Enemy-target progress bars, FPS/Ping widgets and a
DPS Meter in **Beta**, Notifications and optional intro/visual tweaks are available now. **The DPS Meter is in Beta and still incomplete: combat
data is partial, some events are not yet supported, and we are actively working
on broader coverage and validation.** Behavior may vary between PCs and game
updates. Please report
problems through [GitHub Issues](https://github.com/Carchutin19/Aion-2-Helper/issues).

## Download

Download the Windows ZIP from [Releases](https://github.com/Carchutin19/Aion-2-Helper/releases).
The [v1.13.0 release — Experimental Enemy Bars and Built-in Updates](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.13.0)
includes a [Windows ZIP download](https://github.com/Carchutin19/Aion-2-Helper/releases/download/v1.13.0/Aion-2-Helper-v1.13.0-windows-x64.zip).
Extract all
files into one folder and run **Aion2Helper.exe**. Npcap must be installed separately
for Energy/Stagger bars, DPS Meter and invitation notifications;
the game must be running with your character in the world to receive energy updates.

**Recommended startup order: always open Aion 2 Helper before launching Aion 2.**
This helps the helper receive your character's initial maximum-stamina stat when
you enter the world. Opening it after character entry can miss that stat and leave
a saved/default reference that may affect auto-hide. If this happens, return to
character selection and enter again with the helper running. Maximum-stamina
changes during play are still updated automatically when the game sends them.

Aion 2 Helper is a community project designed to keep useful information visible
while you play, with movable widgets, a clean interface, and a focus on low overhead.
It starts with an independent dash and sprint energy bar and is intended to grow
into a collection of useful overlays for Aion 2.

## New in v1.13.0: Experimental enemy bars and built-in updates

**Enemy health and Enemy target are experimental.** Configure them in
**Settings → Progress bar**. The main bar shows name, received current/max HP
and percentage, with optional level, creature type and distance. The second bar
shows the health/name of whoever the enemy targets, with independent bar/name
colors when that player is you. Both support unlocked attacks by default and
an optional locked-target-only filter.

- Independently place/style names, values, distance, type and locked/unlocked
  labels. Optional black outlines improve contrast; whole-bar scale is available
  separately for all four progress bars.
- Sharper enemy text with cached rendering. The character-recovery notice now
  sits above the DPS widget instead of covering its rows.
- **General → Application updates:** installed/latest versions, manual checking,
  update action and a startup-check switch. A startup offer can download/install
  a newer stable GitHub version, show progress and restart the helper.
- Downloads use HTTPS, SHA-256 and checked package paths. Program files are
  backed up before replacement; install/check failures attempt restoration.
  Personal settings, combat history and game-tweak backups are preserved.
- **Download v1.13.0 manually once** to get the updater. The first real update
  from this version will be checked with a following release; it is not yet a
  completed live version-to-version validation.

**Known limitations:** missing initial enemy/player profiles can hide either
bar if the helper starts late; open it before Aion. The captured **Pesadilla**
NPC-range issue is corrected, but other instances/unknown NPCs still need tests.
**Metres may still be missing after reconnects/instance changes**; the session-key
lookup fix remains pending for a later release. Exact distance is optional,
default-off and reads game memory; see [How it works](#how-it-works). Missing data
is not replaced with fabricated health or distance. The DPS Meter remains Beta;
open-world boss party-damage freezes and Energy PvP remain pending.

See the [full v1.13.0 release notes](RELEASE-NOTES.md) for behavior, validation,
update privacy, experimental limits and troubleshooting.

## New in v1.12.1: Intro status recovery and diagnostics

**The v1.12.1 ZIP includes this hotfix and all v1.12.0 features.** Intro status
was recovered in the reported local case with the installed video and original
backup unchanged. The exact earlier cause was not captured; persistent warnings
still need diagnostic evidence.

- Specific intro record errors replace the generic warning where possible.
- **Game Tweaks → Startup → Copy intro diagnostics** provides version, paths,
  record state and error details for feedback. Reports stay local and may contain
  your Windows username/paths; review them before sharing publicly.
- Cleaner Startup menu; intro apply/restore protections remain intact.

If a warning persists, click **Refresh status**, then copy the diagnostic report.
See [release notes](RELEASE-NOTES.md) for details.

## New in v1.12.0: Combat UI, adaptive notifications and visual tweaks

**The v1.12.0 Windows ZIP includes all changes below.** See the
[release notes](RELEASE-NOTES.md) for details and known limitations.

- **Clearer DPS menus:** Combat, History and Configuration. Skills are integrated
  with player statistics; visible tables no longer show skill or target IDs.
- **Role-colored DPS/HPS charts:** select a player to see their fight averages,
  contribution, supported critical statistics and skill breakdown. Live combat
  and saved fights use the same layout. Charts show average rates, not a timeline.
- **More usable Settings:** responsive tables, expandable details and mouse-wheel
  scrolling over the menu content.
- **Notifications fit their text:** shorter backgrounds for shorter messages,
  wrapping for longer ones, adjustable maximum width/scale and one generic test
  button. Existing notification types, sounds and appearance settings remain.
- **Game Tweaks → Visuals:** independent Depth of Field, Chromatic Aberration,
  Film Grain and Motion Blur controls, automatic Engine.ini discovery, a profile
  selector/manual fallback and verified backups/restoration. Other INI settings
  are preserved and the file stays writable.

**Close Aion 2 before applying visual changes and relaunch it to check the result.**
Changes with the game open are not guaranteed. The helper blocks writes while the
game runs; opening Settings or using undo/redo does not edit game files.

**DPS remains Beta and incomplete.** This update improves presentation rather
than resolving every unsupported event. Open-world boss party-damage freezes and
Energy PvP remain pending; wider group/healing and visual-effect testing continues.

## New in v1.11.0: DPS Meter Beta, visibility and stagger animation

**The v1.11.0 Windows ZIP includes all the changes below.** The DPS Meter is now
in **Beta** after substantial progress; broader live group/healing tests continue.
See [release notes](RELEASE-NOTES.md) for the full update and remaining limitations.

- **Skill names ready before combat:** embedded catalogue of 16,899 definitions,
  with English and available Spanish names. Local character skills/levels are
  separate. Unknown names retain numeric IDs and validated totals.
- **Live skill breakdown:** open **DPS Meter → Combat** (Skills was integrated in v1.12.0) to inspect damage,
  periodic damage, healing done/received, hits/ticks, highest hits and supported
  critical statistics by player, skill and target. Histories save the new details,
  show the selected player's skills and include a breakdown in TXT exports.
  Older histories remain readable.
- **Broader supported combat coverage:** more compatible DoT/HoT and healing
  families, explicit tick attribution and bounded recovery when player identity
  arrives after a valid impact. Unverified effects are still excluded.
- **Cleaner combat widget:** Beta label and no partial-damage/healing footer.
  **DPS and Energy hide when character data is missing**. After 60 continuous
  seconds, a separate click-through recovery message appears at the DPS position
  while the game is connected. It disappears on recovery and remains absent with
  the game closed. Real area changes clear stale energy samples; unlocked widgets
  remain available for positioning.
- **Stagger fills during the countdown:** a visual empty-to-full animation follows
  the estimated duration (5 seconds by default). Numeric labels retain real gauge
  values. New game values and phase changes override the animation; repeated zero
  updates do not restart it. Stale-zero and unavailable-phase hiding are retained.

Solo live names/damage and zone/identity recovery were checked; local tests and
offline capture comparisons passed. **Beta is still incomplete:** group/healing
live testing continues, open-world boss party damage may freeze, Energy PvP is
pending, and effective healing, causal buff effects and per-player stagger remain
unvalidated. Retained features include Progress bar settings, independent
FPS/Ping, invitation/event notifications, display adaptation and intro skip.

## New in v1.10.0 Alpha: Progress bar

**Settings → Progress bar** has separate **Energy bar** and **Stagger** tabs.
The **v1.10.0 ZIP includes all the features below**, plus the independent FPS/Ping
widgets introduced in v1.9.0. See [release notes](RELEASE-NOTES.md) for the update
history. The Stagger reader requires Npcap and shares the existing game capture.

- **Independent Stagger widget:** reads the supported enemy's current and maximum
  stagger gauge, with its own enable switch, colors, glow, dark track, smoothing,
  fades, position and size. Move and resize it with the shared widget lock.
- **Values on both bars:** optionally show the current value, maximum and
  percentage. Give each label its own above/below, left/center/right placement
  and vertical offset; adjust text size and color.
- **Separate stagger countdown:** optional estimated remaining seconds after
  the observed stagger trigger, with independent position, vertical offset,
  size, color, glow and shrinking effect. The default estimate is five seconds;
  it never invents a gauge refill or overrides actual game data.
- **Phase-aware visibility:** supported inactive-phase updates replace old values
  and hide the Stagger widget. Actual active-phase data makes it visible again.
  A gauge left at zero hides after five seconds until new eligible positive data
  arrives. The Stagger bar has no full-gauge auto-hide; **Energy auto-hide stays
  unchanged**. Unlocked widgets remain accessible for positioning.
- **Stability and preferences:** tolerate small capture/UI timestamp differences
  that could hide a valid gauge; retain the tracked enemy through unrelated
  area hits. New settings support English/Spanish, saving, undo/redo and display
  adaptation. Editing progress settings preserves independent Ping preferences.

**Stagger is experimental.** Local comparisons and a replay of the reported
multi-phase boss encounter validate the supported updates, but wider live testing
is still needed. Selection follows the last directly attacked enemy with known
stagger data; it does not read the game's UI target selector. An initial attack
may be needed before the bar appears. Unknown enemies and unsupported phases
are not assigned invented values. Per-player stagger contribution is not yet
available in the DPS Meter.

**DPS remains incomplete:** new periodic damage/healing patterns are being
investigated, but unverified effects have not been added to totals. The known
open-world boss party-damage issue and Energy Bar PvP issue remain pending.

## New in v1.9.0 Alpha: FPS/Ping

**Settings → FPS/Ping** now has separate **FPS** and **Ping** tabs and independent
widgets. Existing FPS preferences are preserved.

Ping shows Windows' smoothed TCP round-trip time in milliseconds for an
established AION2 game-server connection on port 13328. It measures the network
connection, not server processing time or the game's internal latency counter.
It sends no extra ping packets and does not require Npcap. Windows normally
requires an elevated helper to enable these per-connection statistics; if access
is denied, the Ping tab explains how to restart the helper as administrator and
offers **Copy Ping diagnostics**. No automatic elevation prompts are added.

Enable either widget independently. Each retains its own text color, optional
soft black background, opacity, softness, size, position and refresh interval.
Move/resize it directly while unlocked; lock it for click-through operation.
Ping defaults to a 1000 ms refresh (configurable from 250 to 5000 ms), starts
disabled for existing installations, supports undo/redo, and follows the same
monitor/resolution adaptation as the other widgets. Connection discovery and
sampling run in the background with bounded storage. Old readings expire, game
connection changes are rediscovered, and disabling Ping stops sampling and
releases collection enabled by this helper. Statistics already enabled by
another application are preserved. IPv4 and IPv6 discovery are supported;
live validation on the development PC used IPv4.

**New known issue — Energy Bar in PvP:** energy readings were reported not to
work during PvP. This is recorded for investigation; the cause and scope are
not yet confirmed, and this development change does not fix it.

## New in v1.8.1 Alpha

Energy Bar, FPS Counter, DPS Meter and Notifications automatically adapt their
position and size to Windows display resolution and monitor changes. The saved
reference preserves the last user-edited layout, including across restarts.
Returning to the original display restores that reference. Matching monitors
keep their widgets when rearranged; removed monitors fall back to the primary
screen. Widget size limits and the taskbar area remain respected. Older settings
have no screen reference, so inaccessible widgets are safely brought back into
view; the old resolution is not guessed. Adaptation uses display events rather
than per-frame polling.

**Settings → FPS Counter → Geometry → Reset FPS position** restores a visible
position without resetting appearance or refresh speed. Diagnostics include
widget visibility and bounds to distinguish an off-screen counter from missing
measurement data.

**Energy Bar → Behavior** now explains auto-hide, the shared widget lock and how
to receive a missing maximum-energy stat. A waiting-for-maximum message appears
even below or at 100% of a provisional saved reference. See the troubleshooting
steps under **Automatic energy capacity** below.

### Retained from v1.8.0

**Settings → Game Tweaks → Startup** adds a reversible NCSOFT intro skip using
our own generated black video. The game folder is detected automatically when
needed, including additional Steam libraries. If detection fails, choose it
manually. Apply and restore are explicit actions with the game closed and a
verified backup. The intro skip and normal mouse input were tested locally.
No files or scripts from another mod are required.

### Retained from v1.7.1

**Settings → FPS Counter** now shows a visible explanation when measurement fails,
including permission denials, a missing PresentMon executable, Windows errors,
missing game events and reader disconnections. **Copy FPS diagnostics** copies
the app version, Windows/process information, actual permission state, reader
connection, dependency presence and available error codes for a bug report.
No packet captures, personal settings, credentials or FPS session tokens are included.

The administrator action hides when the helper is already elevated or a requested
elevated reader is running. A running game without a reported main window can
now reach the bounded PresentMon fallback. Unexpected reader disconnections
release their resources and recover with a delay; there are no automatic UAC prompts.
These fixes passed local checks, but the cause of a failure on another PC still
requires that PC's diagnostics. Widget appearance and saved preferences are retained.

### Retained from v1.7.0

Notifications now distinguish normal party invitations from dungeon-group
invitations, including the re-invitation when converting a party for a dungeon.
Dungeon notices show the sender's name and a specific message in English or Spanish.
**Settings → Notifications** provides an independent switch for each invitation
type; sound, fades, appearance and position remain shared. Choices save automatically
and support undo/redo. Settings → General shows the update summary.

The validated weapon-poison effect, per-player hit tags, highest hits and history
controls remain available. **DPS coverage is partial; the open-world boss
party-damage issue remains unresolved.**

## Available now: Game Tweaks

**Settings → Game Tweaks → Visuals** edits only four renderer overrides in your
Engine.ini: Depth of Field, Chromatic Aberration, Film Grain and Motion Blur.
Enabled uses the game's settings; disabled writes zero. With no overrides, all
four default to enabled. The game may still choose whether an effect is rendered.

Configuration discovery looks under **%LOCALAPPDATA%/AION2**, including
**Saved_Steam/Config/Windows/Engine.ini** and other launcher profiles. Select your
profile if several are found, or use **Browse Engine.ini**. With Aion closed,
choose effects and click **Apply visual changes**; relaunch to check the result.
Live changes are not guaranteed, so writes are blocked with Aion running.

Backups stay under **LocalAppData/Aion2Helper/VisualBackups**. **Restore previous
visual change** undoes the last change, preserving unrelated later edits and
rejecting conflicting visual settings. No whole-file replacement from another
INI is used. Paths, DLSS/FSR and unrelated settings are retained; encoding/line
endings are preserved and Engine.ini stays writable. Grain quantization/dithering
is separate from Film Grain and is not edited. Game updates may rewrite the file;
**Refresh status** checks it without applying changes. Visual effects still need
live validation after restart. Startup intro skipping is independent.

**Settings → Game Tweaks → Startup** provides our own reversible **Skip the
NCSOFT intro** function. The helper first detects the game installation from the
running game, Windows installation records, Steam libraries/manifests and common
NCSOFT locations. A valid saved folder is reused. If the folder was moved or
cannot be detected, use **Browse folder**. **Detect game folder** retries the
search. Detection runs once on opening this section when needed, or on request;
it does not scan entire drives or poll while you play.
No external mod ZIP, download or batch script is required. The helper embeds its
own independently generated one-frame black video, without audio.

Close Aion 2, switch **Skip intro** on and click **Apply change**. The helper
verifies a backup of the installed original, then atomically replaces
**Aion2/Content/Movies/CI_Type_A.bk2** with its own short video. The expected
filename remains present so playback can complete. **Restore original** puts
back the exact saved original. The earlier rename prototype has been replaced;
restoration remains available for existing installations of that prototype.
Startup with intro skipping and normal mouse input have been verified locally.
This is a startup
convenience, not a gameplay FPS optimization.

Backups are retained outside the game folder under Local AppData/Aion2Helper/
IntroBackups. The tweak stays applied when the helper closes; Settings undo/redo
does not modify game files. Game updates can recreate the video: refresh status
before applying again. Refresh status only checks files; it never applies or
restores a tweak. Backups are verified and versioned, unrelated or changed
files are preserved, and no continuous game-file monitoring is added. Local
folder choices are stored in **game-tweaks.json**, excluded from releases.
Private file-fixture tests cover application, restoration, legacy recovery and
preservation of updated files. Asset provenance and generation instructions are
in [assets/startup](assets/startup/README.md).

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

Enable the FPS widget in **Settings → FPS/Ping → FPS**. It is off by default on
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
close the helper, then right-click **Aion2Helper.exe → Run as administrator**
to try measurement with elevated permissions. Settings also has a conditional
**Start measurement as administrator** button for the measurement worker only;
it is hidden while waiting for the game, receiving FPS, when the helper is
already elevated, or while its requested elevated reader is running. Accepting
UAC confirms permissions, not that Windows is delivering game FPS events.

If you see **— FPS**, open **Settings → FPS/Ping → FPS** and read the explanation
directly below the preview. Keep Aion in the foreground, then use
**Measurement → Copy FPS diagnostics** and paste the report into a GitHub issue
with a short description. For an elevated-reader connection failure, toggle the
FPS module off and on before retrying. A missing PresentMon file requires
extracting the full ZIP (including **tools/presentmon**); check antivirus quarantine
if the file disappears.

An em dash means no
current reading; it is never
replaced with an invented FPS value.

The black background has soft transparent edges; it does not capture or blur
pixels from the game. The Energy Bar's glow and animations remain independent.

## Available now: DPS Meter (Beta)

**The DPS Meter is in Beta and is still not a complete damage or healing log.**
It counts the event formats currently identified and validated; unsupported or
unidentified events can be missing. Totals and rankings can therefore be incomplete.
We are actively working on additional data, reliability and protocol coverage.

Enable it in **Settings → DPS Meter → Configuration**. It starts disabled on new
installations and on updates without previous DPS settings. Enter with your
character. Missing identity hides the widget; after 60 seconds a separate notice
explains how to recover it by changing zones, dashing or logging in again.

- Preloaded skill names, role-colored DPS/HPS charts and per-player skill
  breakdowns in **Combat**, with expandable statistics, bounded saved details
  in History and raw skill/target identifiers retained in TXT exports.
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
contributions, damage during stagger, aggro and zone names are pending. The
separate experimental Stagger widget is available under Progress bar.
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

Notifications include incoming **normal party invitations**, **dungeon-party
invitations** and two **Shugo Festival** alerts, each with its own switch:

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
- **Notification types → Party invitations / Dungeon party invitations:**
  enable or disable each type independently. Dungeon notices say
  **“{Player} is inviting you to a dungeon party.”** Shugo advance/opening
  alerts also have separate switches.
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
Repeated copies of the same invitation are deduplicated; a normal invitation does
not suppress a subsequent dungeon invitation. Turning off one invitation type
clears only that type's queued and active notices. Global disable
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

This is a Windows desktop application. **Npcap** is required for Energy/Stagger
bars, DPS Meter and invitation notifications; FPS uses Windows presentation events and Ping uses
per-connection Windows TCP statistics. The application was
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

1. **Open Aion 2 Helper before launching the game:** run **Aion2Helper.exe** or
   **Aion-2-Helper.cmd**. If FPS measurement needs permission on your PC, use the
   administrator options below.
2. Launch Aion 2, enter with your character, and dash once to receive a current
   energy update. Opening the helper first lets it listen for initial maximum
   stamina; a dash alone does not guarantee that maximum is resent.
3. Right-click the icon next to the clock and choose **Settings**. Double-clicking
   the icon also opens Settings.
4. Open **Settings → FPS/Ping** and enable FPS and/or Ping independently.
   For Ping, run the helper as administrator if Windows denies TCP-statistics access.
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

- Close the helper from its tray menu, then right-click **Aion2Helper.exe →
  Run as administrator** and accept the Windows prompt to launch the helper
  with administrator permissions. This is a Windows action, not an always-visible
  button in the FPS settings.
- **Settings → FPS/Ping → FPS** also contains **Start measurement as administrator**,
  which only appears when FPS is enabled and no reading is available, after the
  initial start/wait-for-game states. If it appears, it elevates only the FPS worker.

The button appears when measurement has no current FPS reading; it does not
necessarily mean access was denied. If elevation does not help, check that Aion 2
is running and report the measurement status in GitHub Issues. Administrator
permissions do not replace the separate Npcap installation needed by Energy Bar,
DPS Meter and Notifications.

### Settings

**General**, above Progress bar, includes an **English /
Español** language selector. Changes apply immediately to Settings, the tray menu,
the color picker, and normal energy status messages, and persist across restarts.
English is the default for new installations and preferences from earlier versions.
Low-level diagnostic tools and native error messages remain in English.

Energy Bar has **Behavior**, **Appearance**, and **Position & size** tabs,
with a persistent preview above them.

**Progress bar** replaces the Energy Bar section.
Choose **Energy bar** or **Stagger** before adjusting their independent settings;
the Stagger tab also includes its separate countdown controls.

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

If auto-hide does not work, open **Settings → Progress bar → Energy bar → Behavior**, enable
**Auto-hide**, and **Lock** the widgets from the tray menu. Unlocked widgets stay
visible for editing. Wait for full energy and the configured **Delay at 100%**
plus fade. If the status says **waiting for maximum energy stats**, return to
character selection and enter again while the helper is running, or start it
before character entry next time. A dash identifies current energy but does not
guarantee the missing maximum is resent. This is guidance for a known missing-stat
case; other causes still need a report with the version, status and lock state.

With no current game data, the bar shows a grey dashed line and Settings reports
the missing signal. Missing data is never treated as full energy.

## How it works

The energy, combat and notification modules passively capture Aion TCP traffic through Npcap,
reconstruct the stream, decode framed/LZ4 messages, and read supported energy,
identity, party, incoming invitation and combat updates.
By default these modules do not read game memory. The optional **Progress bar →
Enemy health → Details → Exact distance (reads game memory)** setting is off by
default. When enabled together with the enemy bar and metres, it reads the game
session key from Aion's process memory to decode outgoing movement and calculate
distance from player and enemy coordinates. Enable it before starting Aion;
if the connection was already open, fully restart the game to capture its
handshake. Administrator access may be required. Keys and decrypted packet
bodies are not saved or logged; disabling the option clears its session state
and stops the memory reader. Missing or unsupported data hides the distance.
This changes the access method and does not guarantee anti-cheat compatibility.
The helper does not write game memory, inject code, send game packets, or
automate character actions.

The optional **Game Tweaks → Startup** feature changes the startup video, while
**Game Tweaks → Visuals** edits the four supported Engine.ini renderer overrides.
Both require explicit application with the game closed, keep verified backups
for restoration, and remain applied after the helper closes.

The normal overlay processes readings in memory and saves completed combat
summaries locally. It does not save packet captures or upload gameplay data. The optional startup/manual
update check contacts GitHub for the latest stable release and downloads only when
you choose to update; disable startup checking in General if preferred. **live-status.json** contains the latest
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
.\DashProbe.exe --visual-tweaks-test
.\DashProbe.exe --game-tweaks-test
.\DashProbe.exe --settings-test
.\DashProbe.exe --progress-test
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

Progress checks also cover active/inactive stagger phases, actual refill updates,
the five-second zero fallback, timestamp tolerance, independent value placement,
countdown controls and settings migration. Captured phase transitions were
replayed privately; those captures are not part of the repository or download.

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
