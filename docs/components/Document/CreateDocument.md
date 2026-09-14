# Create Document

Creates one or more new, empty Google Docs with the given title(s).

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Token | string | Yes | "" | The OAuth 2.0 bearer token. |
| Title | string (list) | Yes | — | Title(s) for the new document(s). A list creates one document per item. |

## Outputs

| Name | Type | Description |
|---|---|---|
| Status | string (list) | HTTP status of each request, index-matched to Title. |
| Response | string (list) | Raw response body from each create_document call, index-matched to Title. |
| Document ID | string (list) | ID of each newly created document, index-matched to Title. Chains into Batch Update Document, Clear Document, Create Header, Create Footer, and Create Footnote. |

## API endpoint

`POST https://docs.googleapis.com/v1/documents`

## Example response

```json
{
  "documentId": "1mnF2mvJ3rfOpKpwL3b8yJWoXT99uxXmcVCV1GaUWhGY",
  "title": "PleaseREST DocsConnect Seed Doc"
}
```

## Notes

The request body is just `{ "title": ... }` — the create_document call uses its own Title-only shape and does not go through the Requests JSON payload contract that Batch Update Document consumes.

The Document ID in the response is the key output: wire it into Batch Update Document for a create-then-edit workflow, or into Clear Document / Create Header / Create Footer / Create Footnote to operate on the freshly created document.

Feed a list of Title to create several documents in one call — each title runs its own create_document request in turn, and a failure on one title does not stop the rest from being attempted. Status, Response, and Document ID are index-matched to Title, so a failed entry shows "Failed." with the error in Response and a blank Document ID at that position, while the other entries still succeed normally.

## Related

- [Batch Update Document](BatchUpdateDocument.md) — apply edits to the new document immediately after creating it.
- [Clear Document](ClearDocument.md) — wipe the new document's body (mainly useful when re-running a workflow against an existing document instead).
- [Create Header](CreateHeaderJson.md), [Create Footer](CreateFooterJson.md), [Create Footnote](CreateFootnoteJson.md) — add header/footer/footnote segments to the new document.
- [Get Document](GetDocument.md) — fetch a document later by the ID this component returned.
