# Twist-Taper Tower Report

Demo file: `demos/example-twist-taper-tower.gh`

This example builds a short report around a parametric geometry study — a twisted, tapered
tower — by capturing the Rhino viewport as an image and dropping it into a Google Doc next to
generated heading and body text. It exercises the plugin's image pipeline end to end: viewport →
local file → public URL → inline image, sequenced alongside a text block through Request Aggregator.

## Pipeline

1. **[Create Document](../components/Document/CreateDocument.md)** (Document tab) — creates the
   target doc from a `Title` and returns its `Document ID`, which every downstream write component
   below chains from.

2. **[Capture Rhino View](../components/Insert/CaptureRhinoView.md)** (Insert tab) — captures the
   active Rhino viewport showing the twist-taper tower geometry to a local PNG file and outputs its
   `File Path`. Leave `View Name` blank to grab whatever viewport is currently active; set `Width` /
   `Height` for a specific capture resolution instead of the viewport's own size.

3. **[Get Image URL](../components/Insert/GetImageUrl.md)** (Insert tab) — takes that `File Path`,
   uploads it to Google Drive via the supplied `Token`, and makes it link-public. This step exists
   because `insertInlineImage` fetches its image over an unauthenticated request — a local file path
   or in-memory bitmap has no way into a document until it's hosted somewhere public first. The
   resulting `Image URL` is what actually goes into the request.

4. **[Report Skeleton](../components/Paragraph/ReportSkeleton.md)** (Paragraph tab) — builds the
   surrounding text: a document `Title`, one or more `Headings` (with `Heading Levels`) describing
   the study, and `Bodies` paragraphs with commentary. Self-contained, like every Paragraph-tab
   component — no document index required.

5. **[Insert Image](../components/Insert/InsertInlineImageJson.md)** (Insert tab) — wires the
   `Image URL` from step 3 into a self-contained `insertInlineImage` request. `Width`/`Height` can
   be set here to control the inserted image's size on the page.

6. **[Request Aggregator](../components/Combine/RequestAggregator.md)** (Combine tab) — takes the Report
   Skeleton block and the Insert Image block on its `Request JSON` input (one wire each, or both
   through a Merge), places them in that order, and emits one `Requests JSON` payload.

7. **[Batch Update Document](../components/Document/BatchUpdateDocument.md)** (Document tab) — the
   `Document ID` from step 1 and the `Requests JSON` from step 6, fired on a button click, write
   everything to the live document in one call.

## Notes

- Steps 2 and 3 are button-triggered, same as Batch Update in step 8 — the viewport capture and
  Drive upload only run when you click them, not on every canvas recompute.
- If you already have the image hosted somewhere public (e.g. a Drive link or a CDN URL), skip steps
  2–3 and wire the URL straight into Get Image URL's `URL` input (or directly into Insert Image) —
  see [Get Image URL](../components/Insert/GetImageUrl.md)'s priority order (URL passthrough >
  Bitmap > File Path).
- See [quickstart.md](../quickstart.md) for the minimal text-only version of steps 1, 4, 6–8.

## Related

- [authentication.md](../authentication.md) — the token used in steps 1, 3, and 8 needs the
  `documents` scope for the Docs calls; the same (or a separate) token used in step 3 also needs the
  `drive.file` scope for Get Image URL's upload-and-host step.
