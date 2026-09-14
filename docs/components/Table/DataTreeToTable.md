# Data Tree To Table

Builds a table from a Grasshopper data tree, inferring the table's row and column count from the tree's shape.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Data | string (tree) | Yes | — | A Grasshopper data tree where each branch becomes a table row and each item within a branch becomes a cell in that row. A cell may be plain text, or the Request JSON of any Text/Paragraph component — see [Formatted cells](#formatted-cells). |
| Header Row | bool | Yes | true | Whether the first row of the table is treated as a header row. |
| Bold Header | bool | Yes | true | Whether the header row's text is bolded. |
| Bold | bool (tree) | No | — | Bold per cell, matched to Data. |
| Italic | bool (tree) | No | — | Italic per cell, matched to Data. |
| Underline | bool (tree) | No | — | Underline per cell, matched to Data. |
| Font Size | number (tree) | No | — | Font size in points per cell, matched to Data (>0 to apply). |
| Font Family | string (tree) | No | — | Font family per cell, matched to Data (blank to skip). |
| Text Color | string (tree) | No | — | Text colour as `#RRGGBB` per cell, matched to Data (blank to skip). |
| Fill | string (tree) | No | — | Cell background colour as `#RRGGBB` per cell, matched to Data (blank to skip). |
| Transpose | bool | Yes | false | Read each branch as a **column** instead of a row. `Entwine(labels, values)` then gives a labels column beside a values column. Format trees follow the same orientation. |

## Outputs

| Name | Type | Description |
|---|---|---|
| Request JSON | string (list) | A JSON array string containing the `insertTable` request plus the sequenced cell `insertText` requests that fill it. |

## Output

This component never calls the API directly — it emits a Request JSON list (an InsertTable request plus sequenced cell InsertText requests, self-indexed from 1) that must be wired into [Request Aggregator](../Combine/RequestAggregator.md)'s `Request JSON` input (on its own wire, in the order it should appear, to get a real document position) before [Batch Update](../Document/BatchUpdateDocument.md) can push it live.

## Notes

### Formatting trees

The seven format inputs are trees matched to `Data` **cell for cell**: row = branch, column = item. A shorter list repeats its last value in both directions — one item formats every cell, one branch formats every row, a full tree formats each cell individually — and a single flat branch longer than one row is read row-major, mirroring Fixed Size Table's flat Data form. Leave an input unwired to not touch that property. `Fill` is the cell background (`updateTableCellStyle`); `Text Color` is the text itself.

Typical wiring for a data table with a coloured status column:

```
Data        {0}: Item, Qty, Status      Fill        {0}: "", "", ""
            {1}: Beam, 12, OK                       {1}: "", "", "#C6EFCE"
            {2}: Column, 8, CHECK                   {2}: "", "", "#FFEB9C"
Bold        true                        (one value → every cell)
```

Format-tree styling is applied after `Bold Header` (an explicit format wins over the header default) and before any styling a cell carries as Request JSON (a chained block's own traits win over the tree). The tree route and the Request JSON route can be mixed freely.

### Columns instead of rows

Grasshopper data usually comes one list per attribute — a column — rather than one list per row. Set `Transpose = true` and each branch becomes a column: `Entwine(labels, values)` produces a two-column table with as many rows as the longest list, shorter columns padded with blank cells. This is the natural way to put a coloured Text Color output (one coloured line per value) next to its labels. The format trees are read in the same orientation as Data.

### Formatted cells

A cell does not have to be a plain string. Wire the `Request JSON` of any Text- or Paragraph-tab component — Bold Text, Text Color, Font Size, or a chain of them — into the `Data` tree alongside (or instead of) plain strings. A component's output is a list of requests, one per line it inserted plus its styles; the table combines the request items in a row back into blocks and turns **each formatted line into one cell**, carrying the style requests that overlap that line. So Text Color with `Text = {Red, Green, Blue}` and `Color = {#C00000, #006600, #000099}` wired into a row gives three cells, each in its own colour. Plain strings in the same row stay one cell each, in order, and two components wired into the same row stay apart (each starts fresh at index 1).

The block's trailing paragraph break is stripped — a cell already ends in its own paragraph — and any style range that ran past it is clamped back. A string that isn't Docs request JSON (including a bare number, or text that happens to parse as JSON) is used verbatim. To build a whole coloured table from one Text Color component, use [Fixed Size Table](FixedSizeTable.md), which cuts a single flat branch into rows.

Styles are applied *after* every cell fill, at each cell's final index, so a cell's own formatting is never thrown off by the text in the cells before it. Where `Bold Header` overlaps a header cell's own styling, the cell's styling wins.

The table's shape is inferred entirely from the tree: the number of branches sets the row count, and the number of items in each branch sets the column count. There's no separate Rows/Columns input to keep in sync with the data — the tree is the source of truth.

If an explicit row and column count is needed instead (for example, to force a fixed-width table regardless of how much data is supplied), use [Fixed Size Table](FixedSizeTable.md) instead.

## Related

- [Fixed Size Table](FixedSizeTable.md) — same composite pattern, but with an explicit Rows x Columns size instead of one inferred from the tree.
