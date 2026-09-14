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
[ ] demos/*.gh                      [MANUAL] user-owned — rebuild against the current build (Request Aggregator replaces Arrange Content)
[ ] Live re-verification of the current build   [MANUAL] ← [HUMAN GATE 6] header/footer content, nested bullets, image list, table fills, style-reset `fields:"*"`
[x] /rest-package                   [CODE] .yak for net48 / net7.0-windows / net7.0 + publish workflow — dist/ built 2026-09-13
[x] GitHub publish                  [MANUAL] ← [HUMAN GATE 7] repo live 2026-09-13; Food4Rhino listing published 2026-09-13 (zips of .gha + dlls); yak push not yet done
