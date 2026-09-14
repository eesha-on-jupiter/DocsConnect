# Capture Rhino View

Captures the active (or named) Rhino viewport to a PNG file on disk and outputs its file path. Button-triggered, like the other Rhino/network-facing components — capturing the viewport is a "do it now" action, not a pure data transform.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| View Name | string (list) | No | — | Named Rhino viewport(s) to capture — a list captures each in turn. Blank captures the currently active viewport. |
| File Path | string | No | "" | Folder to save the PNGs in — each capture is auto-named `<viewport>_<timestamp>_<n>.png`. For a single capture a full file path is accepted. Blank saves to a temp folder. |
| Width | int | No | 0 | Capture width in pixels. 0 uses the current viewport size. |
| Height | int | No | 0 | Capture height in pixels. 0 uses the current viewport size. |

## Outputs

| Name | Type | Description |
|---|---|---|
| File Path | string (list) | Path of each saved PNG, index-matched to View Name. |

## Notes

Does not upload or host the image anywhere — the output is a local disk path. Google Docs' `insertInlineImage` fetches its `uri` over the public internet, so a local path is not directly usable there; route the output through [Get Image URL](GetImageUrl.md) to get a URL Insert Image can use.

If File Path resolves to an existing folder rather than a file, an auto-named file is saved inside it instead of erroring.

## Related

- [Get Image URL](GetImageUrl.md) — hosts this component's File Path output on Google Drive and returns a public URL.
