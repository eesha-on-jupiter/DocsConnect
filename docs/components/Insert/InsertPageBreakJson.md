# Insert Page Break

Builds one or more Google Docs `insertPageBreak` requests, self-indexed from a placeholder position.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Count | int | No | 1 | Number of page breaks to insert in sequence. |

## Outputs

| Name | Type | Description |
|---|---|---|
| Request JSON | string (list) | The `insertPageBreak` Request JSON object(s). |

## Output

This component never calls the API directly — it emits Request JSON objects (Google Docs `Request`s) that must be wired into [Request Aggregator](../Combine/RequestAggregator.md)'s `Request JSON` input (on its own wire, in the order it should appear, to get a real document position) and [Batch Update](../Document/BatchUpdateDocument.md) can push them live.

## Notes

No index needed. Each request is authored at a placeholder position and self-sequenced: multiple breaks in one call land back-to-back, since each page break occupies two indices in the document (the break plus the newline Google inserts after it).

## Related

- [Insert Section Break](InsertSectionBreakJson.md) — inserts a section break instead of a page break.
