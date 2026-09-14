# Insert

Components for inserting images and page/section breaks, plus two prep helpers for getting a local Rhino capture onto the public internet where Google Docs can fetch it.

Four of these build Google Docs `Request` JSON and are index-free like the Text and Paragraph tabs: each authors its request(s) at a placeholder position and must be wired into [Request Aggregator](../Combine/RequestAggregator.md)'s `Request JSON` input to get a real document index. The exception is [Replace Image](ReplaceImageJson.md), which targets an existing image by object ID rather than inserting new content, so there's no position to place and it feeds Request Aggregator directly.

The other two — [Capture Rhino View](CaptureRhinoView.md) and [Get Image URL](GetImageUrl.md) — don't emit Request JSON at all. They're button-triggered HTTP/file-I/O helpers that turn a Rhino viewport into a public image URL (capture to PNG, then host it), which [Insert Image](InsertInlineImageJson.md) or Replace Image can then reference.

There is no Insert Table component: Google doesn't publish an index-delta formula for a table's structural growth, so a bare table insert can't be reliably auto-sequenced the way image/break requests can. Build a table from data instead, via [Data Tree To Table](../Table/DataTreeToTable.md) or [Fixed Size Table](../Table/FixedSizeTable.md) — those compute their own verified cell-offset math internally.

| Component | Description |
|---|---|
| [Insert Image](InsertInlineImageJson.md) | Inserts one or more inline images from URLs, with optional width/height. |
| [Replace Image](ReplaceImageJson.md) | Replaces an existing inline image, referenced by object ID, with a new image from a URL. |
| [Insert Page Break](InsertPageBreakJson.md) | Inserts one or more page breaks. |
| [Insert Section Break](InsertSectionBreakJson.md) | Inserts one or more section breaks of a given type. |
| [Capture Rhino View](CaptureRhinoView.md) | Captures the active Rhino viewport to a local PNG file. |
| [Get Image URL](GetImageUrl.md) | Hosts a Bitmap or local File Path on Google Drive and returns a public URL, or passes an already-public URL through. |
