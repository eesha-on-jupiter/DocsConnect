using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace DocsConnect
{
    // Turns a Grasshopper data tree (one branch per row) into a populated Google Docs table.
    public class DataTreeToTableComponent : GH_Component
    {
        public DataTreeToTableComponent()
            : base("Data Tree To Table", "Tree2Tbl",
                "Builds the requests to insert and populate a Google Docs table from a data tree (one branch per row). No index needed — wire it into Request Aggregator to place it in the document. Cells accept plain text or another component's Request JSON for formatted cell content. Table shape is inferred from the tree; for explicit Rows/Columns control, use Fixed Size Table instead.",
                PluginUtilities.TabName, PluginUtilities.CategoryAFRequestsTable)
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Data", "D", "Cell data — one branch per row. Items are plain text (one cell each) or the Request JSON of a Text/Paragraph component (Bold Text, Text Color, a chain of them): each formatted line becomes one cell, so Text Color with three texts and colours wired into a row gives three coloured cells.", GH_ParamAccess.tree);
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
            for (int i = 3; i < 3 + 7; i++) pManager[i].Optional = true;
            // Registered last so saved definitions keep their wires. Grasshopper data usually comes
            // one list per attribute (a column) rather than one list per row — this reads it that way.
            pManager.AddBooleanParameter("Transpose", "TR", "Read each branch as a COLUMN instead of a row — Entwine(labels, values) becomes a labels column next to a values column. Format trees follow the same orientation.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Request JSON", "RJ", "batchUpdate requests array.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GHDataHelpers.WarnIfIterated(this, DA);
            GH_Structure<GH_String> tree;
            bool headerRow = true;
            bool boldHeader = true;

            if (!DA.GetDataTree(0, out tree)) return;
            DA.GetData(1, ref headerRow);
            DA.GetData(2, ref boldHeader);
            bool transpose = false;
            DA.GetData(10, ref transpose);

            // one branch = one row; a branch's request items (Text Color's output list, a chain)
            // expand to one cell per formatted line, plain strings stay one cell each
            var rows = new List<IList<string>>();
            foreach (var branch in tree.Branches)
            {
                var items = new List<string>();
                foreach (var cell in branch) items.Add(cell?.Value ?? "");
                rows.Add(BlockBuilders.ExpandCells(items));
            }

            if (rows.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Data tree is empty.");
                return;
            }


            // per-cell formatting from the optional format trees
            var boldT = GHDataHelpers.TreeOrNull<GH_Boolean>(DA, 3);
            var italicT = GHDataHelpers.TreeOrNull<GH_Boolean>(DA, 3 + 1);
            var underlineT = GHDataHelpers.TreeOrNull<GH_Boolean>(DA, 3 + 2);
            var sizeT = GHDataHelpers.TreeOrNull<GH_Number>(DA, 3 + 3);
            var familyT = GHDataHelpers.TreeOrNull<GH_String>(DA, 3 + 4);
            var colorT = GHDataHelpers.TreeOrNull<GH_String>(DA, 3 + 5);
            var fillT = GHDataHelpers.TreeOrNull<GH_String>(DA, 3 + 6);

            int cols = 0;
            foreach (var row in rows) cols = Math.Max(cols, row.Count);
            
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

            if (transpose)
            {
                rows = Transpose(rows, "");
                formats = Transpose(formats, new AecHelperBuilders.CellFormat());
                Message = rows.Count + " x " + (rows.Count > 0 ? rows[0].Count : 0) + " (transposed)";
            }

            DA.SetDataList(0, new List<string> { AecHelperBuilders.DataTreeToTable(rows, headerRow, boldHeader, formats) });
        }

        // branches-as-columns -> rows, padding short columns with `blank`
        private static List<IList<T>> Transpose<T>(IList<IList<T>> columns, T blank)
        {
            int rowCount = 0;
            foreach (var col in columns) rowCount = Math.Max(rowCount, col.Count);
            var result = new List<IList<T>>();
            for (int r = 0; r < rowCount; r++)
            {
                var row = new List<T>();
                foreach (var col in columns) row.Add(r < col.Count ? col[r] : blank);
                result.Add(row);
            }
            return result;
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_DataTreeToTable;
        public override Guid ComponentGuid => new Guid("9a14da4b-db50-4d84-a47c-b05a000e17da");
    }
}
