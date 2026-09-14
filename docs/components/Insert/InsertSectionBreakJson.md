# Insert Section Break

Builds one or more Google Docs `insertSectionBreak` requests, self-indexed from a placeholder position — one break per Section Type entry.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Section Type | string (list) | No | "NEXT_PAGE" | The `SectionType` value for each break, e.g. `NEXT_PAGE` or `CONTINUOUS`. A list inserts one break per entry. |

## Outputs

| Name | Type | Description |
|---|---|---|
| Request JSON | string (list) | The `insertSectionBreak` Request JSON object(s), one per Section Type entry. |

## Output

This component never calls the API directly — it emits Request JSON objects (Google Docs `Request`s) that must be wired into [Request Aggregator](../Combine/RequestAggregator.md)'s `Request JSON` input (on its own wire, in the order it should appear, to get a real document position) and [Batch Update](../Document/BatchUpdateDocument.md) can push them live.

## Notes

No index needed. Each request is authored at a placeholder position and self-sequenced: multiple breaks in one call land back-to-back, since each section break occupies two indices in the document (the newline Google inserts before it plus the break). The number of breaks is driven by how many entries are in Section Type.

## Related

- [Insert Page Break](InsertPageBreakJson.md) — inserts a page break instead of a section break.
