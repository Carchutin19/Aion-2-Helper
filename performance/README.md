# Optimization verification — 8 October 2026

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
