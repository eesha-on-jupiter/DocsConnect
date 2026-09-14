using System.Collections.Generic;
using System.Linq;
using Xunit;
using Newtonsoft.Json.Linq;
using DocsConnect;

namespace DocsConnect.Tests
{
    // Proves components actually wire into one another end to end at the builder level: one
    // component's output list/tree is exactly the shape the next component's input expects, and the
    // resulting indices are correct once combined. This is the closest thing to a cross-component
    // integration test that doesn't require a live Grasshopper canvas — GH_Component classes need the
    // Grasshopper/RhinoCommon runtime and can't be instantiated here, so these tests exercise the same
    // pure builder calls each component's SolveInstance makes, in the same order a canvas would.
    public class PipelineIntegrationTests
    {
        #region Insert-tab blocks wire into Request Aggregator like Text-tab blocks do

        [Fact]
        public void RequestAggregator_SequencesMixedTextAndInsertBlocks()
        {
            // Simulates: Bold Text ("Hi\n", 3 chars) -> Insert Image -> Insert Page Break -> Insert
            // Section Break, all stacked into one Request Aggregator run (Start Index 1).
            string textBlock = "[" + string.Join(",", TextRequestBuilders.InsertStyledTextLines(
                new List<string> { "Hi" }, true, null, null, null, null, null, null, null, null, null)) + "]";
            string imageBlock = "[" + string.Join(",", InsertRequestBuilders.InsertInlineImagesSequential(
                new List<string> { "https://img" }, new List<double?>(), new List<double?>())) + "]";
            string pageBreakBlock = "[" + string.Join(",", InsertRequestBuilders.InsertPageBreaksSequential(1)) + "]";
            string sectionBreakBlock = "[" + string.Join(",", InsertRequestBuilders.InsertSectionBreaksSequential(
                new List<string> { "NEXT_PAGE" })) + "]";

            var blocks = new List<string> { textBlock, imageBlock, pageBreakBlock, sectionBreakBlock };
            var result = JArray.Parse(DocumentBuilders.ReindexBlocks(blocks, 1));

            // textBlock = insertText("Hi\n", ...) + updateTextStyle -> 2 requests, content length 3
            var insertTextReq = result[0];
            Assert.Equal(1, (int)insertTextReq["insertText"]["location"]["index"]);

            // image lands right after the 3-char text block
            var imageReq = result.First(r => r["insertInlineImage"] != null);
            Assert.Equal(4, (int)imageReq["insertInlineImage"]["location"]["index"]);

            // page break lands right after the 1-index image
            var pageBreakReq = result.First(r => r["insertPageBreak"] != null);
            Assert.Equal(5, (int)pageBreakReq["insertPageBreak"]["location"]["index"]);

            // section break lands after the page break AND the newline Docs adds behind it
            var sectionBreakReq = result.First(r => r["insertSectionBreak"] != null);
            Assert.Equal(7, (int)sectionBreakReq["insertSectionBreak"]["location"]["index"]);
        }

        [Fact]
        public void RequestAggregator_MultiItemInsertImageBlockShiftsAsOneUnit()
        {
            // Simulates one Insert Image component call with 3 images (one GH branch = one block),
            // stacked after a page break. All 3 images must shift together by the same amount.
            string pageBreakBlock = "[" + string.Join(",", InsertRequestBuilders.InsertPageBreaksSequential(1)) + "]";
            string imagesBlock = "[" + string.Join(",", InsertRequestBuilders.InsertInlineImagesSequential(
                new List<string> { "https://a", "https://b", "https://c" }, new List<double?>(), new List<double?>())) + "]";

            var result = JArray.Parse(DocumentBuilders.ReindexBlocks(new List<string> { pageBreakBlock, imagesBlock }, 1));

            var images = result.Where(r => r["insertInlineImage"] != null).ToList();
            Assert.Equal(3, images.Count);
            // page break + its newline occupy 1-2, so the 3 images land at 3, 4, 5 (1 index apart)
            Assert.Equal(3, (int)images[0]["insertInlineImage"]["location"]["index"]);
            Assert.Equal(4, (int)images[1]["insertInlineImage"]["location"]["index"]);
            Assert.Equal(5, (int)images[2]["insertInlineImage"]["location"]["index"]);
        }

