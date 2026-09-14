# Fixed Size Table

Builds a table with an explicit row and column count, padding or dropping cells as needed to fit the supplied data.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Data | string (tree) | Yes | — | A Grasshopper data tree where each branch becomes a table row and each item within a branch becomes a cell in that row. A cell may be plain text, or the Request JSON of any Text/Paragraph component — see [Formatted cells](DataTreeToTable.md#formatted-cells). |
| Rows | int | Yes | 1 | The number of rows the table will have. |
| Columns | int | Yes | 1 | The number of columns the table will have. |
| Header Row | bool | Yes | true | Whether the first row of the table is treated as a header row. |
| Bold Header | bool | Yes | true | Whether the header row's text is bolded. |
| Bold | bool (tree) | No | — | Bold per cell, matched to Data. |
| Italic | bool (tree) | No | — | Italic per cell, matched to Data. |
| Underline | bool (tree) | No | — | Underline per cell, matched to Data. |
| Font Size | number (tree) | No | — | Font size in points per cell, matched to Data (>0 to apply). |
| Font Family | string (tree) | No | — | Font family per cell, matched to Data (blank to skip). |
| Text Color | string (tree) | No | — | Text colour as `#RRGGBB` per cell, matched to Data (blank to skip). |
| Fill | string (tree) | No | — | Cell background colour as `#RRGGBB` per cell, matched to Data (blank to skip). |

## Outputs

| Name | Type | Description |
|---|---|---|
| Request JSON | string (list) | A JSON array string containing the `insertTable` request plus the sequenced cell `insertText` requests that fill it. |

## Output

This component never calls the API directly — it emits a Request JSON list (an InsertTable request plus sequenced cell InsertText requests, self-indexed from 1) that must be wired into [Request Aggregator](../Combine/RequestAggregator.md)'s `Request JSON` input (on its own wire, in the order it should appear, to get a real document position) before [Batch Update](../Document/BatchUpdateDocument.md) can push it live.

## Notes

### Formatting trees

The seven format inputs are trees matched to `Data` **cell for cell**: row = branch, column = item. A shorter list repeats its last value in both directions — one item formats every cell, one branch formats every row, a full tree formats each cell individually — and a single flat branch longer than one row is read row-major (the same flat form `Data` accepts here). Leave an input unwired to not touch that property. `Fill` is the cell background (`updateTableCellStyle`); `Text Color` is the text itself.

Format-tree styling is applied after `Bold Header` (an explicit format wins over the header default) and before any styling a cell carries as Request JSON (a chained block's own traits win over the tree). The two routes can be mixed freely.

Cells accept formatted content as well as plain text: wire any Text- or Paragraph-tab component's `Request JSON` into the `Data` tree and each formatted line becomes one cell with its styling. **Whole table from one component:** a single flat branch holding more cells than `Columns` is read in reading order and cut into rows of `Columns` — so one Text Color with every cell's text and colour (row by row) plus `Rows`/`Columns` builds the entire coloured table. See [Formatted cells](DataTreeToTable.md#formatted-cells) for the details.

Unlike [Data Tree To Table](DataTreeToTable.md), the table size here is set explicitly via Rows and Columns rather than inferred from the data tree. If the tree supplies fewer cells than Rows x Columns, the remaining cells are left blank; if it supplies more, the extra cells are dropped and a runtime warning is raised.

Use this component when the table needs a fixed size regardless of how much (or how little) data is available — for example, a template table that should always come out the same dimensions.

## Related

- [Data Tree To Table](DataTreeToTable.md) — use instead when the table's size should simply follow the shape of the data tree.
