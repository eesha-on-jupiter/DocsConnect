# Batch Update Document

Applies the same batch of edit requests to one or more existing documents.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Token | string | Yes | "" | The OAuth 2.0 bearer token. |
| Document ID | string (list) | Yes | — | ID(s) of the document(s) to edit. A list applies the same Requests JSON to each document in turn. |
| Requests JSON | string (list) | Yes | "" | The list of request objects to apply, in order, to every Document ID. |

## Outputs

| Name | Type | Description |
|---|---|---|
| Status | string (list) | HTTP status of each request, index-matched to Document ID. |
| Response | string (list) | Raw response body from each batch_update_document call, index-matched to Document ID. |

## API endpoint

`POST https://docs.googleapis.com/v1/documents/{documentId}:batchUpdate`

## Example response

```json
{
  "documentId": "1mnF2mvJ3rfOpKpwL3b8yJWoXT99uxXmcVCV1GaUWhGY",
  "replies": [{}],
  "writeControl": { "requiredRevisionId": "ALtnJHw2..." }
}
```

## Notes

This is a custom-method endpoint (note the `:batchUpdate` suffix on the URL) rather than a plain REST verb. The `replies` array in the response aligns 1:1 with the requests sent in `Requests JSON`, so the Nth reply corresponds to the Nth request.

`writeControl.requiredRevisionId` in the response enables optimistic concurrency for a follow-up call, though this component does not currently expose a `writeControl` input of its own.

Requests JSON is normally produced upstream by Request Aggregator (Combine tab), which collects the Request JSON output of Text-, Paragraph-, Insert-, and Table-tab components into the single list this component expects. This component is the only one on the Document tab that consumes that payload contract directly — Clear Document, Create Header, Create Footer, and Create Footnote all build their own requests internally instead.

Feed a list of Document ID to apply the same Requests JSON to several documents in one call — each document is updated in turn, and one failing document does not stop the rest. Status and Response are index-matched to Document ID, so each entry reports "Applied." or "Failed." independently.

## Related

- [Get Document](GetDocument.md), [Create Document](CreateDocument.md) — both provide a Document ID to feed here.
- [Clear Document](ClearDocument.md) — a narrower operation that composes its own batch update internally rather than taking Requests JSON.
- [Create Header](CreateHeaderJson.md), [Create Footer](CreateFooterJson.md), [Create Footnote](CreateFootnoteJson.md) — create segments whose returned IDs let further content, sent through this component, target a header/footer/footnote instead of the body.
