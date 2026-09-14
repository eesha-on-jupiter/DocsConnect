<!-- pr:begin header -->
# DocsConnect — CLAUDE.md

Generated Grasshopper REST plugin built with the **PleaseREST** pipeline (`please-rest` plugin, v1.0.0).

- **Commands:** `/rest-status` (progress tree), `/rest-resume-plugin`, `/rest-add-feature`, `/rest-build-component <id>`, `/rest-icons`, `/rest-package`, `/rest-test-api`
- **Skills:** `pr-conventions`, `pr-component`, `pr-button`, `pr-builders`, `pr-client`, `pr-preset`, `pr-bulk-data`, `pr-unit-tests`, `pr-build`, `pr-docs`, `pr-gh-package`, `pr-yak-package`, `pr-ci-template`, `pr-rhino-test`, `pr-session`, `pr-dogfood`
- **Agents:** `pr-api-scout`, `pr-aec-advisor`, `pr-spec-critic`
- **MCPs:** `pr-api-extractor`, `pr-api-tester`, `pr-icon-generator`; `cordyceps` (Grasshopper bridge for live canvas tests)
- **Build:** `dotnet build DocsConnect.csproj` (net48 / net7.0-windows / net7.0). Grasshopper loads `bin/Debug/net48/DocsConnect.gha` via its developer-folder setting — **close Rhino before building** (it locks the file) and restart it to pick up a build.
- **Tests:** `dotnet test DocsConnect.Tests/DocsConnect.Tests.csproj` — pure builder logic, net8.0, no Rhino needed.
<!-- pr:end header -->

<!-- pr:begin state -->
## Current build state

- **Version:** 1.0.0 — pre-release; version tracking starts with the first GitHub release
- **Phase:** 14 — GitHub / packaging. Code, unit tests (153) and `docs/` are complete.
- **Last completed step:** Placement rework — Request Aggregator absorbed Arrange Content (deleted); block splitting for merged lists; per-insert style isolation; segment start at 0; break/tab/CRLF growth fixes; unplaced-block detection; per-cell table formatting trees + Transpose; list-native image chain; placement sweep tests across every content-emitting builder.
- **Next step:** demo `.gh` files (user-owned — do not touch `demos/`), Gate 6 live re-verification of the current build, `/rest-package` for `.yak`, GitHub publish.
- **Last active:** 2026-09-13T17:38:54
<!-- pr:end state -->

<!-- pr:begin platform -->
## Platform context

- **API:** Google Docs API v1 — `https://docs.googleapis.com` — reference: https://developers.google.com/docs/api/reference/rest
- **Also:** Google Drive API v3 (`files.list` for lookup by name; multipart upload + `permissions.create` for image hosting)
- **Auth:** OAuth 2.0 Bearer token supplied by the user (`Authorization: Bearer {token}`); the plugin runs no OAuth flow. Scopes: `documents`; `drive.file` (image upload); `drive.readonly` (name lookup).
- **Resource model:** one document; content addressed by UTF-16 index inside an index space — the body (starts at 1, index 0 is its section break) or a header/footer/footnote segment (starts at 0; a new footnote holds `" \n"`). Everything is written through `documents.batchUpdate`, which applies requests in array order, atomically.
<!-- pr:end platform -->

<!-- pr:begin structure -->
## Plugin structure

Tab `DocsConnect`, 46 components. HTTP components (Connect, Document) call the API; write operations are `ButtonComponent`s that fire only on a click. JSON components (Text, Paragraph, Insert, Table, Combine, Update Document Style) emit `Request JSON` and never touch the network.

**Connect** (1)

- `DocsConnectAuth` — **Connect** (`Auth`): Token → Status, Response

**Document** (8)

