# Chaining styles

Every Text- and Paragraph-tab component has an optional **Request JSON** input. Leave it empty and
the component behaves exactly as it always has — it inserts its own text and styles it. Wire another
component's `Request JSON` output into it and the component switches to **style-only mode**: it
skips its own insert and layers its trait onto the incoming block's existing text.

That's how you get one heading that is also colored, instead of two paragraphs.

## The problem it solves

Each Text/Paragraph component authors its own `insertText`. So wiring the same string into Heading
Text and into Text Color used to insert it twice — once as a heading, once colored — as two separate
paragraphs. [Styled Text](components/Text/UpdateTextStyleJson.md) and
[Styled Paragraph](components/Paragraph/UpdateParagraphStyleJson.md) existed to work around this by
bundling many traits into one component, but they can't mix text traits with paragraph traits, and
they can't add a trait to a composite like [Report Skeleton](components/Paragraph/ReportSkeleton.md).

Chaining removes the limit: build up whatever combination you want, one component at a time.

## How to wire it

```
[Heading Text] ─RJ→ [Text Color] ─RJ→ [Aligned Text] ─RJ→ [Space Below] ─RJ→ [Request Aggregator] → Batch Update
   TX "Results"        FC #C00000        AL CENTER          SB 12
   NS HEADING_1
```

Only the first component in the chain takes `Text`. Everything after it takes the block. The last
one's `Request JSON` output goes into Request Aggregator exactly like an unchained block would — one
branch, one block, still self-indexed from 1.

## Why it doesn't break placement

[Request Aggregator](components/Combine/RequestAggregator.md) shifts every index in a branch by one shared
offset and advances its cursor by the block's *content length*. Content length counts `insertText`
(text length), `insertInlineImage` / `insertPageBreak` / `insertSectionBreak` (+1 each), and
`deleteContentRange` (negative). Style requests count **zero**.

So an appended `updateTextStyle` adds no length, and nothing downstream of the chained block moves.
The appended requests also sit in the same branch as the block they style, so they shift with it.

Google applies requests in array order and chained traits are appended after the block, so a later
component in the chain wins if two of them set the same field.

## Rules

**Only styles chain.** `updateTextStyle`, `updateParagraphStyle` and `createParagraphBullets` are
zero-length and can be layered on. Anything that adds content — Insert Text, Insert Image, page and
section breaks — has to stay its own block, sequenced through Request Aggregator. That's the split:
chain *styles* onto a block, chain *blocks* through Request Aggregator.

**Text is ignored while a block is connected.** The component warns if you leave something wired
into `Text`, so a chained component never silently inserts a stray copy.

**Segment ID is inherited.** The incoming block already knows whether it targets the body or a
header/footer/footnote, so the chained component reuses it. Setting Segment ID on a chained
component does nothing and reports a remark saying so.

**One trait per span, index-matched.** A block with three lines has three spans. A single value
covers all three (the usual "a shorter list repeats its last value" rule); a three-item list styles
each line separately. This works on multi-paragraph composites too — chain a color onto
[Report Skeleton](components/Paragraph/ReportSkeleton.md) and every paragraph takes it, or feed a
per-paragraph list to color them individually.

**Blocks that insert no text can't be chained onto.** Replace All Text, Insert Image and the break
components have no text span to target; chaining onto one reports an error.

**A "Same Paragraph" run is one span.** [Styled Text](components/Text/UpdateTextStyleJson.md) with
Same Paragraph on emits a single `insertText` for the whole joined run, so a chained trait applies
to all of it uniformly rather than per item.

## Chainable components

Every component on the [Text](components/Text/index.md) and
[Paragraph](components/Paragraph/index.md) tabs that inserts and styles text:

| Tab | Components |
|---|---|
| Text | Styled Text, Bold, Italic, Underline, Strikethrough, Font Size, Font Family, Text Color, Highlight, Link, Baseline |
| Paragraph | Styled Paragraph, Heading, Aligned, Line Spacing, Indent, Space Above, Space Below, Create Paragraph Bullets |

Insert Text and Replace All Text (Text tab), and the Report Skeleton / Report Header Block / Bullet
List composites (Paragraph tab), have no chain input — they are sources you chain *from*, not
stylers you chain *into*.

## Examples

**A colored, centered heading**
```
[Heading Text] NS=HEADING_1 ─RJ→ [Text Color] FC=#C00000 ─RJ→ [Aligned Text] AL=CENTER ─RJ→ Request Aggregator
```

**A report skeleton in a custom font**
```
[Report Skeleton] ─RJ→ [Font Family] FF="Source Serif Pro" ─RJ→ Request Aggregator
```

**A bulleted, indented, italic list**
```
[Italic Text] TX={a,b,c} ─RJ→ [Create Paragraph Bullets] BP=BULLET_CHECKBOX ─RJ→ [Indent Text] IN=36 ─RJ→ Request Aggregator
```

**Per-line colors across a heading block**
```
[Heading Text] TX={"One","Two","Three"} ─RJ→ [Text Color] FC={#C00000,#006600,#000099} ─RJ→ Request Aggregator
```
