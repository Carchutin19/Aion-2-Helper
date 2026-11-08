# 🧭 Aion 2 Helper — Roadmap

**Built with the community. Updated November 8, 2026.**

[Download the latest release](https://github.com/Carchutin19/Aion-2-Helper/releases/latest) · [Suggest a feature](https://github.com/Carchutin19/Aion-2-Helper/issues/new) · [Join Discord](https://discord.gg/yf7SDZMyd8)

> [!NOTE]
> The application is **Alpha**, the DPS Meter is **Beta**, and Hotbar is **experimental**.
> Available features can still have limitations. Planned features have no promised release date.

**✅ Available** · **🟡 Ongoing / In testing** · **⬜ Planned**

> [!TIP]
> **v1.22.0: VPN / ExitLag connection fix released.** Missing character/overlay detection was corrected for the affected ExitLag tester. Other VPN and routing configurations still need validation. [Release details](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.22.0).

## ✅ Available now

| Feature | What is included | First release |
| :--- | :--- | :---: |
| **Widget alignment and grid** | Global edge and center snap, independent Hotbar slot snap, and a grid for each screen with highlighted center axes and adjustable opacity. Available while widgets are unlocked. | [v1.26.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.26.0) |
| **Foreground-only overlays** | Optional setting in General. Locked overlays hide when Aion loses focus. Unlock widgets to edit their layout outside the game. | [v1.25.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.25.0) |
| **VPN / ExitLag connection recovery** | Restores detection for verified alternate game connections. Confirmed on the affected ExitLag setup; other VPN/routing configurations remain under validation. | [v1.22.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.22.0) |
| **Custom HP & MP bars** | Independent live player bars; adjustable size, position, visibility, text, colors, glow and outlines. | [v1.15.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.15.0) |
| **DPS text scaling** | Independent scaling for meter names, values, headings and tabs. | [v1.16.2](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.16.2) |
| **Selection outline control** | Optional control for the game's selection outline through Game Tweaks. | [v1.16.2](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.16.2) |
| **Pet tracking & offline atlas** | Login collection sync, searchable registry, saved character/server records, Completed / MAX status, soul cards, pins and recorded map locations. | [v1.17.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.17.0) |
| **Spacetime Rift alerts** | Scheduled advance, opening and closing alerts, entry countdown and time-zone conversion. **Schedule-based, not live portal detection.** | [v1.17.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.17.0) |
| **Gathering / Crafting overlay** | Resource names, supported types and a customizable three-segment remaining-use bar, based on supported profiles and your own interactions. | [v1.18.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.18.0) |
| **Rounded overlay blur** | Cleaner pet soul-card backdrop and optional dark DPS blur. Ordinary transparency remains the fallback on unsupported systems. | [v1.18.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.18.0) |
| **Staggered screen effect** | Optional centered text, broken ring and glow; separate text/effect colors and Test effect under Progress bar > Stagger. Disabled by default. | [v1.20.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.20.0) |
| **Our target & Target of target** | Supported NPC/player HP and focus, with four color palettes sharing one layout. Neutral-NPC and PvP classification has limited confirmed coverage. | [v1.20.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.20.0) |
| **Opt-in support reports** | General records a bounded local support ZIP; v1.22.1 adds capture losses, stream-gap ages, visual eviction reasons and Helper resource counters. Disabled by default; no automatic upload. [Guide](https://github.com/Carchutin19/Aion-2-Helper/blob/main/docs/SUPPORT.md). | [v1.22.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.22.0) |
| **Separate critical damage** | Own critical text styling, shared damage position or an independent movable widget, looping preview and immediate edit-guide hiding on relock. | [v1.22.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.22.0) |
| **Additional interface languages** | German, French and Arabic primary controls, Arabic RTL settings and English fallback for untranslated advanced help. | [v1.22.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.22.0) |
| **Floating Combat Text** | Independent damage dealt/taken and received healing; text/outline colors, placement, scale, fade, up/down movement, duration, adjustable horizontal/vertical spacing from zero, preview and confirmed-critical **!!**. Supported combat events only. | [v1.21.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.21.0) |
| **Custom skill Hotbar** | Detected icons, received cooldowns and supported reductions/resets; group/individual layouts, snap, hidden slots, character/server presets, looping preview and optional native skill-bar hiding. **Experimental.** [Setup guide](docs/HOTBAR.md). | [v1.19.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.19.0) |

These highlights include community requests. Other existing modules and complete changes are documented in the [README](README.md) and [release notes](RELEASE-NOTES.md).

## 🛠️ Fixes & reliability

Community bug reports are tracked here alongside new features. A shipped change
is described separately from cases that still need more testing.

| Status | Report / correction | Update |
| :--- | :--- | :---: |
| ✅ Confirmed in tested cases | **Switching characters could leave the Hotbar empty or retain old Energy data.** The Helper resets the previous character's bar and values, restores the incoming Hotbar preset, and refreshes Energy, Combat Power and XP from the incoming character. | [v1.26.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.26.0) |
| ✅ Released | **Target distance can be missing or intermittent.** Added support for another connection setup and an approximate fallback from recent confirmed server positions. Exact mode still depends on connection setup and fresh readings. | [v1.26.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.26.0) |
| ✅ Released | **Flight Energy missing after login:** reads the current value and maximum from the own-character profile, then applies live changes. The 2,500 → 3,500 wings change and subsequent full recharge were checked in game. | [v1.25.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.25.0) |
| 🟡 Correction released · live retest pending | **Hidden Cube Radar shows distance without a compass:** accumulates short movement samples, retains the last verified direction and now accepts an additional player-position update. The radar remains in testing. | [v1.26.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.26.0) |
| 🟡 Correction released · live retest pending | **New Hotbar skill delayed inside an instance:** the Helper now applies supported changes to individual slots after receiving the full bar. The Illusory Clone unlock inside an instance still needs a dedicated live retest. | [v1.26.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.26.0) |
| ✅ Released | **Hotbar on new characters and when re-enabled:** checked the initial assignments, a level 3 skill unlock and the switch between skill presets 1 and 2. Re-enabling the Helper Hotbar restores the detected bar and preserves cooldown tracking. | [v1.23.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.23.0) |
| ✅ Confirmed in tested cases | **Assassin Q/E icons stuck on the base skill:** Q follows the verified stun trigger for **Shadow Fall**. E follows the verified critical-hit and evasion triggers for **Heart Gore** and **Whirlwind Slice**. The corrected build was confirmed in game and picked the expected icon before all 14 recorded casts in replay. Priority when two E variants are ready together still needs a live check. | [v1.23.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.23.0) |
| ✅ Confirmed in tested cases | **Enemy HP missing without target lock:** the health bar appears when landing a supported attack without locking the enemy. Confirmed in game with a normal enemy. | [v1.23.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.23.0) |
| ✅ Confirmed on affected PC | **VPN / ExitLag — missing character and overlay detection:** the affected tester confirmed restored detection. Verified owned-flow routing is integrated; other routing tools/configurations still need validation. | [v1.22.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.22.0) |
| 🟡 Correction released · feedback welcome | **Conquest / boss delays and disappearing overlays:** a local session reproduced shared Hotbar/target/combat-number loss and inconsistent native socket states. v1.22.2 preserves verified input through inconclusive state observations, improves ping continuity and adds diagnostics. The affected-player comparison is still required; dense-number capacity is a separate pending issue. | [v1.22.2](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.22.2) |
| 🟡 Correction released · regression checked | **Helper closes after choosing an empty or invalid HUD export:** the export error is handled; an empty preset explains how to export the populated layout. Original HUD files remain unchanged. | [v1.22.2](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.22.2) |
| 🟡 Local regression checks passed | **Redundant capture-filter changes:** web and already-covered standard-port socket churn no longer reinstall an equivalent filter. Exact alternate-socket ownership and optional position capture remain enforced. Conquest impact awaits feedback. | [v1.22.1](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.22.1) |
| ✅ Confirmed in tested cases | **HP/level bar on manufacturing stations:** reproduced on four professions and corrected with explicit workstation IDs. Recorded merchant controls retain their data; the user confirmed the fix in game. | [v1.22.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.22.0) |
| ✅ Confirmed | **Hotbar not populating on an affected PC:** corrected a supported assigned-skill record being rejected. Live skills and cooldowns were confirmed by the community tester. | [v1.19.1](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.19.1) |
| ✅ Confirmed | **Original hotbar hiding did not persist:** replaced automatic saved-settings edits with an original-preserving exported HUD copy. Import/select it in game, **Save and leave the editor**. The game can stay open; the companion confirmed this workflow works. [Guide](docs/HOTBAR.md). | [v1.19.1](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.19.1) |
| ✅ Reviewed | **Crowded Hotbar/Pets Widget settings:** clearer section spacing, aligned controls and numbered HUD instructions. Collection and Atlas layouts are retained. | [v1.19.1](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.19.1) |
| ✅ Released | **Windows hidden-icons menu closes on right-click:** uses the native notification-area popup with the Helper's dark styling and existing commands. | [v1.25.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.25.0) |
| ✅ Confirmed in tested zones | **Abyss / Pesadilla recovery:** live entry/exit tests confirmed Pesadilla Hotbar recovery and Abyss HP/MP, focus and own pet soul notices. Other zone profiles remain under validation. | [v1.20.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.20.0) |
| ✅ Confirmed in tested cases | **Selected-player HP and target of target:** neutral players and normal 1v1 rivals now use received HP/focus. Neutral-player and 1v1 colors were visually confirmed. Hostile Abyss-player colors and other PvP/party modes remain unvalidated. | [v1.20.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.20.0) |
| ✅ Reproduction corrected | **NPC bars disappearing in crowded cities:** independent bounded NPC/player caches prevent player updates evicting stationary NPCs. Exact recorded comparison: 3/12 missing warehouse selections to 0/12; short live bars/colors check passed. Neutral NPC palette covers confirmed profiles only. | [v1.20.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.20.0) |
| 🟡 Setup documented; discovery can take time | **Exact distance after restarting Helper:** open Helper before fully launching Aion; character selection alone may miss setup. Correct startup confirmed working; no startup-speed improvement is claimed. | [v1.20.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.20.0) |
| 🟡 Correction shipped; live comparison pending | **Hotbar hidden by capture-clock drift:** local receipt time avoids rejecting a valid bar when capture timestamps lead the UI clock. Duration, true stale and reset regression checks pass. | [v1.21.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.21.0) |
| 🟡 Correction shipped; feedback welcome | **Floating Text absent until instance entry / jerky motion:** identity can be retained/recovered on late enable; stable trajectories, cached cropped drawing and active-only pacing replace repeated positioning and drawing. Earlier numbers appeared in game; final animation revision awaits live comparison. | [v1.21.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.21.0) |
| ✅ Confirmed | **Floating Text references persist after locking:** edit guides now hide immediately, including independent criticals, without pressing Done. User confirmed in game. | [v1.22.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.22.0) |
| 🟡 Investigating | **HP/MP gaps during some map transitions:** recovery still depends on supported fresh profile data. | Ongoing |
| 🟡 Investigating | **World-boss party damage stops updating / incomplete combat coverage:** DPS remains Beta; totals and rankings can be incomplete. | Ongoing |

For a new problem, [report an issue](https://github.com/Carchutin19/Aion-2-Helper/issues/new)
with the Helper version, what happened and steps to reproduce it. Please keep
personal logs and HUD exports out of public posts unless their contents have
been reviewed.

## 🟡 Ongoing work

| Area | Current focus |
| :--- | :--- |
| **FPS & GPU optimization** | [v1.24.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.24.0) adds **Game Tweaks → Performance**, with a 60-second frame pacing test, an in-game countdown, the two latest results and a reversible FPS limit that preserves Visuals settings. The effect on frame pacing depends on the game and hardware. Overlay drawing and composition optimization continues separately. |
| **Hotbar compatibility & validation** | Validate more classes, charges/reloads, shared cooldown groups and contextual skill variants. Retest skill unlocks inside instances and validate further client/HUD variants of the export/import workflow. |
| **Language coverage & review** | German, French and Arabic are available from v1.22.0. Continue translating advanced help that currently falls back to English and reviewing wording/layout. |
| **Floating Combat Text validation** | Available in v1.21.0. Continue live smoothness checks and expand supported damage/healing event coverage; validate more characters and dense bursts. |
| **Data coverage & reliability** | Continue investigating HP/MP loading gaps, unknown special targets and partial combat coverage, including the existing world-boss party-damage limitation. |

## 💡 Community requests

Requests from players are kept separate from the personal feature roadmap.
Released community features and corrections are listed above.

| Status | Request | Intended scope |
| :--- | :--- | :--- |
| ✅ Available | **Staggered screen effect** | Optional centered animation, text/glow colors and Test effect; introduced in [v1.20.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.20.0). Preview approved and automatic-transition tests pass; separate controlled live trigger comparison remains pending. |
| ⬜ Planned / feasibility to check | **Hide Helper overlays while game menus are open** | Explore reliable detection of the map, shops, manufacturing stations and other menus, then restore overlays on close. In-game menu detection remains a separate investigation from the released foreground-only setting. |
| ⬜ Planned / feasibility to check | **Enemy status-effect visual removal** | Explore an option to hide visual effects associated with altered states/debuffs on enemies. Exact scope and compatibility need validation; this is separate from the existing selection-outline option. |
| ✅ Available in v1.24.0 | **Real-time combat log** | Displays supported outgoing hits with configurable skill icons, names, damage and hit labels. New entries fade in and are briefly outlined. Includes configurable sizing, opacity, blur, inactivity timeout, entry order and scrollable recent history. Healing coverage remains planned. |
| ✅ Available in v1.24.0 | **Estimated fight duration in the DPS Meter** | Shows an estimate beside elapsed time using recent HP loss. With multiple enemies, it prioritizes a single known boss or uses the locked target. Pauses or insufficient readings show a dash when there is a known subject. |
| ✅ Available in [v1.26.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.26.0) | **Snap alignment for overlay widgets** | Align nearby widget edges and centers while editing. The optional per-screen grid has stronger center lines and adjustable opacity. The Hotbar keeps its own slot snap and spacing controls. |
| ✅ Available in v1.24.0 | **Hit direction in Floating Combat Text** | Optional Front, Back, Critical, Perfect and Double labels from supported combat events. Label appearance is adjustable, with separate controls for independently styled critical hits. |
| 🟡 Ongoing | **FPS & GPU optimization** | A 60-second frame pacing test and reversible FPS limit are ready for comparison. See ongoing work above for validation status. |
| 🟡 In testing | **Special chest radar / Hidden Cube Radar** | Detects supported hidden cubes in Verteron and fades the overlay in on detection. Live testing confirmed the alert. Distance, elevation and the arrow depend on confirmed player coordinates and direction of travel. [v1.26.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.26.0) adds another position update. The compass still needs a live check at another cube. |
| ⬜ Planned | **Controller support** | A system for integrating controller input with the Helper. |

## Personal roadmap

### ✅ Released in [v1.26.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.26.0) · November 8, 2026

| Feature | What changed and what was checked |
| :--- | :--- |
| **Hotbar feedback** | Icon slides on skill changes, dimming and a green ready pulse for supported contextual skills, plus optional yellow feedback for key presses and confirmed casts. Live testing included the basic combo and Aion's built-in macros. |

### ✅ Released in [v1.25.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.25.0) · November 6, 2026

| Feature | What changed and what was checked |
| :--- | :--- |
| **Flight Energy bar** | Customizable bar under **Progress bar**, with an option to show it only during recovery and a configurable full-bar delay and fade. Flight, landing, recovery and the wings capacity change were checked in game. |

### ✅ Released in [v1.23.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.23.0) · November 4, 2026

| Feature | What changed and what was checked |
| :--- | :--- |
| **Combat Power overlay** | Added an FPS/Ping-style widget with white text and sword icons by default, configurable color, size and position. Click the locked widget to switch between the two values. Both values appeared in live testing, including after equipment changes. |
| **Experience progress bar** | Added a customizable bar under **Progress bar**, with level and XP display options, optional fade after a configurable period without gains and an option to hide at maximum level. Tested level-ups and XP gains from enemies after the tutorial. |

### ⬜ Planned

| Feature | Intended scope |
| :--- | :--- |
| **Player interaction menu** | A custom action menu for selected friendly or neutral players, with convenient access to supported interactions. |
| **World-boss tracker** | Supported active-boss information, locations, timers based on detection time and optional notifications. |
| **Custom cast bar** | A customizable casting-progress overlay with adjustable information, appearance, size and position. This is separate from Hotbar cooldowns. |
| **Custom NPC nameplates** | Adjustable NPC nameplates to improve readability and control the information displayed. |

The list is **not a development order**. Feasibility depends on what can be reliably read or measured.

<details>
<summary><strong>Validation notes and current limits</strong></summary>

- The core Hotbar and tested cooldown/chain cases were compared locally. A community tester confirmed the missing-bar correction and the new HUD export/import/save workflow. Character-switch recovery was checked locally. Other class and client combinations remain under validation.
- Charge/reload skills, unmapped shared cooldown groups and other contextual variants are not universally supported. Keyboard labels are manual.
- Rift alerts follow a schedule; the first local event-time comparison remains pending.
- Atlas markers are reference locations, not live pet spawns.
- Gathering supports the tested three-use rule for compatible profiles; it does not claim a universal maximum field or complete exact subtype coverage.
- Neutral NPC colors use confirmed template/map pairs, not a generic allegiance flag. Normal 1v1 and neutral-player colors have live confirmation; other PvP and party cases remain limited.
- Staggered is available; its preview was visually approved and received-state trigger logic passed isolated tests. A dedicated live trigger comparison remains pending.
- Some HP/MP map-loading gaps and special targets without verified names remain. DPS is incomplete and stays in Beta.

See each release's notes for the evidence and limits of that build.

</details>

## ❤️ How priorities are chosen

Community requests take priority over personal roadmap ideas whenever possible. Development order depends on available time, complexity and reliable implementation.

Have an idea? [Open a feature request](https://github.com/Carchutin19/Aion-2-Helper/issues/new) or share it in [Discord](https://discord.gg/yf7SDZMyd8). Feedback and bug reports help decide what comes next.
