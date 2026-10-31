# Capture Recovery Test A

Testing build: **1.22.1.1-capture-test-a** (assembly1.22.1.1).
This candidate needs an affected-PC comparison before a normal release.

## What this tests

- Merge ownership when both endpoints of a TCP connection belong to Aion. The
  Windows enumeration order must not reverse the connection's interpretation.
- Use an independent, stable capture handle for the ordinary game port. A
  discovery-filter update on an auxiliary/alternate connection cannot flush the
  ordinary game handle's buffered profile, Hotbar or combat messages.
- Open the extra native handle only when an exact owned alternate socket exists.
  An extra idle worker can wait for that condition. Ordinary and alternate
  receive paths have independent buffers; this is not a claimed FPS improvement.
- Keep exact socket ownership, ordinary incoming scope, optional outgoing position
  capture, and own-profile/matching-energy verification for alternate connections.
  Dual-owned streams also require protocol proof before gameplay dispatch.
- Log capture lanes, actual filter-application times and native counter reset
  epochs. Npcap statistics can restart at a filter change, not just at startup.

The source of the flush/reset behavior was checked against Npcap1.89:
[filter installation](https://github.com/nmap/npcap/blob/v1.89/packetWin7/npf/npf/Packet.c#L1211),
[buffer and statistics reset](https://github.com/nmap/npcap/blob/v1.89/packetWin7/npf/npf/Openclos.c#L298).

## Instructions for the tester

1. Keep your normal Helper folder. Close Aion and every running Helper.
2. Extract the test ZIP into a **new folder**. Do not mix two running versions.
3. Copy **overlay-settings.json** and, if present, **hotbar-profiles.json** from
   your normal Helper folder into the test folder. Copy them; do not move/delete
   the originals. This preserves the appearance/settings used in the comparison.
4. Open the test folder's **Aion2Helper.exe** as usual, then enable
   **General → Play with diagnostics**. Open the Helper before launching Aion.
5. Start Aion and enter your character. Check that Hotbar and Floating Text are
   enabled, and check whether live skills appear before entering the activity.
6. Play a boss fight in the affected activity. If a natural ping spike happens,
   note roughly when it occurred; there is no need to induce a network failure.
7. If possible, leave for a normal zone and play another30–60seconds in the same
   recording. Record whether cooldowns, damage and stagger recover normally.
8. Disable diagnostics, wait for completion and use **Show support report**.
   Send the test folder's **support-report.zip privately**, with the zone,
   approximate time, symptoms and whether you use a VPN/routing tool.

Record for2–5minutes around the problem when possible. The existing15minute /
8MiB limits remain; restart diagnostic recording if its limit is reached.
For rollback, close the test Helper and use your retained normal folder again,
with the Helper opened before fully restarting Aion.

## Limits and validation

This does not increase the16-number display limit, invent lost packets/skills,
reuse another character's bar, or add a speculative TCP gap timeout. A genuinely
missing initial bar/profile still requires that information to be received again.
Starting the test before character entry is essential to compare clean sessions.
Not every VPN/local relay configuration or real ping-spike case is confirmed.

Local regressions cover reversed dual ownership, real offline IPv4/IPv6 filters,
native-like per-handle buffer clearing, retained fragmented login, actual HP/Hotbar/
Floating/Stagger receivers, new received character identity, ambiguous-stream
quarantine and duplicate protection. Other existing module suites run separately.
These checks do not replace the tester's game/performance comparison.

Reports remain optional and local; no packet bodies, chat, IP endpoints, session
keys or decrypted position data are recorded. Nothing is uploaded automatically.
