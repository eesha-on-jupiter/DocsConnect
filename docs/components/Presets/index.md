# Presets

Value-list components that give you a curated set of string options to pick from, rather than components you wire inputs into. Each one is a standard Grasshopper `GH_ValueList` — drop it on the canvas, right-click to choose a value from its fixed list, and wire its single output into the matching input on another component. They have no Inputs and no API call of their own.

| Component | Description |
|---|---|
| [Named Style Type Preset](NamedStyleTypePreset.md) | Paragraph named-style values (`NORMAL_TEXT`, `TITLE`, `HEADING_1`, etc.) for the Named Style input. |
| [Bullet Glyph Preset](BulletGlyphPreset.md) | Bullet/numbering glyph values for the Bullet Preset input. |
| [Alignment Preset](AlignmentPreset.md) | Paragraph alignment values (`START`, `CENTER`, `END`, `JUSTIFIED`) for the Alignment input. |
| [Font Family Preset](FontFamilyPreset.md) | Curated shortcut list of common font names for Font Family inputs — not a closed enum, any installed font name can also be typed directly. |
