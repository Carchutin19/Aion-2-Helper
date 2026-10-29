# Floating Combat Text — separate critical damage (v1.22.0)

Available from v1.22.0. Existing settings
continue to show critical hits with the ordinary damage style and the **!!** suffix.
Only critical flags confirmed by supported received combat events are used.

Open **Floating Text → Damage dealt → Critical hits**:

1. Enable **Separate critical damage** to customize critical text size, scale,
   color, outline color/thickness and opacity. Disabled preserves the original
   appearance. Incoming damage and received healing keep their own settings.
2. Leave **Use an independent critical widget** off to use the normal damage
   anchor, motion, lifetime and collision spacing. Criticals use the new appearance
   in that same widget; their larger glyphs participate in collision placement.
3. Enable that switch for a separate critical widget. It has its own X/Y position,
   scale, up/down movement, lifetime, travel, spread and spacing. Unlock widgets
   in General to drag it or resize it using the lower-right corner.
4. Use **Test critical hits / Stop test** for a looping simulated preview. The
   ordinary damage test also includes critical hits. Each impact appears once:
   independent criticals are removed from the normal damage widget.

The category's enable switch still controls damage dealt as a whole. Critical
preferences remain saved when the separate mode or independent widget is disabled.
New appearance/position settings support undo/redo and display-layout recovery.
