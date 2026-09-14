# Font Family Preset

A value-list preset of commonly used font names, for use with Font Family inputs.

## Values

- Arial
- Calibri
- Times New Roman
- Georgia
- Verdana
- Courier New
- Garamond
- Trebuchet MS
- Roboto
- Open Sans

## Feeds into

- [Styled Text](../Text/UpdateTextStyleJson.md)'s Font Family input
- [Font Family Text](../Text/FontFamilyTextJson.md)'s Font Family input

## Notes

Unlike the other presets, Font Family is not a closed API enum — it is not validation. Font Family inputs remain plain free-text, and any installed font name can be typed directly without going through this list at all. This preset is only a shortcut for common choices; picking a value outside this list is not an error.