- `BatchUpdateDocument` — **Batch Update** (`Apply`): Token, Document ID, Requests JSON → Status, Response
- `ClearDocument` — **Clear Document** (`Clear`): Token, Document ID → Status, Response
- `CreateDocument` — **Create Document** (`Create`): Token, Title → Status, Response, Document ID
- `CreateFooterJson` — **Create Footer** (`Footer`): Token, Document ID, Text → Status, Response, Footer ID
- `CreateFootnoteJson` — **Create Footnote** (`Footnote`): Token, Document ID, Text → Status, Response, Footnote ID
- `CreateHeaderJson` — **Create Header** (`Header`): Token, Document ID, Text → Status, Response, Header ID
- `GetDocument` — **Get Document** (`Get`): Token, Document ID, Name → Status, Response, Title, Document ID, End Index
- `UpdateDocumentStyleJson` — **Update Document Style** (`DocStyle`): Margin Top, Margin Bottom, Margin Left, Margin Right, Page Width, Page Height, Background → Request JSON

**Text** (13)

- `BaselineTextJson` — **Baseline Text** (`Baseline`): Text, Baseline, Segment ID → Request JSON
- `BoldTextJson` — **Bold Text** (`Bold`): Text, Segment ID → Request JSON
- `FontFamilyTextJson` — **Font Family Text** (`FontFam`): Text, Font Family, Segment ID → Request JSON
- `FontSizeTextJson` — **Font Size Text** (`FontSz`): Text, Font Size, Segment ID → Request JSON
- `HighlightTextJson` — **Highlight Text** (`Highlight`): Text, Highlight, Segment ID → Request JSON
- `InsertTextJson` — **Insert Text** (`InsTxt`): Text, Segment ID → Request JSON
- `ItalicTextJson` — **Italic Text** (`Italic`): Text, Segment ID → Request JSON
- `LinkTextJson` — **Link Text** (`Link`): Text, URL, Segment ID → Request JSON
- `ReplaceAllTextJson` — **Replace All Text** (`Replace`): Find, Replace, Match Case → Request JSON
- `StrikethroughTextJson` — **Strikethrough Text** (`Strike`): Text, Segment ID → Request JSON
- `UpdateTextStyleJson` — **Styled Text** (`TxtStyle`): Text, Bold, Italic, Underline, Strikethrough, Font Size, Font Family, Text Color, Highlight, Link URL, Baseline, Segment ID, Same Paragraph, Separator → Request JSON
- `TextColorTextJson` — **Text Color** (`TxtColor`): Text, Color, Segment ID → Request JSON
- `UnderlineTextJson` — **Underline Text** (`Underline`): Text, Segment ID → Request JSON

**Paragraph** (11)

- `AlignedTextJson` — **Aligned Text** (`Aligned`): Text, Alignment, Segment ID → Request JSON
- `CreateParagraphBulletsJson` — **Bullet Item Text** (`Bullets`): Text, Bullet Preset, Segment ID → Request JSON
- `BulletList` — **Bullet List** (`BulletList`): Items, Indent Levels, Bullet Preset → Request JSON
- `HeadingTextJson` — **Heading Text** (`Heading`): Text, Named Style, Segment ID → Request JSON
- `IndentTextJson` — **Indent Text** (`Indent`): Text, Indent, Segment ID → Request JSON
- `LineSpacingTextJson` — **Line Spacing Text** (`LineSp`): Text, Line Spacing, Segment ID → Request JSON
- `ReportHeaderBlock` — **Report Header Block** (`HdrBlock`): Project Name, Project Number, Date, Author, Revision, Layout → Request JSON
- `ReportSkeleton` — **Report Skeleton** (`Report`): Title, Headings, Heading Levels, Bodies → Request JSON
- `SpaceAboveTextJson` — **Space Above Text** (`SpaceAbv`): Text, Space Above, Segment ID → Request JSON
- `SpaceBelowTextJson` — **Space Below Text** (`SpaceBlw`): Text, Space Below, Segment ID → Request JSON
- `UpdateParagraphStyleJson` — **Styled Paragraph** (`ParaStyle`): Text, Named Style, Alignment, Line Spacing, Indent, Space Above, Space Below, Segment ID → Request JSON

