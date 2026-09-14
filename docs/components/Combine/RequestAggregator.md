# Request Aggregator

Places self-indexed Request JSON blocks into real document positions and gathers them, with any fixed requests, into the single Requests JSON array Batch Update sends.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Request JSON | string (tree) | No | "" | The blocks to place, in the order they should appear — either one wire per component, or several components through a **Merge** (D1, D2, D3 order). Blocks that arrive in one list are told apart because each one starts fresh at index 1. |
| Start Index | int | Yes | 1 | The index the first block lands at. Leave at `1` for the top of the body **or** of a header/footer/footnote — segments actually start at index 0 (no section break in front of them), and the aggregator uses 0 when its blocks carry a Segment ID. Any other explicit value is used as given. Wire [Get Document](../Document/GetDocument.md)'s `End Index` in to append below existing body content. |
| Fixed Requests | string (tree) | No | "" | Requests that already carry a real position, or none at all. Passed through untouched, after the placed blocks. |

## Outputs

| Name | Type | Description |
|---|---|---|
| Requests JSON | string | A single JSON string containing the re-indexed blocks followed by the fixed requests, ready for [Batch Update Document](../Document/BatchUpdateDocument.md). |

## Output

Request Aggregator is the one mandatory hub between authoring content and pushing it live. Every Text-tab and Paragraph-tab component, the Table tab's composites (Data Tree To Table, Fixed Size Table), and the Insert tab's self-indexed components (Insert Image, Insert Page Break, Insert Section Break) author their request(s) at a placeholder position of 1 — none of them carries a real document index. Request Aggregator reads each wire into `Request JSON` as its own block, in the order the wires were made, tallies how much content each block adds or removes, and rewrites each block's indices to where it will actually land once every earlier block has been applied.

`Fixed Requests` is for everything else: [Replace Image](../Insert/ReplaceImageJson.md) (targets an image by object ID), [Replace All Text](../Text/ReplaceAllTextJson.md), [Update Document Style](../Document/UpdateDocumentStyleJson.md), [Create Header](../Document/CreateHeaderJson.md) / [Create Footer](../Document/CreateFooterJson.md), and the explicit-index components [Update Text Style](../Text/UpdateTextStyleJson.md), [Update Paragraph Style](../Paragraph/UpdateParagraphStyleJson.md) and [Create Paragraph Bullets](../Paragraph/CreateParagraphBulletsJson.md). These are appended verbatim after the placed blocks — nothing in them is shifted.

The component's canvas label reports what it found — `3 blocks`, or `3 blocks + 1 fixed` — so a miswire is visible without opening a panel.

## Notes

### Why each wire is its own block

Grasshopper merges everything arriving at one input: three components each emitting a flat list at path `{0}` reach the parameter as a *single* branch with all their requests concatenated. Reindexed as one block, every request would keep its placeholder index of 1, and Google applies requests in array order — so each insert would land at the top of the document, above the one before it. The visible result is content stacked in reverse (the heading ends up at the bottom), and, because each block is then inserted *inside* the previous block's first paragraph, that paragraph's style — heading level, alignment, bullets — is inherited by everything inserted after it.

Request Aggregator guards against this twice. It reads its wires directly rather than the merged tree, so one wire is one block. And within any single list — a **Merge**, a Relay, internalised data — it splits blocks apart again on its own: every component authors its first insert at index 1 and its indices only grow from there, so an insert landing at 1 after the current block has already inserted something can only be the start of the next block. Either way the blocks are placed one after another, in the order they arrived. The label counts the blocks it found after splitting.

### Nothing inherits from the document

Google Docs gives inserted text the paragraph style, bullets and text style of the position it lands at — so a block placed at index 1 of a document whose first paragraph is a centered H1 would come out as a centered H1 too, and a block appended after a bulleted line would be bulleted. Request Aggregator prevents this: every `insertText` it places is followed by a reset over exactly the inserted range (paragraph style back to Normal text with alignment, spacing and indents cleared; bullets removed; all text properties reset) *before* the block's own style requests, so each block is formatted only by what its component and any chained traits set. A same-paragraph run (text without a paragraph break) gets only the text reset, so it never restyles the paragraph it joins.

This does not clear the document. Re-running a definition with `Start Index = 1` still stacks the new run above the old one — use [Clear Document](../Document/ClearDocument.md) first, or wire Get Document's `End Index` into `Start Index` to append.

### One instance per segment

The document body has its own index space, and each header, footer, or footnote segment has its own independent index space too. Mixing blocks that target different segments into a single Request Aggregator run would cross-contaminate the content-length tally, so the aggregator refuses it with an error. In practice: one Request Aggregator for body content, and a separate one for each header/footer/footnote segment in play. Their outputs can all go into Batch Update's `Requests JSON` list together.

### Content tally

`insertText` counts as the length of the inserted text; `insertInlineImage` counts as +1; `insertPageBreak` and `insertSectionBreak` each count as +2 (Google adds a newline after a page break and before a section break); `insertTable` counts as its structural footprint, `rows * (2 * columns + 1) + 3` (a paragraph before the table, the table start marker, one marker per row plus two indices per cell, and the table end marker) on top of whatever text its cell fills carry; `deleteContentRange` subtracts the size of the deleted range; `createParagraphBullets` subtracts the leading tabs of every paragraph in its range, because Google removes the tabs it read the nesting level from. Style requests (`updateTextStyle`, `updateParagraphStyle`) contribute zero, which is what lets [chained](../../chaining.md) traits travel with their block.

## Related

- [Get Document](../Document/GetDocument.md) — its `End Index` output is the Start Index to use when appending below existing content.
- [Batch Update Document](../Document/BatchUpdateDocument.md) — consumes this component's Requests JSON output.
- [Create Header](../Document/CreateHeaderJson.md), [Create Footer](../Document/CreateFooterJson.md), [Create Footnote](../Document/CreateFootnoteJson.md) — provide the Header ID / Footer ID / Footnote ID that make targeting a segment possible, and that's what makes a separate Request Aggregator instance per segment necessary.
- [chaining.md](../../chaining.md) — layering several traits onto one block before it reaches this component.
