# Optimization verification

## v1.4.0 — Notification review, 10 October 2026

Compared a local notification prototype against its optimized build on the same
Windows development PC. These are isolated tests, not a helper-on/off gameplay
FPS comparison. Both builds use the same notification appearance and settings.

| Isolated workload | Before | After |
| --- | ---: | ---: |
| 1,000,000 idle shared-timer calls | 87.72 ms | 6.44 ms |
| 200 unchanged notification-settings reloads | 638.11 ms | 0.08 ms |
| Temporary managed allocations for those 200 reloads | 45,335,832 bytes | 166,464 bytes |

Idle calls return without locking the notification queue, reading the clock or
checking shell focus. Repeated settings updates reuse the frozen preview bitmap;
only visual changes regenerate pixels. These timings can vary with scheduling
and system load. Allocation totals are temporary managed bytes, not resident RAM.
The measured hot loops have no per-call managed allocations; the 64-byte total
reported for them is measurement overhead.

All **24 notification image hashes matched**, covering three sizes, English and
Spanish, example/real text and editing/locked states. Checks also verify bounded
GDI resources over 120 resizes, stable native bitmap/presentation reuse,
independent fade/hold completion, event-driven UI delivery and enable/disable.
Two private fixtures replayed 253,679 frames and produced exactly five supported
incoming invitations. Fixtures and detailed benchmark output remain local.
The repeatable benchmark source is **tools/NotificationPerformanceHarness.cs**.

The large reduction in a preview-only hold microbenchmark includes removing
repeated native presentations of an invisible test form; it is not presented as
an equivalent in-game performance gain. Stable real notices keep their bitmap
and one timer deadline while holding; fades retain their 16 ms animation timer.
The idle animation timer remains stopped. Audio resources are closed on mute or
disable, and preview workspaces are released when Settings closes.

A separate **15.05-second whole-helper observation** after a fresh restart, with
Settings closed, Energy Bar/FPS/DPS/Notifications enabled and the game running,
measured **0.152% combined CPU** normalized to 32 logical processors. The endpoint
combined working set was **98.50 MiB** (64.29 main + 34.21 FPS worker); endpoint
private committed memory was **84.59 MiB**. These are a brief CPU interval and
memory snapshots, not a long-term leak test, stable memory plateau or notification-
only cost. No invitation occurred during this sample. Earlier 20-minute figures
below used different versions/session conditions and are not directly comparable.


## v1.2.2 — 20-minute gameplay observation, 10 October 2026

A complete 1,200-second session sampled OS process counters and the helper's
existing status file about once per second, producing 1,188 observations. The
game and helper stayed running with normal gameplay. No additional game capture,
frame trace, memory inspection or changes to settings were used during the run.

Configuration: Windows 11 Home (build 26300), 32 logical processors, Energy Bar enabled at 610 × 6
pixels with emissive glow at 100%, smoothing, fades and automatic hiding; FPS
Counter enabled at 87 × 37 pixels with its background and a **2,000 ms refresh
interval**. This is an existing application session, rather than a cold launch.
The default 250 ms FPS interval was not measured in this session.

| Measurement | Helper + FPS reader |
| --- | ---: |
| Mean CPU, normalized to the PC's total logical capacity | 0.046% |
| Highest CPU observation over an approximately one-second interval | 0.676% |
| Mean resident working set | 274.94 MiB |
| Highest observed resident working set | 275.18 MiB |
| New energy readings | 2,839 |
| Energy decoder errors during the run | 0 |
| Observations with a valid FPS reading | 1,188 / 1,188 |

Mean resident memory was 239.11 MiB for the main helper and 35.82 MiB for the FPS
reader. The combined working set stayed between 274.66 and 275.18 MiB. Comparing
the first and last minute, the combined resident mean increased by only 0.084 MiB;
private committed memory increased by 0.433 MiB. GDI objects stayed at 56 apart
from a temporary increase to 58 and returned to 56. Handles fluctuated within a
bounded range. No sustained resource growth was observed over these 20 minutes.

The energy widget was visible in 561 observations and hidden in 627. The same FPS
reader remained running throughout, with no PresentMon fallback or reader restart
observed. The sampler's own mean CPU was 0.048% and resident memory 32.9 MiB;
these are excluded from all helper figures above.

On this 32-logical-processor PC, 0.046% of total CPU corresponds to roughly 1.47%
of one logical processor. These measurements describe this configuration and
workload, rather than a guarantee for other PCs. The actual game process's CPU
and RAM counters were protected; its bootstrap process was not used as a proxy.
GPU cost was not measured. FPS values are snapshots of the existing counter,
not frame-time data or 1% lows, and there was no helper-on/off comparison.
Brief interruptions between observations cannot be excluded.

The observations do not identify an additional performance change worth making.
The existing rendering, emissive glow and text appearance remain intact. Raw
process/status samples and the local monitoring tools remain private.

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