**Insert** (6)

- `CaptureRhinoView` — **Capture Rhino View** (`Capture`): View Name, File Path, Width, Height → File Path
- `GetImageUrl` — **Get Image URL** (`ImgURL`): Token, Bitmap, File Path, URL → Image URL
- `InsertInlineImageJson` — **Insert Image** (`InsImg`): Image URL, Width, Height, Own Paragraph → Request JSON
- `InsertPageBreakJson` — **Insert Page Break** (`PgBreak`): Count → Request JSON
- `InsertSectionBreakJson` — **Insert Section Break** (`SecBreak`): Section Type → Request JSON
- `ReplaceImageJson` — **Replace Image** (`RepImg`): Image Object ID, Image URL → Request JSON

**Table** (2)

- `DataTreeToTable` — **Data Tree To Table** (`Tree2Tbl`): Data, Header Row, Bold Header, Bold, Italic, Underline, Font Size, Font Family, Text Color, Fill, Transpose → Request JSON
- `FixedSizeTable` — **Fixed Size Table** (`FixedTbl`): Data, Rows, Columns, Header Row, Bold Header, Bold, Italic, Underline, Font Size, Font Family, Text Color, Fill → Request JSON

**Combine** (1)

- `RequestAggregator` — **Request Aggregator** (`Aggregate`): Request JSON, Start Index, Fixed Requests → Requests JSON

**Presets** (4)

- `AlignmentPreset` — **Alignment** (`AL`): — → value list
- `BulletGlyphPreset` — **Bullet Preset** (`BP`): — → value list
- `FontFamilyPreset` — **Font Family** (`FF`): — → value list
- `NamedStyleTypePreset` — **Named Style** (`NS`): — → value list
<!-- pr:end structure -->

<!-- pr:begin builders -->
## JSON builders (`JSON Builders/`)

