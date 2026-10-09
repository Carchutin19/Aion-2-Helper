# Optimization verification

## v1.2.1 — 9 October 2026

Compared against the public v1.2.0 executable on the same development PC:

| Temporary managed allocations | v1.2.0 | v1.2.1 | Reduction |
| --- | ---: | ---: | ---: |
| 100,000 presentation samples | 12,800,728 bytes | 64 bytes | >99.99% |
| 1,000 FPS images, 76 × 30 | 12,128,728 bytes | 2,152,064 bytes | 82.26% |
| 200 FPS images, 448 × 176 | 63,699,928 bytes | 449,664 bytes | 99.29% |
| Energy decoder, 30 replays | 4,810,144 bytes | 596,944 bytes | 87.59% |

The frame-sampling result measures the numeric sample collector after warm-up,
not the complete ETW worker. Its retained bounded queues are already allocated.
Drawing still creates an output bitmap when the reading or style changes; mask,
font and pixel storage is reused. These numbers describe temporary managed
allocations during work, not total RAM or native graphics memory.

All 48 FPS image hashes matched v1.2.0, covering four sizes, three readings,
background on/off and editing on/off. All 30 energy image hashes matched, and
energy render allocations remained 528,064 bytes per 1,000 images. The glow,
colors, text sharpness and animation behavior are preserved.

The private energy fixture contains 3,763 TCP segments. Each of the 30 replays
verified 79 readings and zero errors. Additional public self-tests cover stale
bytes in reused buffers, nested compressed containers, malformed output lengths
and the nesting limit.

The optimized native FPS reader passed a real-game interval change from 2,000 to
100 ms without restarting its worker. Two no-game enable/disable cycles each
used 15.625–46.875 ms of worker CPU over four measured seconds on this PC; workers exited
when disabled, and no helper FPS trace was active in standby. No quantified game
FPS improvement is claimed.

Production also filters native events to game process IDs, avoids a second
process scan on the UI thread, stops traces when the game closes, bounds failed
PresentMon trials with retry delays, and skips unchanged Settings previews.
Unchanged FPS readings and move-only updates do not upload another bitmap.
Graphics resource counts remained bounded after 1,500 changing FPS readings.

### Reproduce the FPS benchmark

Build on Windows, then compile the standalone harness with the installed .NET
Framework compiler. Supply an executable and an output JSON path:

~~~powershell
.\Build.ps1
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:exe /platform:x64 /optimize+ /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll /out:build-fps-benchmark.exe tools/FpsPerformanceHarness.cs
.\build-fps-benchmark.exe .\Aion2Helper.exe .\performance\fps-local.json
~~~

The harness also accepts an extracted v1.2.0 executable for comparison; keep its
release files together. It requires no game or traffic fixture. It warms each
case before measuring and hashes rendered pixels. Results depend on hardware,
font/runtime versions and background activity. The JSON contains no game data.

## Earlier Energy Bar optimization — 8 October 2026

The optimized build preserves the emissive filament, glow, colors, smoothing,
fades, and 1.5-second full-energy delay. Aion2Helper.exe is the current executable.

## Internal changes

- Filter incoming game packets, wait on native Npcap events, and stop capture
  when Energy Bar is disabled.
- Reuse TCP/LZ4 buffers and enforce limits on queues and decompressed output.
- Cache native images and glow calculations; opacity/position changes reuse images.
- Animate at 16 ms while changing, with 250 ms idle checks and a 1000 ms disabled
  timer. New readings wake the UI immediately.
- Discover connections and write diagnostic state off the UI thread.
- Dispose capture, timers, and GDI resources on exit.

## Development measurements

The original comparison used 30 replays of a private fixture containing 3763 TCP
segments, and 1000 emissive drawings of a 484 × 4 pixel bar.

| Temporary managed allocations | Before | After | Reduction |
| --- | ---: | ---: | ---: |
| Decoder, 30 replays | 100,644,968 bytes | 4,810,144 bytes | 95.22% |
| Rendering, 1000 images | 32,440,728 bytes | 528,064 bytes | 98.37% |

These are allocations during measured work, not equivalent reductions in total RAM.
Gen0 collections fell from 16 to 0 in decoding and from 5 to 0 in rendering.

The last decoder measurement changed from 3.762 to 0.798 ms per replay. Rendering
was 0.046 ms per image before and 0.084 ms after. The later run coincided with the
game and other checks, so timings are not an isolated CPU comparison. Production
also benefits from skipping unchanged redraws and updates.

## Validation and limits

- All 30 before/after pixel hashes matched, including size, energy, and edit mode.
- All 79 fixture readings matched the independent reference, with zero errors.
- TCP segmentation/reordering/retransmission/wraparound, packet/LZ4 bounds,
  capture filters/events, resizing, hiding, fades, smoothing, and image reuse passed.
- No GDI handle growth was observed after 2000 redraws.
- The live user session yielded at least 285 error-free readings, full recovery,
  and hiding at 100%. The user reported smoother game performance.

A 25.23-second live run used 781.25 ms of helper CPU: 3.10% of one logical processor,
or about 0.097% of the total capacity of that 32-logical-processor PC.
Peak working set was 59.45 MiB. FPS was not measured, so no quantified FPS
improvement is claimed. These measurements predate the WPF Settings redesign
and are not a RAM measurement with the newer configurator open.

Raw captures and local sample JSON stay private and are not included in this
repository. Other live samples had different connection/UI states and are not
used to calculate a gameplay CPU reduction. The historical harness in
tools/PerformanceHarness.cs requires the original local fixture; it does not run
from the public checkout alone.
