# Support reports (v1.22.0)

Available from v1.22.0.

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
