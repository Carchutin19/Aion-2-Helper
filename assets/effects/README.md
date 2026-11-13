# Effect metadata

IDs, names and icon references: taengu/A2Tools-DPS-Meter revision e79550d878bc98759f70b63e7ba1549b1504f0db, src/data/abnormals.json and i18n/abnormals. The upstream GPL-3.0 notice is retained in LICENSE-GPL-3.0.txt for these metadata files. See sources.json for game build provenance. No upstream reader or UI implementation is copied.

Native type attributes are resolved only from explicit SkeeveAN/Aion-DPS-Meter skill_abnormal_types.json variants. Broad base-skill fallbacks and ambiguous types are not used. P retains the native passive category and is excluded from the widget, along with internal hidden IDs. Separate B/D effects triggered by passive skills remain eligible; originating skill and permanence alone do not determine visibility. Unclassified IDs are retained in research captures but do not acquire an invented polarity.

Icons are NCSOFT game content. The build caches validated official CDN PNG assets in icons.zip; no network request occurs in the overlay. Missing assets use the originating skill icon or a neutral placeholder. Arabic effect names fall back to English when absent from the game text.

The collector and renderer distinguish recipient actor, source actor, per-actor instance, effect ID, skill ID and wire expiry. Level bytes are not stack counts. Multiple matching instances display one icon with a count bounded by the native stack limit.

Controlled display exclusion: effect 200 (Dodge / Evasión urgente), observed on the own recipient during dash on 2026-11-11, is omitted from the effect widget because it is not shown as a native HUD debuff. This does not suppress movement, availability or cooldown signals. See sources.json.

Additional controlled display corrections from the 2026-11-12 recording are recorded in `display-overrides.json` and applied to `catalogue.json`. Effect 231 is an internal OnLanding test marker; 470/471 are the generic boss markers absent from the native row. The unnamed green 10018 marker is omitted only for the own recipient; its gameplay function is not invented. The blue-wing 9619 status is beneficial, while 9646 remains harmful.

The 2026-11-13 recording exposed additional variants of the same sustained abilities. The explicit 36 catalogue IDs in `display-overrides.json` now cover all six known variant groups of Light of Protection (174100000), Sprint Mantra (181600000) and Undefeated Mantra (181900000), with effect suffixes 1 and 11 per group. These suppress only the repeating renewal countdown and expiry warning. Actual received deadlines, removals and refreshes still control whether the icon exists. Light of Regeneration (170902301), despite sharing the Spanish label of Light of Protection, and other ordinary short buffs keep their real countdowns.

Lightning Speed (19005001) is beneficial, as confirmed by the exact native name/icon and user comparison. Own-only internal markers 10002, 10003 and 154100581 are omitted from the own row after the recorded native HUD comparisons. This is not a shared-icon, unnamed-effect or permanent-effect filter: Tenacity (100200001), which shares the icon of 10002/10003, stays visible. These presentation exclusions do not alter flight, dash, skill availability or cooldown signals.

Reapply the explicit metadata overrides when regenerating the catalogue; no runtime network requests, translated-name classification or per-character rules are used. Detailed evidence and scope are retained in `sources.json`.