        #endregion

        #region Fixed Requests pass through Request Aggregator untouched, after the placed blocks

        [Fact]
        public void RequestAggregator_CombinesPlacedAndFixedBlocks()
        {
            // Simulates Request Aggregator's SolveInstance: the placed blocks are re-indexed as one
            // array, then Fixed Requests (Replace Image — object-ID based, no position) are appended
            // verbatim, and the whole thing is flattened into one requests array.
            string placed = DocumentBuilders.ReindexBlocks(
                new List<string> { "[" + InsertRequestBuilders.InsertPageBreak(1) + "]" }, 1);
            string replaceImage = InsertRequestBuilders.ReplaceImage("objId123", "https://new-img");

            var finalRequests = JArray.Parse(DocumentBuilders.AssembleRequestsArray(new[] { placed, replaceImage }));

            Assert.Equal(2, finalRequests.Count);
            Assert.NotNull(finalRequests[0]["insertPageBreak"]);
            Assert.NotNull(finalRequests[1]["replaceImage"]);
        }

        [Fact]
        public void RequestAggregator_FixedRequestsKeepTheirAbsoluteIndices()
        {
            // An Update Text Style with explicit indices goes into Fixed Requests precisely so the
            // running content tally of the placed blocks never shifts it.
            string placed = DocumentBuilders.ReindexBlocks(
                new List<string> { "[" + TextRequestBuilders.InsertText("Hello\n", 1) + "]" }, 1);
            string fixedStyle = TextRequestBuilders.UpdateTextStyle(40, 50, true, null, null, null, null, "", "", "", "", "");

            var finalRequests = JArray.Parse(DocumentBuilders.AssembleRequestsArray(new[] { placed, fixedStyle }));

            var range = finalRequests[1]["updateTextStyle"]["range"];
            Assert.Equal(40, (int)range["startIndex"]);
            Assert.Equal(50, (int)range["endIndex"]);
        }

        [Fact]
        public void RequestAggregator_AlreadyPlacedSingleBlockReindexesWithZeroShift()
        {
            // A block whose indices are already real (an aggregator's own output fed back in)
            // arrives as ONE block; re-indexing it from Start Index 1 is a no-op.
            string placedOnce = DocumentBuilders.ReindexBlocks(new List<string>
            {
                "[" + TextRequestBuilders.InsertText("One\n", 1) + "]",
                "[" + TextRequestBuilders.InsertText("Two\n", 1) + "]",
            }, 1);

            var twice = JArray.Parse(DocumentBuilders.ReindexBlocks(new List<string> { placedOnce }, 1));

            Assert.Equal(1, (int)twice[0]["insertText"]["location"]["index"]);
            Assert.Equal(5, (int)twice[1]["insertText"]["location"]["index"]);
        }

        #endregion

        #region Several blocks arriving on one wire (Merge / Relay) are still placed in order

        [Fact]
        public void ReindexBlocks_SplitsBlocksMergedIntoOneList()
        {
            // The exact list a Merge component hands Request Aggregator when a chained heading
            // (Heading Text -> Aligned Text) and a Bullet List are wired through it: one branch,
            // both blocks self-indexed at 1. The bullet block must land after the heading, not on
            // top of it.
            string merged = "[" + string.Join(",", new[]
            {
                TextRequestBuilders.InsertText("Taper Twist Tower\n", 1),
                ParagraphRequestBuilders.UpdateParagraphStyle(1, 19, "HEADING_1", "", null, null, null, null),
                ParagraphRequestBuilders.UpdateParagraphStyle(1, 19, "", "CENTER", null, null, null, null),
                TextRequestBuilders.InsertText("Body text.\n", 1),
                ParagraphRequestBuilders.CreateParagraphBullets(1, 12, "BULLET_DISC_CIRCLE_SQUARE"),
            }) + "]";

            var result = JArray.Parse(DocumentBuilders.ReindexBlocks(new List<string> { merged }, 1));

            Assert.Equal(5, result.Count);
            Assert.Equal(1, (int)result[0]["insertText"]["location"]["index"]);
            Assert.Equal(1, (int)result[2]["updateParagraphStyle"]["range"]["startIndex"]);
            Assert.Equal(19, (int)result[3]["insertText"]["location"]["index"]);
            Assert.Equal(19, (int)result[4]["createParagraphBullets"]["range"]["startIndex"]);
            Assert.Equal(30, (int)result[4]["createParagraphBullets"]["range"]["endIndex"]);
        }

