# Create Header

Creates a header segment in one or more documents, optionally inserting initial text into each in the same step.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Token | string | Yes | "" | The OAuth 2.0 bearer token. |
| Document ID | string (list) | Yes | — | ID(s) of the document(s) to add a header to. A list creates one header per item. |
| Text | string (list) | No | "" | Optional text to insert into each header as soon as it's created, index-matched to Document ID (a shorter list repeats its last value). Leave blank to create an empty header. |

## Outputs

| Name | Type | Description |
|---|---|---|
| Status | string (list) | HTTP status of each request, index-matched to Document ID. |
| Response | string (list) | Raw response body from each underlying batch update call, index-matched to Document ID. |
| Header ID | string (list) | ID of each newly created header segment, index-matched to Document ID. Wire an entry into any Text-tab or Paragraph-tab component's optional Segment ID input to target that header instead of the document body. |

## API endpoint

No single dedicated endpoint. This component composes Batch Update Document calls internally: it issues a `createHeader` request (type is hardcoded to `DEFAULT` — Google's HeaderFooterType enum supports no other value for creation), reads back the server-generated Header ID from the reply, and, if Text is non-blank, issues a follow-up `insertText` request targeting that segment — all within the same component.

## Notes

Headers, footers, and footnotes each live in their own index space (a "segment"), separate from the document body. The Header ID this component returns is the one piece of information that genuinely requires a live round-trip to obtain, since it's server-generated and unknowable before the create request runs.

Once you have the Header ID, richer content — bold text, multiple lines, alignment, mixed styling — can be layered in afterward: every Text-tab and Paragraph-tab component has an optional Segment ID input (blank by default, meaning the document body) that accepts this ID to target the header instead. If more than one block needs to land in the same header, route them through their own, separate Request Aggregator instance (Combine tab) — each segment's index space is independent, and mixing segments through one Request Aggregator run would corrupt the content-length tally used to position them.

"Header Type" is not exposed as an input: Google's API only allows `DEFAULT` for header creation (odd/even/first-page headers are controlled separately through document style flags, not this field), so it's hardcoded rather than offered as a choice.

Document ID and Text are index-matched lists: feed a list of Document ID to create a header in several documents in one call, optionally alongside a list of Text to seed each header's initial content. If Text has fewer entries than Document ID, the last value repeats for the remaining documents. One failing document does not stop the rest — Status, Response, and Header ID are all index-matched to Document ID.

## Related

- [Get Document](GetDocument.md), [Create Document](CreateDocument.md) — both provide a Document ID to feed here.
- [Create Footer](CreateFooterJson.md), [Create Footnote](CreateFootnoteJson.md) — the same create-segment-and-optionally-fill pattern, for the other segment types.
- [Batch Update Document](BatchUpdateDocument.md) — where further content aimed at this header (via Segment ID) is ultimately sent.
