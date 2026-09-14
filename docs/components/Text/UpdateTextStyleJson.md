# Styled Text

Inserts one or more lines of text and applies any combination of style traits — bold, italic, underline, strikethrough, font size, font family, text color, highlight, link, and baseline offset — as `insertText` + `updateTextStyle` request pairs.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Text | string (list) | Yes | — | Text to insert and style. A list places each entry on its own line (unless Same Paragraph is on). |
| Bold | bool | No | false | Bold — only applied if this input is connected; leaving it unconnected leaves bold untouched rather than forcing it off. |
| Italic | bool | No | false | Italic — same connect-to-apply rule as Bold. |
| Underline | bool | No | false | Underline — same connect-to-apply rule as Bold. |
| Strikethrough | bool | No | false | Strikethrough — same connect-to-apply rule as Bold. |
| Font Size | double (list) | No | — | Font size in points, index-matched to Text (a shorter list repeats its last value). A value ≤0 is skipped for that line. |
| Font Family | string (list) | No | — | Font family name, index-matched to Text (a shorter list repeats its last value). Blank is skipped for that line. |
| Text Color | string (list) | No | — | Foreground color as `#RRGGBB` hex or `R,G,B` (0-255), index-matched to Text (a shorter list repeats its last value). Blank is skipped; an unparseable value is skipped with a runtime warning. |
| Highlight | string (list) | No | — | Background highlight color, same format and index-matching rules as Text Color. |
| Link URL | string (list) | No | — | Hyperlink URL, index-matched to Text (a shorter list repeats its last value). Blank is skipped for that line. |
| Baseline | string (list) | No | — | Baseline offset (SUPERSCRIPT or SUBSCRIPT), index-matched to Text (a shorter list repeats its last value). Blank is skipped for that line. |
| Segment ID | string | No | "" | Optional, blank = document body, or wire in a Header ID / Footer ID / Footnote ID from the Document tab to target that segment instead. |
| Same Paragraph | bool | No | false | False (default): each Text item lands in its own paragraph. True: all Text items join into ONE paragraph (joined by Separator), each keeping its own per-item style — e.g. a font-size wave or color gradient running across one sentence. |
| Separator | string | No | "" | Only used when Same Paragraph is true. Inserted between joined items — `""` for letter-by-letter, `" "` for word-by-word. |
| Request JSON | string (list) | No | — | Optional. Wire another Text- or Paragraph-tab component's Request JSON output in to add this trait to that block's existing text instead of inserting new text. Text is ignored while this is connected, and Segment ID is inherited from the incoming block. See [Chaining styles](../../chaining.md). |

## Outputs

| Name | Type | Description |
|---|---|---|
| Request JSON | string (list) | The `insertText` and `updateTextStyle` Request JSON objects for this text. |

## Output

This component never calls the API directly — it emits Request JSON (a Google Docs `Request` object, or a list of them) that goes into [Request Aggregator](../Combine/RequestAggregator.md)'s `Fixed Requests` input (it already carries its own indices, so it must not be re-placed) before [Batch Update](../Document/BatchUpdateDocument.md) can push it live.

## Notes

This component is self-contained: feed it text and any combination of the style inputs, and it builds the insert plus the matching style update together, with no index to manage. Use it whenever more than one style trait needs to land on the same run of text — the atomic Bold/Italic/Underline/etc. components each insert their own separate copy of the text, so wiring the same text into two of them inserts it twice, not once with two traits applied.

Bold, Italic, Underline, and Strikethrough are toggles that only take effect when their input is actually **connected** — an unconnected input leaves that trait untouched (not forced off), while explicitly wiring in `false` clears that trait. This lets you deliberately un-set a style rather than merely skip setting it.

Font Size, Font Family, Text Color, Highlight, Link URL, and Baseline are all index-matched lists, matched against Text the same way Text Color always was: feed a list alongside a list of Text to give each line its own value, and a shorter list repeats its last entry for the remaining lines. Bold, Italic, Underline, and Strikethrough are not per-line — they apply uniformly to every line in the run.

Turning on **Same Paragraph** changes how multiple Text items combine: instead of one paragraph per item, all items are concatenated into a single paragraph (joined by Separator), with each item's own trait values still applied to just its own span. This is the mechanism for a font-size wave or a color gradient across a single sentence — wire a Sine/Graph Mapper sampled per item into Font Size, or a Gradient into Text Color, with Same Paragraph on and each Text item holding one word or character.

Leave Segment ID blank to target the main document body. To target a header, footer, or footnote instead, wire the corresponding Header ID, Footer ID, or Footnote ID (produced by the Document-tab components that create those segments) into Segment ID.

## Related

- [Bold Text](BoldTextJson.md)
- [Italic Text](ItalicTextJson.md)
- [Underline Text](UnderlineTextJson.md)
- [Strikethrough Text](StrikethroughTextJson.md)
- [Font Size Text](FontSizeTextJson.md)
- [Font Family Text](FontFamilyTextJson.md)
- [Text Color Text](TextColorTextJson.md)
- [Highlight Text](HighlightTextJson.md)
- [Link Text](LinkTextJson.md)
- [Baseline Text](BaselineTextJson.md)
- [Font Family preset](../Presets/FontFamilyPreset.md) — feeds the Font Family input.
