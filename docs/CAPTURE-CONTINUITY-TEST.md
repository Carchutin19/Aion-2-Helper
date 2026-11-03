# Capture Recovery Test C

Build **1.22.1.4-capture-test-c**. Separate testing prerelease; the normal release
remains v1.22.1. Includes the HUD-export crash correction and the earlier capture
continuity changes. Live confirmation from the affected player is still needed.

## What changed

A local session reproduced disappearing Hotbar, target bars, combat numbers and
ping. The native TCP-table query repeatedly returned inconsistent states for the
same PID-owned game socket. The old reader could interpret a reported CLOSED
state as teardown and later create a new receiver namespace. Live energy could
resume while the initial character/profile/Hotbar data was no longer available
in that namespace. An offline regression reproduces that failure path. The
reason for the native state inconsistency itself is not established.

Test C handles states 0, CLOSED and LISTEN as inconclusive only for an exact,
non-wildcard, Aion-PID-owned normal game-port tuple with a readable process
lifetime. Such input must pass the own-character profile plus matching-energy
protocol check before being dispatched. A previously verified stream keeps its
namespace through these state changes. Captured SYN/FIN/RST, process replacement,
real connection generations and existing bounded absence/idle checks still
protect against reusing another connection's data. Other ports are unchanged.

Ping can ask Windows for statistics for that exact inconclusive tuple. It still
requires successful native statistics, enabled collection and fresh RTT samples.
Changing the reported state alone no longer changes a ping connection's identity.
No synthetic ping, HP, cooldown, damage or character values are substituted.

Creating a HUD copy now handles an empty or invalid export without closing the
Helper. An empty preset gets instructions to select the populated layout, Save
and export it again. The original file remains unchanged. This does not add a
requirement to close the game before creating a copy.

Diagnostics add inconclusive-row counts and per-stream state transitions. Reports
remain optional, private and bounded to **15 minutes or 8 MiB**. No packet bodies,
IP endpoints, chat or session keys are included. Automatic hiding of Helper
widgets when game menus open is deferred and is not included.

## Keep all your settings

1. Close Aion completely and exit the Helper from its tray icon.
2. **Copy your entire existing Helper folder** to a separate test folder. Keep the
   original folder unchanged. This preserves settings, character Hotbar presets,
   pets and other local data.
3. Extract the test ZIP and copy **all of its contents into the duplicated
   folder**, replacing program files. `Aion2Helper.exe` must sit beside the
   existing `overlay-settings.json`, not inside an extra nested folder.
4. Run only the test folder's Helper, then launch Aion. Open the Helper before
   the game and enable **General → Play with diagnostics** before character login.
5. Confirm that live skills appear, then play a boss fight in the affected area.
   Note the zone, approximate times and which overlays fail or recover. Do not
   deliberately interrupt the network; note a natural ping spike if it happens.
6. Stop diagnostics after reproducing the issue or completing the comparison.
   Use **Show support report** and send that test folder's `support-report.zip`
   privately. A 2–5 minute recording around the issue is ideal; restart diagnostics
   if its limit is reached before the relevant fight.

To return to the normal version, close the test Helper and run the preserved
original folder before fully restarting Aion. Never run both Helpers together.

## Validation and remaining limits

- Twenty exact-binary regression suites pass, including native-state/protocol
  gating, ping, HUD-export/UI errors, capture continuity, Hotbar, HP/MP, combat
  numbers, stagger, targets, settings and updates.
- A cold-login regression quarantines an inconclusive socket until its real own
  profile and matching energy arrive. A 160-step state-flip regression retains
  received Hotbar/profile data and delivers damage and stagger without a gap.
- Five recorded references compare **872,135 decoded messages** with and without
  simulated missing Windows ownership observations, with matching message,
  damage and Hotbar results and no additional decoder errors. These are not raw
  replays of the affected player's metadata-only report.
- The local incident's metadata and a synthetic reproduction establish a
  concrete failure path. They do not establish why every affected PC loses data.
  The new build still needs the affected player's live comparison.
- The 16-number Floating Text capacity and TCP gap-recovery thresholds are
  unchanged. Diagnostic size/duration limits are unchanged. Missing initial
  profiles and actual missing packet contents are not reconstructed.

Implementation references: [Microsoft TCP table API](https://learn.microsoft.com/en-us/windows/win32/api/iphlpapi/nf-iphlpapi-getextendedtcptable),
[Windows TCP states](https://learn.microsoft.com/en-us/windows/win32/api/tcpmib/ns-tcpmib-mib_tcprow_owner_pid),
[native TCP RTT statistics](https://learn.microsoft.com/en-us/windows/win32/api/iphlpapi/nf-iphlpapi-getpertcpconnectionestats).
