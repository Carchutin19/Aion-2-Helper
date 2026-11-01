# Capture Recovery Test B

Build **1.22.1.2-capture-test-b**. Separate testing prerelease; the normal release
remains v1.22.1. Test A did not resolve the affected player's boss-fight delays.

## What changed

The Helper could discard incoming packets when a Windows TCP-table observation
temporarily stopped listing a previously recognized game connection. Subsequent
packets then waited behind the missing bytes, delaying multiple overlays together.

Test B keeps an exact incoming connection through a temporary missing observation
only after receiving a valid own-character profile and matching energy on that
stream. It requires the same process incarnation, bounded idle/absence limits and
a bounded sequence window. Incoming SYN/FIN/RST and connection generations prevent
old proof/receiver state from being reused for a new session. No user key presses,
skills, character identities or missing packet contents are generated.

The reader retries Windows' growing-table response and distinguishes TCP states
instead of treating every non-ESTABLISHED row as disappearance. The independent
standard/alternate capture lanes and alternate-stream protocol checks from Test A
remain. Optional exact-distance streams follow the same connection generations.

Diagnostic schema 4 adds opaque per-flow ownership retention/rejection counters,
verification/restoration/close events and TCP-state totals. Repeated idle capture
handles use compact log entries; the final summary retains their full details.
Reports stay optional, local and bounded to 15 minutes or 8 MiB. No packet bodies,
IP endpoints, chat, account files, session keys or decrypted positions are logged.

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

Local regressions reproduce the packet rejection → restored ownership → blocked
decoder → delayed burst chain, and verify immediate delivery after this fix.
They cover process replacement, unrelated tuples, idle/hard expiry, captured
SYN/FIN/RST, duplicate SYN, received character changes, actual Hotbar/HP/Floating/
Stagger receivers and optional exact-position stream generation changes.

Five reference captures were decoded with and without simulated missing Windows
observations: **872,135 decoded messages** match, including own damage and Hotbar
observations; **29,713 packets** exercise retention, with no additional decoder
errors. These are recorded game packets with synthetic ownership observations,
not a reconstruction of the affected user's metadata-only ZIP.

The affected player's next live comparison is still required. This candidate
does not claim to fix every network loss, target-HP issue or VPN configuration.
Genuinely missing initial profiles still require received data. The 16-number
Floating Text capacity and TCP gap recovery thresholds are unchanged.

Implementation references: [Microsoft TCP table API](https://learn.microsoft.com/en-us/windows/win32/api/iphlpapi/nf-iphlpapi-getextendedtcptable),
[Windows TCP states](https://learn.microsoft.com/en-us/windows/win32/api/tcpmib/ns-tcpmib-mib_tcprow_owner_pid),
[TCP connection lifecycle and sequence rules](https://www.rfc-editor.org/rfc/rfc9293.html).
