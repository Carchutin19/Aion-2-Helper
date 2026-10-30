# Support reports (v1.22.1)

Available from v1.22.0; expanded diagnostics from v1.22.1.

1. Open **General → Support & diagnostics** and enable **Play with diagnostics**.
2. Reproduce the problem. For a connection/login issue, enable this first, fully
   close Aion, and then launch it again with the Helper already open.
3. Turn the option off and wait for the report to finish. Exiting the Helper
   through its tray menu also finalizes an active report.
4. Use **Show support report**. Send **support-report.zip**, from beside
   Aion2Helper.exe, privately to the maintainer. Describe the problem, zone,
   Helper version, and the steps you followed.

Recording is off by default. When enabled, its preference is saved, so check it
again before your next session. Each recording is limited to 15 minutes or 8 MiB
of log data. Turn the option off and on for a new recording. One previous ZIP is
kept as support-report.previous.zip. Settings are preserved.

The ZIP contains support-log.jsonl and support-summary.json: version, driver,
connection/packet counters, protocol progress and selected detection/widget state.
Character/server identifiers and basic device metadata can appear. Packet bodies,
chat, IP endpoints, decrypted position data and session keys are not logged.
No report is uploaded automatically. Do not post personal reports publicly.

The recorder writes on a background timer; ZIP snapshots are refreshed at most
once per 30 seconds during recording and finalized on stop. With recording off,
there are no diagnostic file writes or diagnostic timers. Ordinary live-status.json
is a separate existing status file.

If the folder is not writable, the UI reports an error and the Helper attempts to
write support-write-error.txt. Send that file, if present, instead of an old ZIP.

## Lag, disappearing overlays or missing combat numbers

Use v1.22.1 or newer. Enable diagnostics before the affected activity. Reproduce
it for roughly 2–5 minutes, then, if practical, leave for a normal zone and play
for another 30–60 seconds in the same recording. Stop diagnostics and send the
ZIP privately. Include the approximate time of the problem, the zone, what
vanished or arrived late, and whether it recovered outside the zone. Mention
whether you use a VPN/routing tool. Do not change several settings during the
comparison, and do not use the visual Test buttons as evidence of live data.

Most detector and visual counters are cumulative for the Helper session; compare
the first and last recording samples rather than attributing their full totals
to the recorded incident. The expanded report includes:

- Capture-driver received/drop counters when supported, actual filter application
  counts, and local packet-processing timings. Driver counters cover the handle's
  lifetime; compare consecutive samples, not an absolute total against the log's
  shorter duration. Unsupported counters are reported as unavailable/errors.
- Capture-timestamp age, separate from processing cost. Clock drift can affect
  this age: it is not a direct measurement of server or internet latency.
- Per-flow pending-byte/segment counts, gap age and distance, duplicates,
  out-of-order arrivals, framing/container errors and existing recovery reasons.
  Local numeric flow IDs correlate the active Hotbar with its decoder stream
  without exposing socket endpoints. These IDs are scoped to one recording.
- Floating Text peak active count, configured capacity, layout dimensions and
  relevant text/movement settings; separate counters for capacity evictions,
  placement failures/evictions and normal expiry; diagnostic render timings.
- Stagger phase, sample/traffic age, confirmed-target state, visibility reason
  and screen-effect trigger counters, correlated with the same local flow IDs.
- Helper memory, cumulative CPU time, handles, thread count and GC collection
  counts. This is not GPU usage, game FPS or a memory dump.

A visual eviction does **not** mean the PC ran out of RAM or that a combat hit
was never decoded. The current bounded display retains at most 16 active numbers
per group and can retire older ones if no readable placement remains. This
update measures those cases separately; it does not increase the limit, aggregate
hits or delay numbers to make a burst appear complete.

The Conquest report remains under investigation. These diagnostics do not by
themselves identify the exact missing packet or prove a final fix. Packet bodies,
chat, IP endpoints, session keys and decrypted position data remain excluded.
Detailed native/runtime/render sampling is active only while diagnostics record;
there is no new always-running diagnostic timer or automatic report upload.

Capture-counter semantics: [Npcap API reference](https://npcap.com/guide/npcap-api.html).