        [Fact]
        public void ReindexBlocks_DoesNotSplitAMultiLineBlock()
        {
            // A single Heading Text with three lines inserts at 1, then 5, then 9 — indices only
            // grow, so it must stay one block (and shift as one unit behind a preceding block).
            string threeLines = "[" + string.Join(",", ParagraphRequestBuilders.InsertStyledParagraphLines(
                new List<string> { "One", "Two", "Six" }, new List<string> { "HEADING_2" }, null, null, null, null, null)) + "]";
            string before = "[" + TextRequestBuilders.InsertText("AB\n", 1) + "]";

            var result = JArray.Parse(DocumentBuilders.ReindexBlocks(new List<string> { before, threeLines }, 1));

            Assert.Equal(4, (int)result[1]["insertText"]["location"]["index"]);
            Assert.Equal(8, (int)result[3]["insertText"]["location"]["index"]);
            Assert.Equal(12, (int)result[5]["insertText"]["location"]["index"]);
        }

        #endregion

        #region Nested bullets: createParagraphBullets strips the leading tabs it consumed

        [Fact]
        public void ReindexBlocks_SubtractsLeadingTabsRemovedByCreateParagraphBullets()
        {
            // The exact Bullet List from the tower report: 7 items, 4 of them nested one level, so
            // 4 tab characters that Google removes when it applies the bullets. Inserted text is
            // 127 characters; the document only grows by 123, so the next block must land at 124
            // (Google rejected 128: "must be less than the end index of the referenced segment, 125").
            string bullets = AecHelperBuilders.BulletList(
                new List<string> { "No. of Floors: 24", "Metrics", "Total Area: 11403.304348", "Total Volume: 45613.217391", "Rotational Angle", "Min Angle: 0", "Max Angle: 138" },
                new List<int> { 0, 0, 1, 1, 0, 1, 1 }, "BULLET_DIAMONDX_ARROW3D_SQUARE");
            string heading = "[" + TextRequestBuilders.InsertText("Taper Twist Tower\n", 1) + "]";

            var result = JArray.Parse(DocumentBuilders.ReindexBlocks(new List<string> { bullets, heading }, 1));

            var bulletRange = result[7]["createParagraphBullets"]["range"];
            Assert.Equal(128, (int)bulletRange["endIndex"]);
            Assert.Equal(124, (int)result[8]["insertText"]["location"]["index"]);
        }

        [Fact]
        public void ReindexBlocks_FlatBulletListLosesNothing()
        {
            string bullets = AecHelperBuilders.BulletList(new List<string> { "A", "B" }, null, "BULLET_CHECKBOX");
            string next = "[" + TextRequestBuilders.InsertText("C\n", 1) + "]";

            var result = JArray.Parse(DocumentBuilders.ReindexBlocks(new List<string> { bullets, next }, 1));

            Assert.Equal(5, (int)result[3]["insertText"]["location"]["index"]);
        }

        #endregion

        #region Style isolation: a placed block never inherits from where it lands

