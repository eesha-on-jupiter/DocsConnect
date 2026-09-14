# Changelog

## 1.0.0 — unreleased (in development; versions are tracked from the first GitHub release)

### 2026-09-13

- **Added: `docs/workflows.md`** — thirteen common wirings drawn as component chains (first
  document, chaining, report, viewport images, tables, headers/footers, append, rebuild, lookup by
  name, image swap, page setup, sections with page breaks).

Placement rework and live-testing fixes. Headline: Request Aggregator is now the single hub —
Arrange Content is gone — and it places every block correctly whether blocks arrive on separate
wires or through a Merge, isolates each block's styling from its neighbours, and refuses or flags
the wirings that used to put content at the top of the document. Segment content starts at index
0, page/section breaks are counted at their true size, nested bullets subtract the tabs Google
strips, and Windows line endings no longer throw indices off. Tables gained per-cell formatting
trees, formatted cells from a style component's output, and a Transpose; the image chain takes
lists of viewports.

- **Changed: the image chain takes lists.** Capture Rhino View takes a list of `View Name`s and
  returns one PNG path per view, each auto-named `<viewport>_<timestamp>_<n>.png` in the given
  folder (previously every capture in the same second reused one name and overwrote the last).
  Get Image URL takes lists of Bitmaps, File Paths and URLs and returns one public URL per image,
  in that order. Insert Image gains `Own Paragraph` (default true): each image is followed by a
  paragraph break so a list stacks one per line; set it false for side-by-side.

- **Removed: Formatted Table and the obsolete Arrange Content.** Formatted Table (added earlier
  today) is gone — a Text/Paragraph component's coloured output wired straight into Data Tree To
  Table / Fixed Size Table's `Data` already renders one formatted cell per line, which is the
  simpler route. Arrange Content, hidden and obsolete since Request Aggregator absorbed it, is
  deleted; definitions still carrying it will show a missing component — replace it by wiring
  its inputs straight into Request Aggregator's `Request JSON`.

- **Changed: unplaced blocks are caught before they reach Google.** A block that skips placement
  lands at its own placeholder index — the top of the document, above everything placed, with its
  paragraph style inherited by what follows (the original reversed-order bug, by another route).
  Two checks now make that loud: Request Aggregator refuses a content insert (text, image, break,
  table) in `Fixed Requests` with an error naming the fix, and Batch Update warns when more than
  one content insert targets the start of a segment — the signature of a component wired straight
  into Batch Update next to the aggregator's output.

- **Added: `Transpose` on Data Tree To Table.** Grasshopper data usually arrives one list per
  attribute — a column — not one list per row. With `Transpose = true` each branch is read as a
  column, so `Entwine(labels, Text Color.RJ)` gives a labels column beside a coloured values
  column, 24 rows for 24 values. Format trees follow the same orientation. Registered last so
  saved definitions keep their wires.

- **Added: Formatted Table (Table tab).** A third table component for when the look is easier to
  compose with the style components than as value trees: `Data` carries the cell text, `Style`
  carries Request JSON from Bold Text / Text Color / Font Size / Heading Text or a chain of them,
  matched to Data cell for cell (a shorter list repeats its last value). The style blocks' own
  text is ignored; their style requests are re-targeted at the Data text. Reuses the Data Tree To
  Table icon for now.

- **Added: per-cell formatting trees on Data Tree To Table and Fixed Size Table.** Seven optional
  inputs — `Bold`, `Italic`, `Underline`, `Font Size`, `Font Family`, `Text Color`, `Fill` (cell
  background, via `updateTableCellStyle`) — each a tree matched to `Data` cell-for-cell: row =
  branch, column = item, and a shorter list repeats its last value in both directions, so one
  `true` bolds every cell, one branch formats every row, and a single flat branch is read
  row-major. Formatting from these trees is applied after `Bold Header` (so it wins) and before a
  cell's own chained styles (so those win). Registered after the existing inputs, so saved
  definitions keep their wires.

