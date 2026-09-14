# Insert Text

Builds one or more `insertText` Request JSON objects that insert one or more lines of plain text.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Text | string (list) | Yes | — | Text to insert. A list places each entry on its own line. |
| Segment ID | string | No | "" | Optional, blank = document body, or wire in a Header ID / Footer ID / Footnote ID from the Document tab to target that segment instead. |

## Outputs

| Name | Type | Description |
|---|---|---|
| Request JSON | string (list) | The `insertText` Request JSON object(s), index-matched to Text. |

## Output

This component never calls the API directly — it emits Request JSON (a Google Docs `Request` object, or a list of them) that must be wired into [Request Aggregator](../Combine/RequestAggregator.md)'s `Request JSON` input (on its own wire, in the order it should appear, to get a real document position) before [Batch Update](../Document/BatchUpdateDocument.md) can push it live.

## Notes

This component no longer takes an explicit index — it is always authored at a placeholder position and relies on [Request Aggregator](../Combine/RequestAggregator.md) to resolve it to a real position once it's stacked alongside other blocks. Use one Request Aggregator instance per segment (body, or each header/footer/footnote) you're building content for.

Feed a list of Text to get one `insertText` request per item, each landing as its own paragraph.

Leave Segment ID blank to insert into the main document body. To insert into a header, footer, or footnote instead, wire the corresponding Header ID, Footer ID, or Footnote ID (produced by the Document-tab components that create those segments) into Segment ID. Either way, positions are numbered from 1 within whichever segment is targeted.