        [Fact]
        public void ReindexBlocks_IsolateStyles_ResetsEachInsertBeforeTheBlocksOwnStyle()
        {
            // Heading Text chained through Aligned Text, then an Insert Text. With isolation on,
            // each insertText is followed by a paragraph reset, a bullets delete and a text reset
            // over exactly the inserted range — and the block's own updateParagraphStyle requests
            // still come after the reset, so the heading is still a centered H1.
            string heading = "[" + string.Join(",", ParagraphRequestBuilders.InsertStyledParagraphLines(
                new List<string> { "Title" }, new List<string> { "HEADING_1" }, new List<string> { "CENTER" }, null, null, null, null)) + "]";
            string body = "[" + TextRequestBuilders.InsertText("Body\n", 1) + "]";

            var result = JArray.Parse(DocumentBuilders.ReindexBlocks(new List<string> { heading, body }, 1, true));

            Assert.NotNull(result[0]["insertText"]);
            Assert.Equal("NORMAL_TEXT", (string)result[1]["updateParagraphStyle"]["paragraphStyle"]["namedStyleType"]);
            Assert.Contains("alignment", (string)result[1]["updateParagraphStyle"]["fields"]);
            Assert.Equal(7, (int)result[1]["updateParagraphStyle"]["range"]["endIndex"]);
            Assert.NotNull(result[2]["deleteParagraphBullets"]);
            Assert.Equal("*", (string)result[3]["updateTextStyle"]["fields"]);
            Assert.Equal("HEADING_1", (string)result[4]["updateParagraphStyle"]["paragraphStyle"]["namedStyleType"]);

            // body block: shifted by the heading's 6 characters, resets included, nothing else moved
            Assert.Equal(7, (int)result[5]["insertText"]["location"]["index"]);
            Assert.Equal(7, (int)result[6]["updateParagraphStyle"]["range"]["startIndex"]);
            Assert.Equal(12, (int)result[6]["updateParagraphStyle"]["range"]["endIndex"]);
        }

        [Fact]
        public void ReindexBlocks_IsolateStyles_SkipsParagraphResetForSameParagraphRuns()
        {
            // A run with no paragraph break joins an existing paragraph; resetting that paragraph's
            // style would restyle text the run did not insert. Only the text style is reset.
            string run = "[" + TextRequestBuilders.InsertText("inline", 1) + "]";

            var result = JArray.Parse(DocumentBuilders.ReindexBlocks(new List<string> { run }, 1, true));

            Assert.Equal(2, result.Count);
            Assert.NotNull(result[1]["updateTextStyle"]);
        }

        [Fact]
        public void ReindexBlocks_IsolateStyles_CarriesSegmentId()
        {
            string header = "[" + TextRequestBuilders.InsertText("Hdr\n", 1, "kix.abc") + "]";

            var result = JArray.Parse(DocumentBuilders.ReindexBlocks(new List<string> { header }, 1, true));

            Assert.Equal("kix.abc", (string)result[1]["updateParagraphStyle"]["range"]["segmentId"]);
            Assert.Equal("kix.abc", (string)result[3]["updateTextStyle"]["range"]["segmentId"]);
        }

        #endregion

        #region Segment content starts at index 0

        [Fact]
        public void TargetsSegment_DetectsSegmentIdOnInserts()
        {
            string header = "[" + TextRequestBuilders.InsertText("Hdr\n", 1, "kix.hdr") + "]";
            string body = "[" + TextRequestBuilders.InsertText("Body\n", 1) + "]";

            Assert.True(DocumentBuilders.TargetsSegment(new List<string> { header }));
            Assert.False(DocumentBuilders.TargetsSegment(new List<string> { body }));
        }

        [Fact]
        public void ReindexBlocks_FromSegmentStart_PlacesFirstInsertAtZero()
        {
            // What Request Aggregator does for a header: Start Index 1 (the default) becomes 0, so
            // a fresh segment — one empty paragraph at [0,1) — accepts the insert; a second block
            // follows at 0 + the first block's length.
            string first = "[" + TextRequestBuilders.InsertText("Project X\n", 1, "kix.hdr") + "]";
            string second = "[" + TextRequestBuilders.InsertText("Rev A\n", 1, "kix.hdr") + "]";

            var result = JArray.Parse(DocumentBuilders.ReindexBlocks(
                new List<string> { first, second }, DocumentBuilders.SegmentStartIndex));

            Assert.Equal(0, (int)result[0]["insertText"]["location"]["index"]);
            Assert.Equal("kix.hdr", (string)result[0]["insertText"]["location"]["segmentId"]);
            Assert.Equal(10, (int)result[1]["insertText"]["location"]["index"]);
        }

