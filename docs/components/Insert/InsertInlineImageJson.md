# Insert Image

Builds one or more Google Docs `insertInlineImage` requests, self-indexed from a placeholder position — one image per Image URL entry.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Image URL | string (list) | Yes | "" | Publicly accessible URL of the image to insert. A list inserts one image per entry. |
| Width | double (list) | No | 0.0 | Image width in points, index-matched to Image URL (a shorter list repeats its last value). Leave at 0 to use the image's native size. |
| Height | double (list) | No | 0.0 | Image height in points, index-matched to Image URL (a shorter list repeats its last value). Leave at 0 to use the image's native size. |
| Own Paragraph | bool | Yes | true | Put each image on its own line (a paragraph break after each). False = images sit side by side in one paragraph. |

## Outputs

| Name | Type | Description |
|---|---|---|
| Request JSON | string (list) | The `insertInlineImage` Request JSON object(s), one per Image URL entry. |

## Output

This component never calls the API directly — it emits Request JSON objects (Google Docs `Request`s) that must be wired into [Request Aggregator](../Combine/RequestAggregator.md)'s `Request JSON` input (on its own wire, in the order it should appear, to get a real document position) and [Batch Update](../Document/BatchUpdateDocument.md) can push them live.

## Notes

No index needed. Each request is authored at a placeholder position and self-sequenced: multiple images in one call land back-to-back, since each inline image occupies a single placeholder character in the document (the same content-length accounting Request Aggregator already uses for Text/Paragraph blocks).

Image URL, Width, and Height are index-matched lists: feed a list of Image URL to insert several images in one call. If Width or Height has fewer entries than Image URL, its last value repeats for the remaining images.

Width and Height are optional: leave both at 0 to let Google Docs size the image from its natural dimensions.

The URL needs to be public — Google's servers fetch it directly, they can't read a local file. To host a local image (e.g. a captured Rhino viewport) first, see [Get Image URL](GetImageUrl.md).

## Related

- [Get Image URL](GetImageUrl.md) — resolves a Bitmap or local File Path to the public URL this component needs.
- [Replace Image](ReplaceImageJson.md) — swaps an already-inserted inline image for a different image, referenced by its object ID rather than a position.
