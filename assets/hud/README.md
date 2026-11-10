# Native HUD key-label reference

`native-key-glyphs.json` contains eight small grayscale references for the
native item-bar labels F1 through F8, with an initial search layout. It contains
no character identifiers, game connection data or screenshots.

The reference is embedded into the Helper. A local calibration is not required
for supported layouts. The detector looks for matching groups of labels, arms
only after observing the native bar, and searches again before treating its
absence as a hidden HUD. Moving the bar, including split groups around the level
icon, is supported at the tested glyph size.

Sampling and search use one bounded background worker. Images are not written
to disk, included in diagnostics or uploaded. Ambiguous/unrecognized layouts
fail open: they do not authorize automatic hiding. Other font/UI scales and
customized item-bar key labels still require validation. The full search is
bounded to 8,294,400 client pixels.