- **Fixed: formatted table cells now work from a component's normal output.** Data Tree To Table
  and Fixed Size Table documented that a cell could be "the Request JSON of Text Color", but a
  Text/Paragraph component emits a *list* of requests and the tables read every list item as a
  cell — text, blank, text, blank. A row's request items are now combined into blocks and each
  formatted line becomes one cell carrying the styles that overlap it, so Text Color with three
  texts and three colours wired into a row gives three coloured cells; plain strings in the same
  row stay one cell each; two components in one row stay apart. Fixed Size Table additionally
  reads a single flat branch of cells row-major, cut into rows of `Columns`, so one Text Color can
  feed a whole table.

- **Fixed: header/footer/footnote content was placed at index 1; segments start at 0.** The body's
  index 0 is its section break, so body content starts at 1 — but a header, footer or footnote
  segment has no section break, and a fresh one is a single empty paragraph at `[0,1)`. Both
  Request Aggregator (for blocks carrying a Segment ID) and Create Header / Create Footer's
  convenience Text inserted at 1, which Google rejects (`Index 1 must be less than the end index
  of the referenced segment, 1`). Request Aggregator now treats its default Start Index of 1 as
  "the top" of whichever index space the blocks address and uses 0 for segments (an explicit
  other value is honoured); the label shows `(segment)`. Create Header / Footer insert at 0. A new
  footnote segment holds a space and a newline, so Create Footnote inserts at 1, after the space.

- **Fixed: page breaks and section breaks were counted as 1 index; they are 2.** Per the API
  reference, `insertPageBreak` "inserts a page break followed by a newline" and
  `insertSectionBreak` inserts "a newline character before the section break". Everything placed
  after one landed one index short per break. The per-request growth constants now live in one
  place (`DocumentBuilders.InlineImageLength / PageBreakLength / SectionBreakLength / TableLength`)
  and both the self-sequencing builders and the aggregator's tally read from there.

- **Changed: Request Aggregator refuses blocks that mix index spaces.** Blocks addressing the
  body and a segment, or two different segments, in one aggregator would get a shared content
  tally across independent index spaces — every block after the first mis-placed. It now stops
  with an error naming the count of spaces and the one-aggregator-per-segment rule.

- **Changed: every request component warns when it is re-solved.** A list arriving at a
  single-value input (a list of bullet presets, several Segment IDs, a list of Start Indices)
  makes Grasshopper run the component once per value, emitting the whole block again each time —
  visible only as extra output branches, and in the document as duplicated content. All 33
  request-emitting components (Text, Paragraph, Table, Insert, Update Document Style, Request
  Aggregator, Batch Update) now raise a warning naming the input that received the list.
  Update Document Style also emits its Request JSON as a list like every other component, so it
  merges cleanly alongside them.

- **Fixed: composite blocks jumped ahead of everything else in a Merge.** Bullet List, Report
  Skeleton, Report Header Block, Data Tree To Table and Fixed Size Table emitted Request JSON as a
  single item (path `{0}`) while every other component emits a list (path `{0;0}`). Sent through
  one Merge, the two path shapes stayed as separate branches and branch order beat the D1/D2/D3
  order — a bullet list in D3 was placed before the heading in D1. All five now emit a one-item
  list like the rest, so a Merge concatenates its inputs in slot order and Request Aggregator
  places them in that order.

- **Changed: placed blocks no longer inherit formatting from where they land.** Google Docs gives
  inserted text the paragraph style, bullets and text style of the position it is inserted at, so
  re-running a definition into a document that already held a centered H1 at index 1 made the whole
  new run a centered H1. Request Aggregator now follows every `insertText` it places with a reset
  of exactly the inserted range — paragraph style back to Normal text with alignment, spacing and
  indents cleared, bullets removed, and every text property reset (`updateTextStyle` with
  `fields: "*"`) — before the block's own style requests apply. A heading is a heading, plain text
  is plain, a bullet list is bulleted, whatever is above it. Same-paragraph runs (no `
`) get only
  the text reset so they never restyle the paragraph they join. Style requests move nothing, so
  placement is unchanged.