        #endregion

        #region Structural inserts grow the document by what the API reference says

        [Fact]
        public void ReindexBlocks_PageBreakOccupiesBreakPlusNewline()
        {
            // "Inserts a page break followed by a newline" — two indices, so a block after one
            // page break starts at 1 + 2 = 3, and two breaks self-sequence at 1 and 3.
            string breaks = "[" + string.Join(",", InsertRequestBuilders.InsertPageBreaksSequential(2)) + "]";
            string next = "[" + TextRequestBuilders.InsertText("After\n", 1) + "]";

            var result = JArray.Parse(DocumentBuilders.ReindexBlocks(new List<string> { breaks, next }, 1));

            Assert.Equal(1, (int)result[0]["insertPageBreak"]["location"]["index"]);
            Assert.Equal(3, (int)result[1]["insertPageBreak"]["location"]["index"]);
            Assert.Equal(5, (int)result[2]["insertText"]["location"]["index"]);
        }

        [Fact]
        public void ReindexBlocks_SectionBreakOccupiesNewlinePlusBreak()
        {
            // "A newline character will be inserted before the section break" — two indices.
            string brk = "[" + InsertRequestBuilders.InsertSectionBreak(1, "NEXT_PAGE") + "]";
            string next = "[" + TextRequestBuilders.InsertText("After\n", 1) + "]";

            var result = JArray.Parse(DocumentBuilders.ReindexBlocks(new List<string> { brk, next }, 1));

            Assert.Equal(3, (int)result[1]["insertText"]["location"]["index"]);
        }

        [Fact]
        public void ReindexBlocks_InlineImageOccupiesOneIndex()
        {
            string img = "[" + InsertRequestBuilders.InsertInlineImage(1, "https://x/y.png", null, null) + "]";
            string next = "[" + TextRequestBuilders.InsertText("After\n", 1) + "]";

            var result = JArray.Parse(DocumentBuilders.ReindexBlocks(new List<string> { img, next }, 1));

            Assert.Equal(2, (int)result[1]["insertText"]["location"]["index"]);
        }

        #endregion

        #region Mixed index spaces are refused

        [Fact]
        public void IndexSpaces_DistinguishesBodyFromEachSegment()
        {
            string body = "[" + TextRequestBuilders.InsertText("B\n", 1) + "]";
            string header = "[" + TextRequestBuilders.InsertText("H\n", 1, "kix.h") + "]";
            string footer = "[" + TextRequestBuilders.InsertText("F\n", 1, "kix.f") + "]";
            string noSpace = "[" + TextRequestBuilders.ReplaceAllText("a", "b", true) + "]";

            Assert.Single(DocumentBuilders.IndexSpaces(new List<string> { body, noSpace }));
            Assert.Single(DocumentBuilders.IndexSpaces(new List<string> { header, header }));
            Assert.Equal(2, DocumentBuilders.IndexSpaces(new List<string> { body, header }).Count);
            Assert.Equal(3, DocumentBuilders.IndexSpaces(new List<string> { body, header, footer }).Count);
        }

        #endregion

        #region Unplaced blocks are detected

        [Fact]
        public void CountInsertsAtSegmentStart_FlagsBlocksThatBypassedTheAggregator()
        {
            string placed = DocumentBuilders.ReindexBlocks(new List<string>
            {
                "[" + TextRequestBuilders.InsertText("Title\n", 1) + "]",
                "[" + TextRequestBuilders.InsertText("Body\n", 1) + "]",
            }, 1);
            string strayImage = InsertRequestBuilders.InsertInlineImage(1, "https://x/y.png", null, null);

            Assert.Equal(1, DocumentBuilders.CountInsertsAtSegmentStart(DocumentBuilders.AssembleRequestsArray(new[] { placed })));
            Assert.Equal(2, DocumentBuilders.CountInsertsAtSegmentStart(DocumentBuilders.AssembleRequestsArray(new[] { placed, strayImage })));
        }

