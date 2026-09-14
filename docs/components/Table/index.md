# Table

This tab only holds two components, and that's deliberate. Building a table FROM data — handing it a Grasshopper data tree (or an explicit row/column count) and getting back the requests to create that table — is a realistic thing to author in Grasshopper. Editing the rows, columns, or cells of a table that already exists in a document is not: that kind of edit needs live structural indices pulled from a document you've already fetched, and those indices aren't something you can reasonably hand-author ahead of time in a visual workflow. So this tab covers table creation only.

| Component | Description |
|---|---|
| [Data Tree To Table](DataTreeToTable.md) | Builds a table from a data tree, inferring row and column count from the tree's shape. |
| [Fixed Size Table](FixedSizeTable.md) | Builds a table with an explicit row and column count, padding or dropping cells to fit the supplied data. |