- **Fixed: a nested Bullet List pushed every later block past the end of the document.**
  `createParagraphBullets` reads each paragraph's nesting level from its leading tabs and then
  removes those tabs, so the document grows by one character less per nested item than was
  inserted. Request Aggregator's tally now subtracts the leading tabs of every paragraph inside a
  `createParagraphBullets` range (`Index 128 must be less than the end index of the referenced
  segment, 125` — 4 nested items, 4 characters). Applies to Bullet List and to chained Create
  Paragraph Bullets alike.

- **Fixed: Windows line endings threw every later index off.** Google Docs drops `
` from
  inserted text, so a multi-line Panel's `

` made each block land shorter than the plugin
  counted — the next block's indices were off by one per line and a bullet/style range could run
  past the end of the segment (`Index 170 must be less than the end index of the referenced
  segment, 170`). Text is now normalised to `
` as it enters any component and again in the
  `insertText` builder.

- **Changed: Bullet Preset is validated.** Create Paragraph Bullets and Bullet List now refuse a
  value that is not a Google Docs `BulletGlyphType` (an indent level wired in by mistake, a typo)
  with an error naming the valid presets and pointing nesting at Bullet List's Indent Levels,
  instead of sending it to Google for a 400. A list of presets on the item-access input still
  re-solves the component once per value — wire one preset.

- **Changed: Arrange Content folded into Request Aggregator.** There is now one hub instead of two.
  Request Aggregator's `Request JSON` input reads each wire as its own block and re-indexes the
  blocks in wire order from a new `Start Index` input (wire Get Document's `End Index` in to append
  below existing content), exactly as Arrange Content did. A new optional `Fixed Requests` input
  takes everything that must *not* be shifted — Replace Image, Replace All Text, Update Document
  Style, Create Header/Footer, and Update Text/Paragraph Style or Create Paragraph Bullets with
  explicit indices — and passes it through untouched after the placed blocks. The canvas label
  reports `N blocks` (`+ M fixed`). Blocks that arrive in a single list — through a Merge, a Relay,
  or internalised data — are split apart again: each component's first insert is at index 1 and its
  indices only grow, so an insert at 1 after content has already been inserted starts a new block.
  Merge's D1/D2/D3 order is therefore a supported way to order content. Arrange Content is hidden from the ribbon and marked obsolete but still
  loads, so saved definitions keep running; its output wired into Request Aggregator re-indexes
  with a zero shift. `Request JSON` stays at parameter index 0, so existing Request Aggregator
  wires are preserved. Every component description, the docs and the spec now point at Request
  Aggregator.

- **Fixed: Arrange Content stacked blocks in reverse and let one block's paragraph style bleed into
  the rest.** Grasshopper merges everything arriving at a single input, so several components each
  emitting a flat list at path `{0}` reached `Request JSON` as ONE branch with their requests
  concatenated. Reindexed as a single block, every request kept its placeholder index of 1, and
  since Google applies requests in array order each insert landed at the top of the document, above
  the one before it — content came out reversed (the heading at the bottom of the page), and
  because each block was then inserted *inside* the previous block's first paragraph, that
  paragraph's heading level, alignment and bullets were inherited by everything after it. Arrange
  Content now reads its wired sources directly instead of the merged tree: one block per wire, in
  wire order, and one further sub-block per branch within a wire. Its canvas message reports the
  block count so a miswire is visible at a glance.

- **Fixed: Arrange Content under-counted a table's size.** `insertTable` now contributes its real
  structural footprint, `rows * (2 * columns + 1) + 3`, on top of the text its cell fills carry
  (previously only the cell text counted, so anything stacked after a table landed inside it).
  Derived from the same live-verified cell-offset and stride constants the table builders use.

- **Tables accept formatted cell content.** A cell in Data Tree To Table / Fixed Size Table may now
  be the `Request JSON` of any Text- or Paragraph-tab component — or a chain of them — instead
  of a plain string. The block's `insertText` runs become the cell's text and its style requests are
  re-targeted at the cell's final position, so a table can carry bold, coloured, linked or aligned
  cells. Trailing paragraph breaks are stripped (a cell already ends in its own paragraph) and style
  ranges clamped to match. Values that aren't Docs request JSON — including bare numbers — are
  still used verbatim, so plain and formatted cells mix freely. Cell styling is now emitted after
  every fill, at final indices, so a cell's formatting no longer depends on how much text sits in
  the cells before it.

- **Get Document reports the document's append position.** A new `End Index` output gives the index
  to append at (the body's last `endIndex` minus one). Wire it into Arrange Content's `Start Index`
  to add content below what a document already holds; the default `Start Index` of 1 still puts
  content at the top. There is no separate "append" component — placement is entirely this one
  number.

- **Chainable styles on the Text and Paragraph tabs.** Every styling component on those two tabs
  gained an optional `Request JSON` input. Left empty, the component behaves exactly as before.
  Connected, it skips its own `insertText` and appends its trait over the incoming block's existing
  text spans — so traits can be stacked one component at a time (Heading Text into Text Color into
  Aligned Text produces one colored, centered heading rather than three paragraphs), including onto
  composites like Report Skeleton. Text is ignored while a block is connected (with a warning), and
  Segment ID is inherited from the incoming block. Appended requests are zero-length, so nothing
  downstream of a chained block moves. Insert Text, Replace All Text, and the report composites have
  no chain input — they are sources, not stylers. See [chaining.md](chaining.md).
  - `Text` is now Optional on every chainable component, so a chained component solves without it.
    An unwired Text now reports the component's own "No text supplied." warning instead of
    Grasshopper's generic "failed to collect data".
  - The chain input is registered last on each component, so existing saved definitions keep
    matching their wires by parameter index.

### Earlier

Initial release.

- **Connect tab:** token validation against Google's `tokeninfo` endpoint.
- **Document tab:** get/create a document (with optional lookup by name via Drive's
  `files.list`), push a batch of requests, clear a document's full body, create
  headers/footers/footnotes (optionally seeding them with text in the same step), and update
  document-wide style (margins, page size, background).
- **Text and Paragraph tabs:** every component is self-contained — no document index required.
  Combined-style components (Styled Text, Styled Paragraph) plus a full set of single-trait atomic
  components (bold, italic, underline, strikethrough, font size, font family, color, highlight,
  link, baseline for Text; heading, alignment, line spacing, indent, space above/below for
  Paragraph), plus composite report helpers (Report Skeleton, Report Header Block, Bullet List).
- **Insert tab:** self-contained inserts — inline images (from a URL, a captured Rhino viewport,
  or a hosted local file), image replacement by object ID, page breaks, section breaks.
- **Table tab:** build a populated table directly from a Grasshopper data tree, either
  size-inferred (Data Tree To Table) or with explicit row/column counts (Fixed Size Table).
- **Combine tab:** Arrange Content sequences self-indexed blocks into real document positions;
  Request Aggregator gathers everything into one batch payload.
- **Presets tab:** curated value lists for named styles, bullet glyphs, alignment, and font family.
- **Segment targeting:** any Text- or Paragraph-tab component can target a header, footer, or
  footnote instead of the document body via an optional Segment ID input.
- Document-by-name lookup (via Drive `files.list`) is available on Get Document but not yet
  validated against a live Drive-scoped token — see [authentication.md](authentication.md) for the
  scope it needs.

### Known limitations

- The Table tab does not support editing an existing table's rows, columns, or merged cells — only
  building a new table from data. Editing a live table would need structural indices read back from
  a fetched document, which isn't exposed yet.
- There is no Insert Table or named-range support in the JSON/self-indexed model: Google doesn't
  publish an index-delta formula for a table's structural growth, and named ranges require real
  indices into already-placed content rather than a position to insert at — neither fits self-indexed
  sequencing. Build a table from data instead via Data Tree To Table / Fixed Size Table.
- There's no "Page X of Y" auto-incrementing field — the Docs API doesn't expose one. A footer can
  still hold a static string via any Text/Paragraph component targeting its Segment ID.
