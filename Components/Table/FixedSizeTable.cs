using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace DocsConnect
{
    // Same composite pattern as Data Tree To Table, but table shape is explicit (Rows x Columns)
    // instead of inferred from the data. If the data tree has fewer cells, remaining cells are
    // left blank; if more, extras are dropped with a runtime warning. Self-indexed from 1 —
    // wire it into Request Aggregator to place it in the document.
    public class FixedSizeTableComponent : GH_Component
    {
        public FixedSizeTableComponent()
            : base("Fixed Size Table", "FixedTbl",
                "Builds the requests to insert and populate a Google Docs table at an explicit Rows x Columns size (pads with blank cells or drops extra data as needed). Cells accept plain text or another component's Request JSON for formatted cell content. No index needed — wire it into Request Aggregator to place it in the document.",
                PluginUtilities.TabName, PluginUtilities.CategoryAFRequestsTable)
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Data", "D", "Cell data — one branch per row, or a single flat branch of every cell in reading order (cut into rows of Columns). Items are plain text (one cell each) or the Request JSON of a Text/Paragraph component (Bold Text, Text Color, a chain of them): each formatted line becomes one cell.", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Rows", "RO", "Table row count.", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("Columns", "CO", "Table column count.", GH_ParamAccess.item, 1);
            pManager.AddBooleanParameter("Header Row", "HR", "Treat the first row as a header.", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Bold Header", "BH", "Bold the header row text.", GH_ParamAccess.item, true);
            // Optional per-cell formatting, each matched to Data cell-for-cell (row = branch,
            // column = item; a shorter list repeats its last value, so one item formats every cell).
            pManager.AddBooleanParameter("Bold", "B", "Bold per cell, matched to Data (one value applies to every cell).", GH_ParamAccess.tree);
            pManager.AddBooleanParameter("Italic", "I", "Italic per cell, matched to Data.", GH_ParamAccess.tree);
            pManager.AddBooleanParameter("Underline", "U", "Underline per cell, matched to Data.", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Font Size", "FS", "Font size in points per cell, matched to Data (>0 to apply).", GH_ParamAccess.tree);
            pManager.AddTextParameter("Font Family", "FF", "Font family per cell, matched to Data (blank to skip).", GH_ParamAccess.tree);
            pManager.AddTextParameter("Text Color", "FC", "Text colour as #RRGGBB per cell, matched to Data (blank to skip).", GH_ParamAccess.tree);
            pManager.AddTextParameter("Fill", "FL", "Cell background colour as #RRGGBB per cell, matched to Data (blank to skip).", GH_ParamAccess.tree);
            for (int i = 5; i < 5 + 7; i++) pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Request JSON", "RJ", "batchUpdate requests array.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GHDataHelpers.WarnIfIterated(this, DA);
            GH_Structure<GH_String> tree;
            int rowCount = 1;
            int colCount = 1;
            bool headerRow = true;
            bool boldHeader = true;

            if (!DA.GetDataTree(0, out tree)) return;
            DA.GetData(1, ref rowCount);
            DA.GetData(2, ref colCount);
            DA.GetData(3, ref headerRow);
            DA.GetData(4, ref boldHeader);

            if (rowCount <= 0 || colCount <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Rows and Columns must both be greater than 0.");
                return;
            }

            // one branch = one row; a branch's request items (Text Color's output list, a chain)
            // expand to one cell per formatted line, plain strings stay one cell each
            var rows = new List<IList<string>>();
            foreach (var branch in tree.Branches)
            {
                var items = new List<string>();
                foreach (var cell in branch) items.Add(cell?.Value ?? "");
                rows.Add(BlockBuilders.ExpandCells(items));
            }

            // A single branch holding more cells than one row is the whole table in reading order
            // (Text Color fed every cell's text and colour in one go) — cut it into rows of Columns.
            if (rows.Count == 1 && rows[0].Count > colCount)
            {
                var flat = rows[0];
                rows = new List<IList<string>>();
                for (int i = 0; i < flat.Count; i += colCount)
                {
                    var row = new List<string>();
                    for (int j = i; j < Math.Min(i + colCount, flat.Count); j++) row.Add(flat[j]);
                    rows.Add(row);
                }
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    "Single branch of " + flat.Count + " cells read row-major as " + rows.Count + " x " + colCount + ".");
            }

            int widestSource = 0;
            foreach (var row in rows) widestSource = Math.Max(widestSource, row.Count);

            if (rows.Count > rowCount || widestSource > colCount)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"Data tree ({rows.Count} rows x {widestSource} cols) is larger than the requested {rowCount} x {colCount} table — extra data is dropped.");


            // per-cell formatting from the optional format trees
            var boldT = GHDataHelpers.TreeOrNull<GH_Boolean>(DA, 5);
            var italicT = GHDataHelpers.TreeOrNull<GH_Boolean>(DA, 5 + 1);
            var underlineT = GHDataHelpers.TreeOrNull<GH_Boolean>(DA, 5 + 2);
            var sizeT = GHDataHelpers.TreeOrNull<GH_Number>(DA, 5 + 3);
            var familyT = GHDataHelpers.TreeOrNull<GH_String>(DA, 5 + 4);
            var colorT = GHDataHelpers.TreeOrNull<GH_String>(DA, 5 + 5);
            var fillT = GHDataHelpers.TreeOrNull<GH_String>(DA, 5 + 6);

            int cols = 0;
            foreach (var row in rows) cols = Math.Max(cols, row.Count);
            cols = colCount; // the table is exactly Rows x Columns
            while (rows.Count < rowCount) rows.Add(new List<string>());
            var formats = new List<IList<AecHelperBuilders.CellFormat>>();
            for (int rI = 0; rI < rows.Count; rI++)
            {
                var line = new List<AecHelperBuilders.CellFormat>();
                for (int cI = 0; cI < cols; cI++)
                {
                    line.Add(new AecHelperBuilders.CellFormat
                    {
                        Bold = GHDataHelpers.CellValue(boldT, rI, cI, cols)?.Value,
                        Italic = GHDataHelpers.CellValue(italicT, rI, cI, cols)?.Value,
                        Underline = GHDataHelpers.CellValue(underlineT, rI, cI, cols)?.Value,
                        FontSize = GHDataHelpers.CellValue(sizeT, rI, cI, cols)?.Value,
                        FontFamily = GHDataHelpers.CellValue(familyT, rI, cI, cols)?.Value,
                        TextColor = GHDataHelpers.CellValue(colorT, rI, cI, cols)?.Value,
                        Fill = GHDataHelpers.CellValue(fillT, rI, cI, cols)?.Value,
                    });
                }
                formats.Add(line);
            }

            DA.SetDataList(0, new List<string> { AecHelperBuilders.FixedSizeTable(rows, rowCount, colCount, headerRow, boldHeader, formats) });
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_FixedSizeTable;
        public override Guid ComponentGuid => new Guid("2754eea6-61c4-470e-a71a-906e70b4f104");
    }
}