        [Fact]
        public void CountContentInserts_SeesEveryInsertKind()
        {
            var fixedOk = new[] { TextRequestBuilders.ReplaceAllText("a", "b", true), InsertRequestBuilders.ReplaceImage("id", "https://x") };
            var misrouted = new[] { InsertRequestBuilders.InsertInlineImage(1, "https://x", null, null), InsertRequestBuilders.InsertPageBreak(1) };

            Assert.Equal(0, DocumentBuilders.CountContentInserts(fixedOk));
            Assert.Equal(2, DocumentBuilders.CountContentInserts(misrouted));
        }

        #endregion

        #region Images one per line

        [Fact]
        public void InsertImages_OwnParagraph_StacksAndTalliesTheBreaks()
        {
            // Two images each followed by a paragraph break: image at 1, break at 2, image at 3,
            // break at 4 — four indices in total, so the next block starts at 5.
            string images = "[" + string.Join(",", InsertRequestBuilders.InsertInlineImagesSequential(
                new List<string> { "https://x/a.png", "https://x/b.png" }, new List<double?>(), new List<double?>(), true)) + "]";
            string next = "[" + TextRequestBuilders.InsertText("After\n", 1) + "]";

            var result = JArray.Parse(DocumentBuilders.ReindexBlocks(new List<string> { images, next }, 1));

            Assert.Equal(1, (int)result[0]["insertInlineImage"]["location"]["index"]);
            Assert.Equal("\n", (string)result[1]["insertText"]["text"]);
            Assert.Equal(3, (int)result[2]["insertInlineImage"]["location"]["index"]);
            Assert.Equal(5, (int)result[4]["insertText"]["location"]["index"]);
        }

        #endregion

        #region Table growth is tallied by Request Aggregator

        [Fact]
        public void ReindexBlocks_CountsTableStructuralGrowth()
        {
            // A table's footprint is far larger than the text its cells hold: Docs adds a paragraph
            // before it, a table start marker, one marker per row, two indices per cell (the cell
            // marker and the cell's own paragraph) and a table end marker. For a 1x1 table that is
            // rows * (2 * columns + 1) + 3 = 6, plus the 1-character "A" fill = 7, so a block placed
            // after it starts at 1 + 7 = 8. Same layout constants as AecHelperBuilders' live-verified
            // FirstCellOffset / stride math.
            string tableBlock = AecHelperBuilders.DataTreeToTable(
                new List<IList<string>> { new List<string> { "A" } }, false, false);
            string pageBreakBlock = "[" + InsertRequestBuilders.InsertPageBreak(1) + "]";

            var result = JArray.Parse(DocumentBuilders.ReindexBlocks(new List<string> { tableBlock, pageBreakBlock }, 1));

            var pageBreakReq = result.First(r => r["insertPageBreak"] != null);
            Assert.Equal(8, (int)pageBreakReq["insertPageBreak"]["location"]["index"]);
        }

        [Fact]
        public void ReindexBlocks_CountsTableGrowth_ForA2x2Table()
        {
            // 2x2 with single-character cells: structure 2 * (2 * 2 + 1) + 3 = 13, plus 4 cell
            // characters = 17. Next block starts at 1 + 17 = 18.
            string tableBlock = AecHelperBuilders.DataTreeToTable(
                new List<IList<string>>
                {
                    new List<string> { "A", "B" },
                    new List<string> { "C", "D" }
                }, false, false);
            string pageBreakBlock = "[" + InsertRequestBuilders.InsertPageBreak(1) + "]";

            var result = JArray.Parse(DocumentBuilders.ReindexBlocks(new List<string> { tableBlock, pageBreakBlock }, 1));

            var pageBreakReq = result.First(r => r["insertPageBreak"] != null);
            Assert.Equal(18, (int)pageBreakReq["insertPageBreak"]["location"]["index"]);
        }

        #endregion
    }
}
