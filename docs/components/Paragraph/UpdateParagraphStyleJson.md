# Styled Paragraph

Inserts one or more paragraphs and applies any combination of named style, alignment, line spacing, indent, and spacing to each, producing `insertText` requests followed by `updateParagraphStyle` requests that cover whichever traits are set.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Text | string (list) | Yes | — | Text to insert. A list places each entry in its own paragraph. |
| Named Style | string (list) | No | "" | Named paragraph style (e.g. `HEADING_1`, `NORMAL_TEXT`), index-matched to Text (a shorter list repeats its last value). Leave blank to skip this trait. |
| Alignment | string (list) | No | "" | Paragraph alignment value, index-matched to Text (a shorter list repeats its last value). Leave blank to skip this trait. |
| Line Spacing | double (list) | No | 0.0 | Line spacing percentage, index-matched to Text (a shorter list repeats its last value). Leave at 0 to skip this trait. |
| Indent | double (list) | No | 0.0 | Start indent, index-matched to Text (a shorter list repeats its last value). Leave at 0 to skip this trait. |
| Space Above | double (list) | No | 0.0 | Space above the paragraph, index-matched to Text (a shorter list repeats its last value). Leave at 0 to skip this trait. |
| Space Below | double (list) | No | 0.0 | Space below the paragraph, index-matched to Text (a shorter list repeats its last value). Leave at 0 to skip this trait. |
| Segment ID | string | No | "" | Optional, blank = document body, or wire in a Header ID / Footer ID / Footnote ID from the Document tab to target that segment instead. |
| Request JSON | string (list) | No | — | Optional. Wire another Text- or Paragraph-tab component's Request JSON output in to add this trait to that block's existing text instead of inserting new text. Text is ignored while this is connected, and Segment ID is inherited from the incoming block. See [Chaining styles](../../chaining.md). |

## Outputs

| Name | Type | Description |
|---|---|---|
| Request JSON | string (list) | The `insertText` and `updateParagraphStyle` requests for this paragraph. |

## Output

This component never calls the API directly — it emits Request JSON (a Google Docs `Request` object, or a list of them) that goes into [Request Aggregator](../Combine/RequestAggregator.md)'s `Fixed Requests` input (it already carries its own indices, so it must not be re-placed) before [Batch Update](../Document/BatchUpdateDocument.md) can push it live.

## Notes

This component replaces the older range-based Update Paragraph Style component: it is now self-contained, taking Text directly rather than an index into an already-inserted range. Use it whenever a single paragraph needs more than one style trait — named style, alignment, line spacing, indent, and spacing can all be set together on the same inserted text. The single-trait components below (Heading Text, Aligned Text, Line Spacing Text, Indent Text, Space Above Text, Space Below Text) each insert their own separate copy of the text, so wiring the same text into two of them inserts it twice; use Styled Paragraph instead whenever you need to combine multiple traits on one paragraph.

Text and every trait input are index-matched lists: feed a list of Text alongside lists for Named Style, Alignment, Line Spacing, Indent, Space Above, and Space Below to style several paragraphs in one call, each landing as its own paragraph. Any trait list shorter than Text repeats its last value for the remaining paragraphs; leaving a trait blank/0 skips it for every paragraph where it isn't set. Segment ID stays a single value and targets the same segment for every paragraph.

## Related

- [Heading Text](HeadingTextJson.md), [Aligned Text](AlignedTextJson.md), [Line Spacing Text](LineSpacingTextJson.md), [Indent Text](IndentTextJson.md), [Space Above Text](SpaceAboveTextJson.md), [Space Below Text](SpaceBelowTextJson.md) — single-trait alternatives; combine multiple traits here instead of chaining them.
- [Named Style preset](../Presets/NamedStyleTypePreset.md) — feeds the Named Style input.
- [Alignment preset](../Presets/AlignmentPreset.md) — feeds the Alignment input.
