# Paragraph

Components that build Google Docs `Request` JSON for inserting text with paragraph-level formatting — headings, alignment, line spacing, indentation, spacing, and bullets — plus a few composite components that assemble multi-paragraph blocks (report skeletons, title blocks, bullet lists).

Every component in this tab (except the three composite report components) accepts an optional Segment ID input: leave it blank to target the document body, or wire in a Header ID / Footer ID / Footnote ID from the Document tab to target that segment instead.

Every component here that inserts and styles text also accepts an optional Request JSON input: wire another Text- or Paragraph-tab component's Request JSON output in and this component adds its trait to that block's existing text instead of inserting a new copy, so traits can be stacked one component at a time. The three composite report components have no such input — they are sources you chain from. See [Chaining styles](../../chaining.md).

None of these components call the API directly. Each emits Request JSON that must be wired into [Request Aggregator](../Combine/RequestAggregator.md)'s `Request JSON` input (on its own wire, in the order it should appear, to get a real document position) before [Batch Update](../Document/BatchUpdateDocument.md) can apply it.

| Component | Description |
|---|---|
| [Styled Paragraph](UpdateParagraphStyleJson.md) | Inserts text and applies any combination of named style, alignment, line spacing, indent, and spacing in a single self-contained block. |
| [Heading Text](HeadingTextJson.md) | Inserts text styled with a named heading style (e.g. `HEADING_1`). |
| [Aligned Text](AlignedTextJson.md) | Inserts text with a specific paragraph alignment. |
| [Line Spacing Text](LineSpacingTextJson.md) | Inserts text with a specific line spacing. |
| [Indent Text](IndentTextJson.md) | Inserts text with a specific start indent. |
| [Space Above Text](SpaceAboveTextJson.md) | Inserts text with a specific space above the paragraph. |
| [Space Below Text](SpaceBelowTextJson.md) | Inserts text with a specific space below the paragraph. |
| [Bullet Item Text](CreateParagraphBulletsJson.md) | Inserts a single bulleted line of text. |
| [Report Skeleton](ReportSkeleton.md) | Builds a title, a set of styled section headings, and their body paragraphs in one indexed sequence. |
| [Report Header Block](ReportHeaderBlock.md) | Builds a standard project title block (name, project number, date, author, revision) in one indexed sequence. |
| [Bullet List](BulletList.md) | Builds a multi-item bulleted list, with optional per-item indent levels, in one indexed sequence. |
