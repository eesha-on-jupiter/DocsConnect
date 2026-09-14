using System.Collections.Generic;
using System.Linq;
using Xunit;
using Newtonsoft.Json.Linq;
using DocsConnect;

namespace DocsConnect.Tests
{
    // Proves the aggregate / index-sequencing logic — the cross-request math the AEC helpers absorb.
    public class BulkAssemblyTests
    {
        #region Requests array assembly

        [Fact]
        public void AssembleRequestsArray_FlattensObjectsAndArrays()
        {
            string obj = TextRequestBuilders.InsertText("x", 1);
            string arr = "[" + TextRequestBuilders.InsertText("y", 2) + "," + TextRequestBuilders.InsertText("z", 3) + "]";

            var result = JArray.Parse(DocumentBuilders.AssembleRequestsArray(new List<string> { obj, arr }));
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void AssembleRequestsArray_SkipsBlankAndInvalid()
        {
            var result = JArray.Parse(DocumentBuilders.AssembleRequestsArray(
                new List<string> { "", "   ", "not json", TextRequestBuilders.InsertText("ok", 1) }));
            Assert.Single(result);
        }

        #endregion

        #region AppendCursor re-indexing

        [Fact]
        public void ReindexBlocks_ShiftsByCumulativeTextLength()
        {
            string block1 = "[" + TextRequestBuilders.InsertText("AAA", 1) + "]"; // length 3
            string block2 = "[" + TextRequestBuilders.InsertText("BB", 1) + "]";  // length 2

            var result = JArray.Parse(DocumentBuilders.ReindexBlocks(new List<string> { block1, block2 }, 1));

            Assert.Equal(2, result.Count);
            Assert.Equal(1, (int)result[0]["insertText"]["location"]["index"]);  // base shift 0
            Assert.Equal(4, (int)result[1]["insertText"]["location"]["index"]);  // shifted by 3
        }

        [Fact]
        public void ReindexBlocks_HonoursStartIndex()
        {
            string block = "[" + TextRequestBuilders.InsertText("Z", 1) + "]";
            var result = JArray.Parse(DocumentBuilders.ReindexBlocks(new List<string> { block }, 10));
            Assert.Equal(10, (int)result[0]["insertText"]["location"]["index"]); // base shift 9
        }

        #endregion

        #region Append position

        [Fact]
        public void BodyEndIndex_ReturnsLastElementEndMinusOne()
        {
            // the body's final paragraph break can't be inserted at, so append lands one before it
            string doc = @"{""body"":{""content"":[
                {""startIndex"":1,""endIndex"":9},
                {""startIndex"":9,""endIndex"":25}]}}";
            Assert.Equal(24, DocumentBuilders.BodyEndIndex(doc));
        }

        [Fact]
        public void BodyEndIndex_EmptyOrUnparseable_FallsBackToTopOfBody()
        {
            Assert.Equal(1, DocumentBuilders.BodyEndIndex(""));
            Assert.Equal(1, DocumentBuilders.BodyEndIndex("not json"));
            Assert.Equal(1, DocumentBuilders.BodyEndIndex(@"{""body"":{""content"":[]}}"));
            Assert.Equal(1, DocumentBuilders.BodyEndIndex(@"{""body"":{""content"":[{""endIndex"":1}]}}"));
        }

        #endregion

        #region DataTreeToTable cell indexing (live-verified offset = 4)

        [Fact]
        public void DataTreeToTable_FirstRequestIsInsertTable()
        {
            var rows = new List<IList<string>>
            {
                new List<string> { "A", "B" },
                new List<string> { "C", "D" }
            };
            var arr = JArray.Parse(AecHelperBuilders.DataTreeToTable(rows, false, false));
            Assert.Equal(2, (int)arr[0]["insertTable"]["rows"]);
            Assert.Equal(2, (int)arr[0]["insertTable"]["columns"]);
        }

        [Fact]
        public void DataTreeToTable_FillsCellsAtVerifiedIndices_InReverseOrder()
        {
            var rows = new List<IList<string>>
            {
                new List<string> { "A", "B" },
                new List<string> { "C", "D" }
            };
            var arr = JArray.Parse(AecHelperBuilders.DataTreeToTable(rows, false, false));

            // cell indices for a 2x2 table at index 1: (0,0)=5 (0,1)=7 (1,0)=10 (1,1)=12
            // filled highest-index first so earlier fills do not shift later targets
            var fillIndices = new List<int>();
            for (int i = 1; i < arr.Count; i++)
                fillIndices.Add((int)arr[i]["insertText"]["location"]["index"]);

            Assert.Equal(new List<int> { 12, 10, 7, 5 }, fillIndices);
        }

        [Fact]
        public void DataTreeToTable_BoldHeader_EmitsStyleForHeaderCells()
        {
            var rows = new List<IList<string>>
            {
                new List<string> { "A", "B" },
                new List<string> { "C", "D" }
            };
            var arr = JArray.Parse(AecHelperBuilders.DataTreeToTable(rows, true, true));

            int boldCount = 0;
            foreach (var r in arr)
                if (r["updateTextStyle"] != null && (bool?)r["updateTextStyle"]["textStyle"]["bold"] == true)
                    boldCount++;

            Assert.Equal(2, boldCount); // one per header cell
        }

        [Fact]
        public void ExtractCellContent_PlainText_PassesThrough()
        {
            var cell = BlockBuilders.ExtractCellContent("Beam B-12");
            Assert.Equal("Beam B-12", cell.Text);
            Assert.True(cell.Styles == null || cell.Styles.Count == 0);
        }

        [Fact]
        public void ExtractCellContent_NumericText_IsNotMistakenForJson()
        {
            var cell = BlockBuilders.ExtractCellContent("42");
            Assert.Equal("42", cell.Text);
        }

        [Fact]
        public void ExtractCellContent_Block_StripsTrailingBreakAndKeepsStyle()
        {
            // Bold Text emits insertText("Utilisation\n") + updateTextStyle [1, 13)
            string block = "[" + string.Join(",",
                TextRequestBuilders.InsertText("Utilisation\n", 1),
                TextRequestBuilders.UpdateTextStyle(1, 13, true, null, null, null,
                    null, null, null, null, null, null)) + "]";

            var cell = BlockBuilders.ExtractCellContent(block);

            Assert.Equal("Utilisation", cell.Text);              // paragraph break stripped
            Assert.Single(cell.Styles);
            Assert.Equal(1, (int)cell.Styles[0]["updateTextStyle"]["range"]["startIndex"]);
            Assert.Equal(12, (int)cell.Styles[0]["updateTextStyle"]["range"]["endIndex"]); // clamped
        }

        [Fact]
        public void DataTreeToTable_FormattedCell_PlacesStyleAtFinalCellIndex()
        {
            string boldCell = "[" + string.Join(",",
                TextRequestBuilders.InsertText("BB\n", 1),
                TextRequestBuilders.UpdateTextStyle(1, 4, true, null, null, null,
                    null, null, null, null, null, null)) + "]";

            var rows = new List<IList<string>>
            {
                new List<string> { "A", boldCell }
            };
            var arr = JArray.Parse(AecHelperBuilders.DataTreeToTable(rows, false, false));

            // 1x2 table: cell(0,0) = 5, cell(0,1) = 7. "BB" lands at 7 and is pushed one along by
            // the later, lower-index "A" fill, so its final range is [8, 10).
            var style = arr.First(r => r["updateTextStyle"] != null);
            Assert.Equal(8, (int)style["updateTextStyle"]["range"]["startIndex"]);
            Assert.Equal(10, (int)style["updateTextStyle"]["range"]["endIndex"]);
            Assert.True((bool)style["updateTextStyle"]["textStyle"]["bold"]);

            // the block's own text was inserted, not its JSON
            var fill = arr.First(r => r["insertText"] != null && (int)r["insertText"]["location"]["index"] == 7);
            Assert.Equal("BB", (string)fill["insertText"]["text"]);
        }

        [Fact]
        public void DataTreeToTable_EmptyRows_NoRequests()
        {
            var arr = JArray.Parse(AecHelperBuilders.DataTreeToTable(new List<IList<string>>(), false, false));
            Assert.Empty(arr);
        }

        #endregion

        #region Report helpers

        [Fact]
        public void ReportSkeleton_TitleHeadingBody_SequentialIndices()
        {
            var arr = JArray.Parse(AecHelperBuilders.ReportSkeleton(
                "T", new List<string> { "H" }, new List<int> { 1 }, new List<string> { "B" }));

            // insertText "T\n"@1, paraStyle TITLE; insertText "H\n"@3, HEADING_1; insertText "B\n"@5, NORMAL
            Assert.Equal(6, arr.Count);
            Assert.Equal("TITLE", (string)arr[1]["updateParagraphStyle"]["paragraphStyle"]["namedStyleType"]);
            Assert.Equal(3, (int)arr[2]["insertText"]["location"]["index"]);
            Assert.Equal("HEADING_1", (string)arr[3]["updateParagraphStyle"]["paragraphStyle"]["namedStyleType"]);
        }

        [Fact]
        public void BulletList_AddsBulletsOverFullRange()
        {
            var arr = JArray.Parse(AecHelperBuilders.BulletList(
                new List<string> { "a", "b" }, new List<int>(), "BULLET_CHECKBOX"));

            // insertText "a\n"@1 (->3), insertText "b\n"@3 (->5), createParagraphBullets [1,5)
            Assert.Equal(3, arr.Count);
            var bullets = arr[2]["createParagraphBullets"];
            Assert.Equal(1, (int)bullets["range"]["startIndex"]);
            Assert.Equal(5, (int)bullets["range"]["endIndex"]);
            Assert.Equal("BULLET_CHECKBOX", (string)bullets["bulletPreset"]);
        }

        #endregion
    }
}
