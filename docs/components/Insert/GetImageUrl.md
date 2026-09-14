# Get Image URL

Resolves an image to the public URL that [Insert Image](InsertInlineImageJson.md) / [Replace Image](ReplaceImageJson.md) need. Google Docs' `insertInlineImage` fetches its `uri` unauthenticated over the public internet, so a local Bitmap or File Path (e.g. from [Capture Rhino View](CaptureRhinoView.md)) has no way into a document without first being hosted somewhere public.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Token | string | No | "" | Google OAuth 2.0 access token with the `drive.file` scope. Only needed when uploading a Bitmap or File Path — not used for URL passthrough. |
| Bitmap | generic (list) | No | — | In-memory image(s) to host. |
| File Path | string (list) | No | — | Local path(s) of images to host, e.g. Capture Rhino View's output. |
| URL | string (list) | No | — | Already-public image URL(s) — passed through unchanged, no upload. |

## Outputs

| Name | Type | Description |
|---|---|---|
| Image URL | string (list) | Publicly accessible URL of each image, in input order (URLs, then Bitmaps, then File Paths) — wire into Insert Image / Replace Image's Image URL input. |

## Notes

All three inputs take lists and are all processed: URLs pass straight through (no upload, no token needed), then every Bitmap and every File Path is uploaded, in that order. Bitmap and File Path are uploaded to Google Drive as a new file and made link-public, then returned as `https://drive.google.com/uc?id={fileId}` — the conventional form for embedding a public Drive file's raw bytes (Drive's own `webContentLink` triggers a download instead of an inline fetch).

Uploading requires an OAuth token with the `https://www.googleapis.com/auth/drive.file` scope — the same scope already required plugin-wide, sufficient because it grants full access to files the app itself creates. See [authentication](../../authentication.md) for how to get a token.

## Related

- [Capture Rhino View](CaptureRhinoView.md) — a common source of the File Path input.
- [Insert Image](InsertInlineImageJson.md) — the typical destination for this component's output.
