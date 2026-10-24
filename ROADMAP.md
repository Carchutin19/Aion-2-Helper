# 🧭 Aion 2 Helper — Roadmap

**Built with the community. Updated October 24, 2026.**

[Download the latest release](https://github.com/Carchutin19/Aion-2-Helper/releases/latest) · [Suggest a feature](https://github.com/Carchutin19/Aion-2-Helper/issues/new) · [Join Discord](https://discord.gg/yf7SDZMyd8)

> [!NOTE]
> The application is **Alpha**, the DPS Meter is **Beta**, and Hotbar is **experimental**.
> Available features can still have limitations. Planned features have no promised release date.

**✅ Available** · **🟡 Ongoing** · **⬜ Planned**

## ✅ Available now

| Feature | What is included | First release |
| :--- | :--- | :---: |
| **Custom HP & MP bars** | Independent live player bars; adjustable size, position, visibility, text, colors, glow and outlines. | [v1.15.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.15.0) |
| **DPS text scaling** | Independent scaling for meter names, values, headings and tabs. | [v1.16.2](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.16.2) |
| **Selection outline control** | Optional control for the game's selection outline through Game Tweaks. | [v1.16.2](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.16.2) |
| **Pet tracking & offline atlas** | Login collection sync, searchable registry, saved character/server records, Completed / MAX status, soul cards, pins and recorded map locations. | [v1.17.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.17.0) |
| **Spacetime Rift alerts** | Scheduled advance, opening and closing alerts, entry countdown and time-zone conversion. **Schedule-based, not live portal detection.** | [v1.17.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.17.0) |
| **Gathering / Crafting overlay** | Resource names, supported types and a customizable three-segment remaining-use bar, based on supported profiles and your own interactions. | [v1.18.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.18.0) |
| **Rounded overlay blur** | Cleaner pet soul-card backdrop and optional dark DPS blur. Ordinary transparency remains the fallback on unsupported systems. | [v1.18.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.18.0) |
| **Custom skill Hotbar** | Detected icons, received cooldowns and supported reductions/resets; group/individual layouts, snap, hidden slots, character/server presets, looping preview and optional native skill-bar hiding. **Experimental.** [Setup guide](docs/HOTBAR.md). | [v1.19.0](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.19.0) |

These highlights include community requests. Other existing modules and complete changes are documented in the [README](README.md) and [release notes](RELEASE-NOTES.md).

## 🛠️ Fixes & reliability

Community bug reports are tracked here alongside new features. A shipped change
is described separately from cases that still need more testing.

| Status | Report / correction | Update |
| :--- | :--- | :---: |
| ✅ Confirmed | **Hotbar not populating on an affected PC:** corrected a supported assigned-skill record being rejected. Live skills and cooldowns were confirmed by the community tester. | [v1.19.1](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.19.1) |
| ✅ Confirmed | **Original hotbar hiding did not persist:** replaced automatic saved-settings edits with an original-preserving exported HUD copy. Import/select it in game, **Save and leave the editor**. The game can stay open; the companion confirmed this workflow works. [Guide](docs/HOTBAR.md). | [v1.19.1](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.19.1) |
| ✅ Reviewed | **Crowded Hotbar/Pets Widget settings:** clearer section spacing, aligned controls and numbered HUD instructions. Collection and Atlas layouts are retained. | [v1.19.1](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.19.1) |
| 🟡 Change shipped; feedback welcome | **Tray menu closing too quickly:** switched to a reusable native context menu. Local command/lifetime checks passed; further Windows hidden-icon-panel confirmation is welcome. | [v1.19.1](https://github.com/Carchutin19/Aion-2-Helper/releases/tag/v1.19.1) |
| 🟡 Investigating | **HP/MP gaps during some map transitions:** recovery still depends on supported fresh profile data. | Ongoing |
| 🟡 Investigating | **World-boss party damage stops updating / incomplete combat coverage:** DPS remains Beta; totals and rankings can be incomplete. | Ongoing |

For a new problem, [report an issue](https://github.com/Carchutin19/Aion-2-Helper/issues/new)
with the Helper version, what happened and steps to reproduce it. Please keep
personal logs and HUD exports out of public posts unless their contents have
been reviewed.

## 🟡 Ongoing work

| Area | Current focus |
| :--- | :--- |
| **FPS & GPU optimization** | Reuse drawing/composition resources, reduce unnecessary updates and continue validation under actual game load. Caching improvements are already included; this work is not marked complete or presented as a guaranteed FPS increase. |
| **Hotbar compatibility & validation** | Validate more classes, charges/reloads, shared cooldown groups and contextual skill variants. Complete the automatic character-switch live comparison and validate further client/HUD variants of the new export/import workflow. |
| **Data coverage & reliability** | Continue investigating HP/MP loading gaps, unknown special targets and partial combat coverage, including the existing world-boss party-damage limitation. |

## ⬜ Planned features

| Feature | Intended scope |
| :--- | :--- |
| **Player interaction menu** | A custom action menu for selected friendly or neutral players, with convenient access to supported interactions. |
| **World-boss tracker** | Supported active-boss information, locations, timers based on detection time and optional notifications. |
| **Custom cast bar** | A customizable casting-progress overlay with adjustable information, appearance, size and position. This is separate from Hotbar cooldowns. |
| **Custom NPC nameplates** | Adjustable NPC nameplates to improve readability and control the information displayed. |
| **Special chest radar** | Explore reliable detection and location information for special chests. |
| **Controller support** | A system for integrating controller input with the Helper. |

The list is **not a development order**. Feasibility depends on what can be reliably read or measured.

<details>
<summary><strong>Validation notes and current limits</strong></summary>

- The core Hotbar and tested cooldown/chain cases were compared locally. A community tester confirmed the missing-bar correction and the new HUD export/import/save workflow. The full automatic character-switch live comparison remains pending; this does not establish universal class/client support.
- Charge/reload skills, unmapped shared cooldown groups and other contextual variants are not universally supported. Keyboard labels are manual.
- Rift alerts follow a schedule; the first local event-time comparison remains pending.
- Atlas markers are reference locations, not live pet spawns.
- Gathering supports the tested three-use rule for compatible profiles; it does not claim a universal maximum field or complete exact subtype coverage.
- Some HP/MP map-loading gaps and special targets without verified names remain. DPS is incomplete and stays in Beta.

See each release's notes for the evidence and limits of that build.

</details>

## ❤️ How priorities are chosen

Community requests take priority over personal roadmap ideas whenever possible. Development order depends on available time, complexity and reliable implementation.

Have an idea? [Open a feature request](https://github.com/Carchutin19/Aion-2-Helper/issues/new) or share it in [Discord](https://discord.gg/yf7SDZMyd8). Feedback and bug reports help decide what comes next.
