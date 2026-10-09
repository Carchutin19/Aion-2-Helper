# Aion 2 Helper v1.0.0 — Initial public preview

The first public preview includes **Energy Bar**, an independent dash and sprint
energy overlay for Aion 2 Global.

- Move and resize the bar, including its width and thickness.
- Customize high/medium/low energy colors, glow, opacity, fades, and smoothing.
- Auto-hide at full energy, or keep the bar visible.
- Configure it through an English Settings window with preview and color picker.
- Undo/redo changes through buttons or Ctrl+Z / Ctrl+Y.
- Lock/unlock widgets from the tray icon; disable Energy Bar without losing settings.

## Installation

1. Download **Aion-2-Helper-v1.0.0-windows-x64.zip** and extract all files together.
2. Install Npcap separately if it is not already available on your PC.
3. Run **Aion2Helper.exe**, enter the game, and dash once to start receiving readings.
4. Right-click the tray icon to open Settings or unlock the widget.

If calibration is needed, wait until your in-game energy is completely full, then
use **Use current reading as maximum**. The button uses the energy at that moment;
it does not detect the maximum automatically.

## Current scope

This is an early community preview tested on the development PC. Game protocol
updates or traffic-hiding VPNs can prevent reading. Fullscreen overlay behavior
can vary by system and presentation mode.

**DPS tracking, event notifications, FPS, custom HP/MP bars, and language selection
are planned, not included in this release.**

The archive contains no personal settings, packet captures, login credentials,
or reference repository checkouts. Npcap and the game are not bundled.

No open-source license has been selected for the project code. Attribution and
the MIT license for the third-party opcode data are included in the download.
