# Text Color Text

Builds `insertText` + `updateTextStyle` request pairs that insert one or more lines of text, each in a specified foreground color.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Text | string (list) | Yes | — | Text to insert. A list produces one paragraph per item. |
| Color | string (list) | No | "#000000" | Foreground text color as `#RRGGBB` hex or `R,G,B` (0-255), index-matched to Text. A shorter list repeats its last color. An unparseable value is skipped (that line keeps the default color) with a runtime warning. |
| Segment ID | string | No | "" | Optional, blank = document body, or wire in a Header ID / Footer ID / Footnote ID from the Document tab to target that segment instead. |
| Request JSON | string (list) | No | — | Optional. Wire another Text- or Paragraph-tab component's Request JSON output in to add this trait to that block's existing text instead of inserting new text. Text is ignored while this is connected, and Segment ID is inherited from the incoming block. See [Chaining styles](../../chaining.md). |

## Outputs

| Name | Type | Description |
|---|---|---|
| Request JSON | string (list) | The `insertText` and `updateTextStyle` Request JSON objects for this text. |

## Output

This component never calls the API directly — it emits Request JSON (a Google Docs `Request` object, or a list of them) that must be wired into [Request Aggregator](../Combine/RequestAggregator.md)'s `Request JSON` input (on its own wire, in the order it should appear, to get a real document position) before [Batch Update](../Document/BatchUpdateDocument.md) can push it live.

## Notes

This is an atomic single-trait component: it inserts the given text and sets its foreground color in one block. Because it inserts its own copy of the text, wiring the same text string into this component and into another atomic trait component (for example, Bold Text) inserts the text twice — once in the given color, once bold — rather than once with both traits applied. To combine text color with other traits on the same piece of text, use [Styled Text](UpdateTextStyleJson.md) instead. Chaining is now the direct way to do this: wire this component's Request JSON output into the next trait component's Request JSON input and the trait is added to the same text, no second copy and no need to switch to the composite. See [Chaining styles](../../chaining.md).

Feed a list of Text and a list of Color to get several lines, each in its own color and each landing as its own paragraph. If Color has fewer entries than Text, the last color repeats for the remaining lines.

Leave Segment ID blank to insert into the main document body. To insert into a header, footer, or footnote instead, wire the corresponding Header ID, Footer ID, or Footnote ID (produced by the Document-tab components that create those segments) into Segment ID.

## Related

- [Styled Text](UpdateTextStyleJson.md) — combine text color with other style traits on the same text.
