# Clear Document

Deletes all content from one or more documents' bodies, leaving each empty.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Token | string | Yes | "" | The OAuth 2.0 bearer token. |
| Document ID | string (list) | Yes | — | ID(s) of the document(s) to clear. A list clears one document per item. |

## Outputs

| Name | Type | Description |
|---|---|---|
| Status | string (list) | HTTP status of each request, index-matched to Document ID. |
| Response | string (list) | Raw response body from each underlying batch update call, index-matched to Document ID. |

## API endpoint

No single dedicated endpoint. This component composes two calls internally: it first fetches the document (Get Document's endpoint) to find the real end index of the body, then issues one Batch Update Document call containing a single `deleteContentRange` request spanning from index 1 to that end index minus 1.

## Notes

This clears the entire body — there is no partial-range option. The deleted range always starts at index 1; only the document's very last index is excluded, because Google Docs always keeps an implicit trailing newline there that cannot be removed.

Like Create Header / Create Footer / Create Footnote, this component does not consume the Requests JSON payload contract for its own operation — it builds its request internally rather than taking a Requests JSON input.

Feed a list of Document ID to clear several documents in one call — each ID is fetched and cleared in turn, and one failing document (unreadable, or already empty) does not stop the rest. Status and Response are index-matched to Document ID, so each entry reports its own outcome ("Cleared.", "Already empty.", "Could not read document.", etc.) independently.

## Related

- [Get Document](GetDocument.md), [Create Document](CreateDocument.md) — both provide a Document ID to feed here.
- [Batch Update Document](BatchUpdateDocument.md) — the underlying call this component composes for the actual delete.
- [Create Header](CreateHeaderJson.md), [Create Footer](CreateFooterJson.md), [Create Footnote](CreateFootnoteJson.md) — clearing the body does not affect existing header/footer/footnote segments, which have independent index spaces.
