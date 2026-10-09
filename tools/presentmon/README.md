# PresentMon console dependency

Aion 2 Helper uses the unmodified **PresentMon 2.6.0 x64 console** from Intel's
[official release](https://github.com/GameTechDev/PresentMon/releases/tag/v2.6.0).
The FPS module starts this executable only as a fallback when its native DXGI
reader has no samples. Disabling FPS stops all of its own ETW sessions.
No PresentMon service or installer is required.

Run `Download-Dependencies.ps1` from the project to download it. The binary is not
committed to Git. Release packages include the verified binary and both license
files in this directory.

SHA-256 of `PresentMon-2.6.0-x64.exe` (renamed locally to `PresentMon.exe`):

`b2a706bc6ad475749e3b7e3409263aa1e6906d45bdcf993f6dbc0f660188f1af`

The binary has a valid Authenticode signature from Intel Corporation. Original
licenses: `LICENSE.txt` and `THIRD_PARTY.txt`. These apply to this dependency,
independently of Aion 2 Helper's code license.

The FPS reader requests only presentation timing, with GPU, display, and input
tracking disabled. It listens for `Aion2.exe`, including game launches after the helper starts, and uses a bounded
one-second window for the dominant swap chain. The number represents
game-rendered FPS; driver-generated frames are not added.
