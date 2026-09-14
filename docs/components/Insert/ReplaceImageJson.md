# Replace Image

Builds one or more Google Docs `replaceImage` requests, each swapping an existing inline image for a different image, given the image's object ID.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Image Object ID | string (list) | Yes | — | Object ID(s) of the inline image(s) to replace. A list builds one request per item. |
| Image URL | string (list) | Yes | "" | URL of the replacement image, index-matched to Image Object ID (a shorter list repeats its last value). |

## Outputs

| Name | Type | Description |
|---|---|---|
| Request JSON | string (list) | The `replaceImage` Request JSON object(s), index-matched to Image Object ID. |

## Output

This component never calls the API directly — it emits a single Request JSON object (a Google Docs `Request`) that goes into [Request Aggregator](../Combine/RequestAggregator.md)'s `Fixed Requests` input (it carries no document position, so there is nothing to place) before [Batch Update](../Document/BatchUpdateDocument.md) can push it live. Because it targets the image by object ID rather than a position, it must not go into the placed `Request JSON` input the way Text/Paragraph components do.

## Notes

This component has no Index input, but the same principle applies through Image Object ID: it must be a real object ID already present in the document. Google Docs assigns that ID when an image is inserted, so it has to come from a prior [Get Document](../Document/GetDocument.md) read (or from the response of an earlier insert) — it is not something chosen ahead of time. Like the Index used by the rest of the Insert tab, this is a genuine reference into the existing document, unlike the self-indexed Text/Paragraph-tab components which always work from a placeholder position.

Feed a list of Image Object ID and a list of Image URL to replace several images in one call, each pair index-matched. If Image URL has fewer entries than Image Object ID, the last URL repeats for the remaining replacements.

## Related

- [Insert Image](InsertInlineImageJson.md) — inserts a new inline image; use this first if the image doesn't exist in the document yet, then reference its object ID here to replace it later.