- `SharedBuilders.cs` — `Wrap`, `Range`/`Location` (with segmentId), colour + `Pt` helpers, `FlattenRequests`, `NormalizeLineEndings` (Docs drops `\r`). Used by every builder.
- `TextRequestBuilders.cs` — `InsertText`, `InsertTextLines`, `InsertStyledTextLines`, `InsertStyledRun`, `UpdateTextStyle` (fields mask from supplied traits), `ReplaceAllText`, `GetOrLast`. Text tab.
- `ParagraphRequestBuilders.cs` — `UpdateParagraphStyle`, `CreateParagraphBullets` (+ `BulletPresets` / `IsBulletPreset`), `DeleteParagraphBullets`, `InsertStyledParagraphLines`, `InsertBulletedParagraphLines`. Paragraph tab.
- `InsertRequestBuilders.cs` — `InsertInlineImage(s)`, `InsertPageBreak(s)`, `InsertSectionBreak(s)`, `InsertTable`, `ReplaceImage`. Insert tab.
- `StructureRequestBuilders.cs` — `createHeader` / `createFooter` / `createFootnote` bodies. Document tab live writes.
- `AecHelperBuilders.cs` — composites: `ReportHeaderBlock`, `ReportSkeleton`, `BulletList` (tab-nested), `DataTreeToTable` / `FixedSizeTable` → `BuildTable` (live-verified cell-offset math, per-cell `CellFormat` → `updateTextStyle` + `updateTableCellStyle`), `UpdateTableCellFill`.
- `BlockBuilders.cs` — chaining and cells: `ExtractSpans`, `AppendTextStyle` / `AppendParagraphStyle` / `AppendBullets`, `IsRequestJson`, `ExpandCells` (a row's request items → one cell per formatted line), `ExtractCellContent`.
- `DocumentBuilders.cs` — the placement engine: `ReindexBlocks` (+ `SplitSelfIndexedBlocks`, `StyleResetRequests`), `ContentLength` with the per-request growth constants (`InlineImageLength` 1, `PageBreakLength` 2, `SectionBreakLength` 2, `TableLength`, bullets minus stripped tabs), `BodyStartIndex` / `SegmentStartIndex` / `FootnoteStartIndex`, `IndexSpaces`, `CountContentInserts`, `CountInsertsAtSegmentStart`, `BodyEndIndex`, `AssembleRequestsArray`. Used by Request Aggregator, Batch Update, Get Document, Clear Document, Create Header/Footer/Footnote.
- `Utilities/BlockChaining.cs` — canvas glue for the chain input every Text/Paragraph component registers last. `Utilities/GHDataHelpers.cs` — `FlattenText/Number/Integer`, `CellValue` (cell-matched tree lookup), `TreeOrNull`, `WarnIfIterated`.
<!-- pr:end builders -->

<!-- pr:begin decisions -->
## Key design decisions

- **Index-free authoring.** No content component takes a document index. Every block is self-indexed from 1 and outputs a *list*; **Request Aggregator** is the only place real positions are computed. It reads each wire as a block, splits a merged list back into blocks wherever an insert restarts at index 1, shifts each block by the running content tally, then appends `Fixed Requests` (Replace Image, Replace All Text, Update Document Style, explicit-index style requests) untouched. One aggregator per index space; mixing spaces is refused. Start Index 1 means "the top" of whichever space the blocks address (0 for segments).
- **Growth constants come from the API reference, in one place** (`DocumentBuilders`): page/section break = 2, inline image = 1, table = newline + structure, `createParagraphBullets` removes the leading tabs it read nesting from, `\r` is dropped. `PlacementSweepTests` runs every content-emitting builder through the placement contract.
- **Style isolation.** After every placed `insertText` the aggregator resets the inserted range (Normal text, alignment/spacing/indents cleared, bullets removed, `updateTextStyle` with `fields: "*"`) before the block's own styling — Docs otherwise makes inserted text inherit the paragraph it lands in. Same-paragraph runs get only the text reset.
- **Unplaced blocks are caught**: a content insert in `Fixed Requests` is an error; more than one insert at a segment's start in Batch Update is a warning. Every request component warns when Grasshopper re-solves it (a list on a single-value input duplicates the whole block).
- **Style chaining**: every Text/Paragraph component's optional `Request JSON` input layers its trait onto the incoming block's spans as zero-length requests, so chained blocks place like unchained ones.
- **Tables**: cells accept plain text, a style component's output (one cell per formatted line), or matching format trees (`Bold`…`Fill`); Fixed Size Table reads a flat branch row-major; Data Tree To Table has `Transpose` for column-wise data. Cells are filled highest-index-first, styles applied at final positions, chained cell styles win over format trees which win over `Bold Header`.
- **Writes only on click** (`ButtonComponent`); Create Header/Footer/Footnote are live so the server-generated segment ID can feed Segment ID inputs.
<!-- pr:end decisions -->

<!-- pr:begin limitations -->
## Known limitations

- Appending below *existing* content inside a header/footer/footnote: Get Document's `End Index` is body-only.
- Table row/column/cell editing of an existing table (insert/delete row/column, merge cells) is out of scope — needs live structural indices.
- Named ranges, positioned (floating) objects, "Page X of Y" fields — not in the Docs request set or not authorable index-free.
- Cell background fill (`updateTableCellStyle`) and the `fields: "*"` style reset are built to the API reference but were not exercised against the live API from this environment.
- `Same Paragraph` runs (Styled Text with `New Paragraph After = false`) deliberately join the following paragraph; a block placed right after one inherits that paragraph's style by design.
- Tokens from the OAuth Playground expire after ~1 hour; the plugin does not refresh them.
<!-- pr:end limitations -->

<!-- pr:begin todo -->
## Todo (spec/todo.md)

# DocsConnect — Build TODO

> Regenerated 2026-09-13 from the built plugin · 46 components (incl. 4 presets) · Google Docs API (OAuth Bearer)
> Tags: [CODE] write/edit · [TEST] test file · [ASSET] icon · [MANUAL] human action
> Icon prefix: GDC_

---

## PHASE 1 — AUTH
[x] DocsConnectClient.cs       [CODE] Bearer auth, base HTTP, batchUpdate colon-path, Drive upload + permissions.create
[x] DocsConnectAuth.cs         [CODE] Token → tokeninfo → Status (granted scopes), Response
[x] DocsConnectInfo.cs / PluginUtilities.cs / ButtonComponent.cs
[x] Verify: live tokeninfo                                   ← [HUMAN GATE 5] passed

## PHASE 2 — DOCUMENT (HTTP)
[x] DocumentBuilders.cs             [CODE] requests-array assembly, BodyEndIndex, placement engine (ReindexBlocks + growth constants + sanity checks)
[x] GetDocument / CreateDocument / BatchUpdateDocument / ClearDocument
[x] CreateHeaderJson / CreateFooterJson / CreateFootnoteJson   (live writes; convenience Text inserts at segment start 0 / footnote 1)
[x] UpdateDocumentStyleJson         (index-free → Fixed Requests)

## PHASE 3 — REQUEST UNITS (JSON), all self-indexed from 1, list output, chain input registered last
### Text (13)
[x] InsertText, Styled Text (UpdateTextStyle), Bold, Italic, Underline, Strikethrough, Font Size, Font Family, Text Color, Highlight, Link, Baseline, Replace All Text
### Paragraph (11)
[x] Styled Paragraph (UpdateParagraphStyle), Heading, Aligned, Line Spacing, Indent, Space Above, Space Below, Bullet Item Text (CreateParagraphBullets, preset validated), Bullet List (nested via tabs), Report Skeleton, Report Header Block
### Insert (6)
[x] Insert Image (list, Own Paragraph), Insert Page Break (+2), Insert Section Break (+2), Replace Image, Capture Rhino View (list of views), Get Image URL (lists)
### Table (2)
[x] Data Tree To Table (formatted cells from a style component's output, format trees Bold…Fill, Transpose), Fixed Size Table (flat branch row-major, format trees)
### Combine (1)
[x] Request Aggregator              [CODE] one wire = one block, merged-list splitting, Start Index (1 = top; 0 for segments), Fixed Requests, style isolation, mixed-space refusal, misroute error
### Presets (4)
[x] Named Style Type, Bullet Glyph, Alignment, Font Family

## PHASE 4 — QUALITY
[x] Style chaining (Utilities/BlockChaining.cs + BlockBuilders.cs)
[x] Line-ending normalisation, bullet-tab subtraction, break sizes from the API reference
[x] Iteration guard (WarnIfIterated) on every request component
[x] Unplaced-block detection (aggregator error, Batch Update warning)
[x] Unit tests — 153 passing (BuilderTests, BulkAssemblyTests, BlockChainingTests, PipelineIntegrationTests, PlacementSweepTests)
[x] Icons                           [ASSET] all components (Formatted Table removed; GDC_AppendCursor resource unused)

## PHASE 5 — DOCS
[x] docs/ — index, quickstart, authentication, chaining, examples, per-component pages, changelog
[x] README.md, CLAUDE.md, LICENSE, .gitignore

## PHASE 6 — SHIP
[ ] demos/*.gh                      [MANUAL] user-owned — rebuild against 1.1.0 (Request Aggregator replaces Arrange Content)
[ ] Live re-verification of the build  [MANUAL] ← [HUMAN GATE 6] header/footer content, nested bullets, image list, table fills, style-reset `fields:"*"`
[ ] /rest-package                   [CODE] .yak for net48 / net7.0-windows / net7.0 + publish workflow
[ ] GitHub publish                  [MANUAL] ← [HUMAN GATE 7]

<!-- pr:end todo -->
