# Create Footnote

Creates one or more footnote references, appended at the end of each document's body, optionally inserting initial text into each footnote's segment in the same step.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Token | string | Yes | "" | The OAuth 2.0 bearer token. |
| Document ID | string (list) | Yes | — | ID(s) of the document(s) to add a footnote to. A list creates one footnote per item. |
| Text | string (list) | No | "" | Optional text to insert into each footnote as soon as it's created, index-matched to Document ID (a shorter list repeats its last value). Leave blank to create an empty footnote. |

## Outputs

| Name | Type | Description |
|---|---|---|
| Status | string (list) | HTTP status of each request, index-matched to Document ID. |
| Response | string (list) | Raw response body from each underlying batch update call, index-matched to Document ID. |
| Footnote ID | string (list) | ID of each newly created footnote segment, index-matched to Document ID. Wire an entry into any Text-tab or Paragraph-tab component's optional Segment ID input to target that footnote instead of the document body. |

## API endpoint

No single dedicated endpoint. This component composes API calls internally: it first calls Get Document to find the current end of the body, issues a `createFootnote` request anchored there, reads back the server-generated Footnote ID from the reply, and, if Text is non-blank, issues a follow-up `insertText` request targeting that segment — all within the same component.

## Notes

No index needed. Unlike the Text- and Paragraph-tab components, this is a live write against a real document rather than a stackable content block routed through Request Aggregator, so "append" means reading the document first: it fetches the body's current end index (the same `content[].endIndex - 1` pattern used by [Clear Document](ClearDocument.md), anchoring before the document's implicit trailing newline) and anchors the footnote marker there.

Headers, footers, and footnotes each live in their own index space (a "segment"), separate from the document body. The Footnote ID this component returns is the one piece of information that genuinely requires a live round-trip to obtain, since it's server-generated and unknowable before the create request runs.

Once you have the Footnote ID, richer content — bold text, multiple lines, alignment, mixed styling — can be layered in afterward: every Text-tab and Paragraph-tab component has an optional Segment ID input (blank by default, meaning the document body) that accepts this ID to target the footnote instead. If more than one block needs to land in the same footnote, route them through their own, separate Request Aggregator instance (Combine tab) — each segment's index space is independent, and mixing segments through one Request Aggregator run would corrupt the content-length tally used to position them.

Document ID and Text are index-matched lists: feed a list of Document ID to create a footnote in several documents in one call, alongside a list for Text. If Text has fewer entries than Document ID, the last value repeats for the remaining documents. One failing document does not stop the rest — Status, Response, and Footnote ID are all index-matched to Document ID.

## Related

- [Get Document](GetDocument.md), [Create Document](CreateDocument.md) — both provide a Document ID to feed here.
- [Create Header](CreateHeaderJson.md), [Create Footer](CreateFooterJson.md) — the same create-segment-and-optionally-fill pattern, for the other segment types.
- [Batch Update Document](BatchUpdateDocument.md) — where further content aimed at this footnote (via Segment ID) is ultimately sent.
