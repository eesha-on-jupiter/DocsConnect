# Space Above Text

Inserts one or more paragraphs and applies spacing above each, producing `insertText` requests followed by `updateParagraphStyle(spaceAbove)` requests.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Text | string (tree) | Yes | — | Text to insert. Each item places its own paragraph; a multi-branch tree is flattened into one ordered list. |
| Space Above | double (tree) | No | 0.0 | Space above the paragraph, in points, index-matched to Text (a shorter list repeats its last value; flattened the same way as Text). Only applied where >0. |
| Segment ID | string | No | "" | Optional, blank = document body, or wire in a Header ID / Footer ID / Footnote ID from the Document tab to target that segment instead. |
| Request JSON | string (list) | No | — | Optional. Wire another Text- or Paragraph-tab component's Request JSON output in to add this trait to that block's existing text instead of inserting new text. Text is ignored while this is connected, and Segment ID is inherited from the incoming block. See [Chaining styles](../../chaining.md). |

## Outputs

| Name | Type | Description |
|---|---|---|
| Request JSON | string (list) | The `insertText` and `updateParagraphStyle` requests for this paragraph. |

## Output

This component never calls the API directly — it emits Request JSON (a Google Docs `Request` object, or a list of them) that must be wired into [Request Aggregator](../Combine/RequestAggregator.md)'s `Request JSON` input (on its own wire, in the order it should appear, to get a real document position) before [Batch Update](../Document/BatchUpdateDocument.md) can push it live.

## Notes

This is an atomic single-trait component: it inserts its own separate copy of the text and applies only space above. If the same text is also wired into another atomic component (Heading Text, Aligned Text, etc.), the text is inserted twice as two separate paragraphs — to combine space above with other traits on the same paragraph, use Styled Paragraph instead. Chaining is now the direct way to do this: wire this component's Request JSON output into the next trait component's Request JSON input and the trait is added to the same text, no second copy and no need to switch to the composite. See [Chaining styles](../../chaining.md).

Feed a list of Text and a list of Space Above to get several paragraphs, each with its own spacing. If Space Above has fewer entries than Text, the last value repeats for the remaining paragraphs.

Text and Space Above are read as data trees and flattened across all branches into one ordered list, rather than plain list access — this keeps the component's running insertion cursor intact even when the upstream source is a multi-branch tree (list access would re-solve the component once per branch and silently reset that state).

## Related

- [Styled Paragraph](UpdateParagraphStyleJson.md) — use instead when combining space above with other traits on the same paragraph.
