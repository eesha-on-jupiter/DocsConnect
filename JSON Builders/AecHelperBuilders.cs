using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DocsConnect
{
    // Composite builders for the AEC helper components — each emits a full requests-array JSON string
    // with all character indices pre-computed (the index math the raw units leave to the user).
    public static class AecHelperBuilders
    {
        #region Report header block

        // builds a project title block: project name (TITLE) then non-empty meta lines (NORMAL).
        // Date is taken as-is (the component fills today's date when blank). Layout "table" falls back
        // to lines in v1. Self-indexed from 1 — place it via Request Aggregator.
        public static string ReportHeaderBlock(
            string projectName, string projectNumber, string date, string author, string revision,
            string layout)
        {
            var reqs = new JArray();
            int cursor = 1;

            cursor = AppendStyledParagraph(reqs, projectName ?? "", cursor, "TITLE");

            cursor = AppendMetaLine(reqs, "Project No.", projectNumber, cursor);
            cursor = AppendMetaLine(reqs, "Date", date, cursor);
            cursor = AppendMetaLine(reqs, "Author", author, cursor);
            cursor = AppendMetaLine(reqs, "Revision", revision, cursor);

            return reqs.ToString(Formatting.None);
        }

        private static int AppendMetaLine(JArray reqs, string label, string value, int cursor)
        {
            if (string.IsNullOrWhiteSpace(value)) return cursor;
            return AppendStyledParagraph(reqs, label + ": " + value, cursor, "NORMAL_TEXT");
        }

        #endregion

        #region Report skeleton

        // title + per-section styled heading and body paragraphs, all indices computed in sequence.
        // Self-indexed from 1 — place it via Request Aggregator.
        public static string ReportSkeleton(
            string title, IList<string> headings, IList<int> levels, IList<string> bodies)
        {
            var reqs = new JArray();
            int cursor = 1;

            if (!string.IsNullOrWhiteSpace(title))
                cursor = AppendStyledParagraph(reqs, title, cursor, "TITLE");

            int n = headings?.Count ?? 0;
            for (int i = 0; i < n; i++)
            {
                string h = headings[i] ?? "";
                int lvl = (levels != null && i < levels.Count) ? levels[i] : 1;
                if (lvl < 1) lvl = 1;
                if (lvl > 6) lvl = 6;

                cursor = AppendStyledParagraph(reqs, h, cursor, "HEADING_" + lvl);

                if (bodies != null && i < bodies.Count && !string.IsNullOrWhiteSpace(bodies[i]))
                    cursor = AppendStyledParagraph(reqs, bodies[i], cursor, "NORMAL_TEXT");
            }

            return reqs.ToString(Formatting.None);
        }

        #endregion

        #region Bullet list

        // items -> sequential paragraphs (optionally tab-indented for nesting) + a single
        // createParagraphBullets over the whole range. Self-indexed from 1 — place it via Request Aggregator.
        public static string BulletList(IList<string> items, IList<int> indentLevels, string bulletPreset)
        {
            var reqs = new JArray();
            int cursor = 1;
            int rangeStart = cursor;

            int n = items?.Count ?? 0;
            for (int i = 0; i < n; i++)
            {
                int indent = (indentLevels != null && i < indentLevels.Count && indentLevels[i] > 0) ? indentLevels[i] : 0;
                string line = new string('\t', indent) + (items[i] ?? "");
                cursor = AppendParagraph(reqs, line, cursor);
            }

            if (cursor > rangeStart)
                reqs.Add(JObject.Parse(ParagraphRequestBuilders.CreateParagraphBullets(rangeStart, cursor, bulletPreset)));

            return reqs.ToString(Formatting.None);
        }

        #endregion

        #region Data tree -> table

        // structural offset of the first table cell's text relative to the insertTable index.
        // EMPIRICALLY VERIFIED against the live API (dogfood F5): inserting a table at location L
        // produces first-cell paragraph index L+4 (Docs adds a paragraph before the table), with row
        // stride (2*columns + 1) and column stride 2. e.g. 2x2 table at index 1 -> cells at 5,7,10,12.
        private const int FirstCellOffset = 4;

        // The table start marker sits one index after the insert location (Docs puts a newline in
        // front of the table) — this is the index updateTableCellStyle addresses cells relative to.
        private const int TableStartOffset = 1;

        // Per-cell formatting supplied alongside the cell text (from the tables' optional format
        // trees). Null / blank members are "not set" and leave that property alone.
        public struct CellFormat
        {
            public bool? Bold;
            public bool? Italic;
            public bool? Underline;
            public double? FontSize;
            public string FontFamily;
            public string TextColor;
            public string Fill;   // cell background, #RRGGBB

            public bool HasTextStyle =>
                Bold.HasValue || Italic.HasValue || Underline.HasValue || (FontSize.HasValue && FontSize.Value > 0)
                || !string.IsNullOrWhiteSpace(FontFamily) || !string.IsNullOrWhiteSpace(TextColor);
        }

        // updateTableCellStyle for one cell's background, addressed by row/column relative to the
        // table start marker. Self-indexed like everything else here: Request Aggregator shifts
        // the tableStartLocation index along with the block.
        public static string UpdateTableCellFill(int tableStartIndex, int row, int col, string fillHex)
        {
            var inner = new JObject
            {
                ["tableRange"] = new JObject
                {
                    ["tableCellLocation"] = new JObject
                    {
                        ["tableStartLocation"] = new JObject { ["index"] = tableStartIndex },
                        ["rowIndex"] = row,
                        ["columnIndex"] = col
                    },
                    ["rowSpan"] = 1,
                    ["columnSpan"] = 1
                },
                ["tableCellStyle"] = new JObject { ["backgroundColor"] = SharedBuilders.OptColor(fillHex) },
                ["fields"] = "backgroundColor"
            };
            return SharedBuilders.Wrap("updateTableCellStyle", inner);
        }

        // builds an InsertTable + reverse-ordered cell fills (so earlier fills do not shift later
        // target indices). Optionally bolds the header row. Self-indexed from 1 — place it via
        // Request Aggregator. Table shape is inferred from the data (branch count = rows, widest
        // branch = columns).
        public static string DataTreeToTable(IList<IList<string>> rows, bool headerRow, bool boldHeader)
        {
            return DataTreeToTable(rows, headerRow, boldHeader, null);
        }

        // formats: optional per-cell formatting, [row][col], same shape as rows (missing entries
        // are "not set").
        public static string DataTreeToTable(IList<IList<string>> rows, bool headerRow, bool boldHeader, IList<IList<CellFormat>> formats)
        {
            int r = rows?.Count ?? 0;
            if (r == 0) return new JArray().ToString(Formatting.None);

            int c = 0;
            for (int i = 0; i < r; i++) c = Math.Max(c, rows[i]?.Count ?? 0);
            if (c == 0) return new JArray().ToString(Formatting.None);

            return BuildTable(rows, r, c, headerRow, boldHeader, formats);
        }

        // same composite pattern, but table shape is explicit (rowCount x colCount) instead of
        // inferred from the data. Extra data beyond rowCount/colCount is dropped; missing data is
        // left as blank cells. Self-indexed from 1 — place it via Request Aggregator.
        public static string FixedSizeTable(IList<IList<string>> rows, int rowCount, int colCount, bool headerRow, bool boldHeader)
        {
            return FixedSizeTable(rows, rowCount, colCount, headerRow, boldHeader, null);
        }

        public static string FixedSizeTable(IList<IList<string>> rows, int rowCount, int colCount, bool headerRow, bool boldHeader, IList<IList<CellFormat>> formats)
        {
            if (rowCount <= 0 || colCount <= 0) return new JArray().ToString(Formatting.None);

            var normalized = new List<IList<string>>();
            int sourceRows = rows?.Count ?? 0;
            for (int row = 0; row < rowCount; row++)
            {
                var line = new List<string>();
                var sourceRow = row < sourceRows ? rows[row] : null;
                for (int col = 0; col < colCount; col++)
                    line.Add((sourceRow != null && col < sourceRow.Count) ? (sourceRow[col] ?? "") : "");
                normalized.Add(line);
            }

            return BuildTable(normalized, rowCount, colCount, headerRow, boldHeader, formats);
        }

        // shared table-fill logic: insert an r x c table at placeholder index 1, then fill cells
        // from highest index to lowest so insertions never shift unfilled targets, then apply every
        // style request at its FINAL index (after all the lower-index fills have pushed it along).
        //
        // A cell value may be plain text OR a self-indexed Request JSON block from any Text or
        // Paragraph component (Bold Text, Text Color, Styled Paragraph, or a chain of them). For a
        // block, its insertText runs become the cell text and its style requests are re-targeted at
        // the cell — so a table can carry bold, coloured, linked or aligned cell content instead of
        // flat strings.
        private static string BuildTable(IList<IList<string>> rows, int r, int c, bool headerRow, bool boldHeader, IList<IList<CellFormat>> formats = null)
        {
            const int insertIndex = 1;
            var reqs = new JArray();

            // 1. insert the empty table
            reqs.Add(JObject.Parse(InsertRequestBuilders.InsertTable(insertIndex, r, c)));

            // 2. compute each cell's original (empty-table) text index and read its content
            int CellIndex(int row, int col) => insertIndex + FirstCellOffset + row * (2 * c + 1) + 2 * col;

            var order = new List<int[]>();                                   // row, col, origIndex
            var content = new Dictionary<int, BlockBuilders.CellContent>();  // origIndex -> content
            for (int row = 0; row < r; row++)
            {
                for (int col = 0; col < c; col++)
                {
                    string raw = (rows[row] != null && col < rows[row].Count) ? (rows[row][col] ?? "") : "";
                    int idx = CellIndex(row, col);
                    order.Add(new[] { row, col, idx });
                    content[idx] = BlockBuilders.ExtractCellContent(raw);
                }
            }

            // 3. fill cells from highest index to lowest so insertions never shift unfilled targets
            order.Sort((a, b) => b[2].CompareTo(a[2]));
            foreach (var cell in order)
            {
                string text = content[cell[2]].Text;
                if (text.Length == 0) continue;
                reqs.Add(JObject.Parse(TextRequestBuilders.InsertText(text, cell[2])));
            }

            // 4. style at final positions. Every fill at a LOWER index pushes this cell along, so a
            //    cell's final start is its empty-table index plus the total text length below it.
            //    Header bold goes first so a cell's own chained styles win where they overlap.
            order.Sort((a, b) => a[2].CompareTo(b[2]));

            int FinalStart(int orig)
            {
                int shift = 0;
                foreach (var kv in content)
                    if (kv.Key < orig) shift += kv.Value.Text.Length;
                return orig + shift;
            }

            if (headerRow && boldHeader && r > 0)
            {
                for (int col = 0; col < c; col++)
                {
                    int orig = CellIndex(0, col);
                    int len = content[orig].Text.Length;
                    if (len == 0) continue;

                    int finalStart = FinalStart(orig);
                    reqs.Add(JObject.Parse(TextRequestBuilders.UpdateTextStyle(
                        finalStart, finalStart + len,
                        true, null, null, null, null, null, null, null, null, null)));
                }
            }

            // 4b. per-cell formatting from the format trees: text style over the cell's final text
            //     range (after header bold so an explicit format wins; before the cell's own
            //     chained styles so those win over it), and cell background by row/column.
            if (formats != null)
            {
                foreach (var cell in order)
                {
                    int row = cell[0], col = cell[1];
                    if (row >= formats.Count || formats[row] == null || col >= formats[row].Count) continue;
                    var f = formats[row][col];

                    int len = content[cell[2]].Text.Length;
                    if (f.HasTextStyle && len > 0)
                    {
                        int finalStart = FinalStart(cell[2]);
                        reqs.Add(JObject.Parse(TextRequestBuilders.UpdateTextStyle(
                            finalStart, finalStart + len,
                            f.Bold, f.Italic, f.Underline, null,
                            f.FontSize.HasValue && f.FontSize.Value > 0 ? f.FontSize : null,
                            f.FontFamily, f.TextColor, null, null, null)));
                    }

                    if (SharedBuilders.ParseHexRgb(f.Fill) != null)
                        reqs.Add(JObject.Parse(UpdateTableCellFill(insertIndex + TableStartOffset, row, col, f.Fill)));
                }
            }

            foreach (var cell in order)
            {
                var cellContent = content[cell[2]];
                if (cellContent.Styles == null || cellContent.Styles.Count == 0) continue;
                if (cellContent.Text.Length == 0) continue;

                // the block's styles are self-indexed from 1, so shifting by (finalStart - 1) lands
                // them exactly on the text this cell now holds
                int shift = FinalStart(cell[2]) - 1;
                foreach (var style in cellContent.Styles)
                {
                    var placed = (JObject)style.DeepClone();
                    ShiftIndices(placed, shift);
                    reqs.Add(placed);
                }
            }

            return reqs.ToString(Formatting.None);
        }

        // recursively adds shift to every integer index/startIndex/endIndex — the same rewrite
        // Request Aggregator does to place a block, applied here to place a cell's chained styles.
        private static void ShiftIndices(JToken token, int shift)
        {
            if (token is JObject obj)
            {
                foreach (var prop in obj.Properties())
                {
                    if ((prop.Name == "index" || prop.Name == "startIndex" || prop.Name == "endIndex")
                        && prop.Value.Type == JTokenType.Integer)
                        prop.Value = (int)prop.Value + shift;
                    else
                        ShiftIndices(prop.Value, shift);
                }
            }
            else if (token is JArray arr)
            {
                foreach (var item in arr) ShiftIndices(item, shift);
            }
        }

        #endregion

        #region Private helpers

        // inserts "text\n" at cursor and returns the advanced cursor
        private static int AppendParagraph(JArray reqs, string text, int cursor)
        {
            string content = (text ?? "") + "\n";
            reqs.Add(JObject.Parse(TextRequestBuilders.InsertText(content, cursor)));
            return cursor + content.Length;
        }

        // inserts "text\n" at cursor, applies a named paragraph style over its range, returns advanced cursor
        private static int AppendStyledParagraph(JArray reqs, string text, int cursor, string namedStyle)
        {
            string content = (text ?? "") + "\n";
            reqs.Add(JObject.Parse(TextRequestBuilders.InsertText(content, cursor)));
            int end = cursor + content.Length;
            reqs.Add(JObject.Parse(ParagraphRequestBuilders.UpdateParagraphStyle(
                cursor, end, namedStyle, null, null, null, null, null)));
            return end;
        }

        #endregion
    }
}
