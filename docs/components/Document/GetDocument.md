# Get Document

Fetches one or more Google Docs by Document ID, or resolves a document Name to an ID via the Drive API and then fetches it, for each entry independently.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Token | string | Yes | "" | The OAuth 2.0 bearer token. |
| Document ID | string (list) | Yes | — | ID(s) of the document(s) to fetch. Leave an entry blank and supply the matching Name instead to look that document up by name. |
| Name | string (list) | No | — | Document name(s) to resolve to an ID via the Drive API, index-matched to Document ID. Used only at positions where Document ID is blank. |

## Outputs

| Name | Type | Description |
|---|---|---|
| Status | string (list) | HTTP status of each request, index-matched to Document ID / Name. |
| Response | string (list) | Raw response body from each get_document call, index-matched to Document ID / Name. |
| Title | string (list) | Each document's title, index-matched to Document ID / Name. |
| Document ID | string (list) | The ID actually used at each position (resolved from Name when a name lookup happened). Chains into Batch Update Document, Clear Document, Create Header, Create Footer, and Create Footnote. |
| End Index | int (list) | The index to append at — the end of the document body — index-matched to Document ID / Name. Wire it into [Request Aggregator](../Combine/RequestAggregator.md)'s Start Index to add content BELOW what the document already holds. `1` (the top of an empty body) when the fetch failed or the body could not be read. |

## Appending vs. overwriting

There is no separate "append" component. Placement is entirely a matter of what Request Aggregator is told to start from:

- **Start Index = 1** (the default) puts the new content at the *top* of the body, above anything already there.
- **Start Index = this component's End Index** appends it *below* the existing content.

End Index is the last body element's `endIndex` minus one — the body always ends with a final paragraph break that Docs will not let anything be inserted at or after. Fetch the document first, wire End Index into Request Aggregator's Start Index, then send its Requests JSON into Batch Update.

## API endpoint

`GET https://docs.googleapis.com/v1/documents/{documentId}`

Optional name-lookup path (used only when Name is supplied instead of Document ID): `GET https://www.googleapis.com/drive/v3/files` — **this call is UNVALIDATED**. It has not been confirmed against a live Drive API response; see Notes below before relying on it in production.

## Example response

```json
{
  "documentId": "1mnF2...",
  "title": "PleaseREST DocsConnect Seed Doc"
}
```

Name-lookup response shape (UNVALIDATED — not yet confirmed live):

```json
{
  "files": [
    { "id": "1mnF2mvJ3rfOpKpwL3b8yJWoXT99uxXmcVCV1GaUWhGY", "name": "PleaseREST DocsConnect Seed Doc" }
  ]
}
```

## Notes

Get Document normally fetches by Document ID directly. If Name is filled in instead, the component resolves it to an ID first by querying the Drive API's file list, then fetches the document as usual.

The name-lookup path requires a Drive-scoped token — `drive.readonly` or `drive.metadata.readonly`. A token scoped only to `drive.file` will not see documents unless this plugin created them.

The name-lookup call has not been validated against a live response: the token available during testing was scoped to Docs only (`documents`) and expired before a Drive-scoped token could be tested. Confirm the `files[].id` / `files[].name` shape with a fresh `drive.readonly` token before relying on the Name input in a production workflow.

The Document ID output reflects whichever ID was actually used — either the one supplied directly, or the one resolved from Name — and is the value to wire into downstream Document-tab components.

Document ID and Name are index-matched lists: at each position, a non-blank Document ID is used directly; otherwise the Name at that same position is looked up via Drive. Feed lists of either or both to fetch several documents (by ID, by name, or a mix of both) in one call. One failing or unresolvable entry does not stop the rest — Status, Response, Title, and Document ID are all index-matched, so each position reports its own outcome independently.

## Related

- [Create Document](CreateDocument.md) — creates a document whose ID can be fed here (or skips Get Document entirely for freshly-created docs).
- [Batch Update Document](BatchUpdateDocument.md) — apply edits to the document, using this component's Document ID output.
- [Clear Document](ClearDocument.md) — wipe the document's body, using this component's Document ID output.
- [Create Header](CreateHeaderJson.md), [Create Footer](CreateFooterJson.md), [Create Footnote](CreateFootnoteJson.md) — create header/footer/footnote segments on the document, using this component's Document ID output.
