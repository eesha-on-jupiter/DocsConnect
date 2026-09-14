using System.Collections.Generic;
using System.Linq;
using Xunit;
using Newtonsoft.Json.Linq;
using DocsConnect;

namespace DocsConnect.Tests
{
    // Proves style chaining: wiring one self-indexed block into another Text/Paragraph component
    // layers a trait onto the SAME text instead of inserting a second copy, and — the property the
    // whole design rests on — leaves every downstream block's position untouched once Request
    // Aggregator re-indexes the canvas.
    public class BlockChainingTests
    {
        // one Heading Text component's output: insertText("Title\n") + updateParagraphStyle
        private static JArray HeadingBlock(string text = "Title", string segmentId = null)
        {
            return SharedBuilders.FlattenRequests(
                ParagraphRequestBuilders.InsertStyledParagraphLines(
                    new List<string> { text },
                    new List<string> { "HEADING_1" },
                    null, null, null, null, null, segmentId));
        }

        #region Span extraction

        [Fact]
        public void ExtractSpans_CoversInsertedTextIncludingTrailingNewline()
        {
            var spans = BlockBuilders.ExtractSpans(HeadingBlock());

            var span = Assert.Single(spans);
            Assert.Equal(1, span.Start);
            Assert.Equal(7, span.End);   // "Title\n" = 6 chars, self-indexed from 1
        }

        [Fact]
        public void ExtractSpans_OneSpanPerLineOfAMultiLineBlock()
        {
            var block = SharedBuilders.FlattenRequests(
                ParagraphRequestBuilders.InsertStyledParagraphLines(
                    new List<string> { "One", "Two", "Three" },
                    new List<string> { "HEADING_1" },
                    null, null, null, null, null));

            var spans = BlockBuilders.ExtractSpans(block);

            Assert.Equal(3, spans.Count);
            Assert.Equal((1, 5), (spans[0].Start, spans[0].End));    // "One\n"
            Assert.Equal((5, 9), (spans[1].Start, spans[1].End));    // "Two\n"
            Assert.Equal((9, 15), (spans[2].Start, spans[2].End));   // "Three\n"
        }

        [Fact]
        public void ExtractSpans_BlockThatInsertsNoText_YieldsNothing()
        {
            // Replace All Text has no location and no span to target
            var block = SharedBuilders.FlattenRequests(
                new[] { TextRequestBuilders.ReplaceAllText("foo", "bar", true) });

            Assert.Empty(BlockBuilders.ExtractSpans(block));
        }

        [Fact]
        public void ExtractSpans_SameParagraphRun_IsOneSpanCoveringTheWholeRun()
        {
            // InsertStyledRun emits ONE insertText for the joined run
            var block = SharedBuilders.FlattenRequests(
                TextRequestBuilders.InsertStyledRun(
                    new List<string> { "a", "b", "c" }, "", null, null, null, null,
                    null, null, null, null, null, null, newParagraphAfter: true));

            var span = Assert.Single(BlockBuilders.ExtractSpans(block));
            Assert.Equal(1, span.Start);
            Assert.Equal(5, span.End);   // "abc\n"
        }

        #endregion

        #region Appending a trait

        [Fact]
        public void AppendTextStyle_AddsColorOverTheHeadingsOwnSpan_WithoutASecondInsert()
        {
            var block = HeadingBlock();
            var result = SharedBuilders.FlattenRequests(
                BlockBuilders.AppendTextStyle(block, BlockBuilders.ExtractSpans(block),
                    null, null, null, null, null, null,
                    new List<string> { "#FF0000" }, null, null, null));

            // the text is inserted exactly once — this is the whole point
            Assert.Single(result, r => r["insertText"] != null);

            // heading style and color both land on the same range
            var para = result.Single(r => r["updateParagraphStyle"] != null);
            var text = result.Single(r => r["updateTextStyle"] != null);
            Assert.Equal("HEADING_1", (string)para["updateParagraphStyle"]["paragraphStyle"]["namedStyleType"]);
            Assert.Equal("foregroundColor", (string)text["updateTextStyle"]["fields"]);
            Assert.Equal((int)para["updateParagraphStyle"]["range"]["startIndex"],
                         (int)text["updateTextStyle"]["range"]["startIndex"]);
            Assert.Equal((int)para["updateParagraphStyle"]["range"]["endIndex"],
                         (int)text["updateTextStyle"]["range"]["endIndex"]);
        }

        [Fact]
        public void AppendTextStyle_ChainedTraitComesAfterTheBlock_SoItLayersOnTop()
        {
            var block = HeadingBlock();
            var result = SharedBuilders.FlattenRequests(
                BlockBuilders.AppendTextStyle(block, BlockBuilders.ExtractSpans(block),
                    true, null, null, null, null, null, null, null, null, null));

            // Google applies requests in array order, so the appended trait must come last
            Assert.Equal(3, result.Count);
            Assert.NotNull(result[0]["insertText"]);
            Assert.NotNull(result[1]["updateParagraphStyle"]);
            Assert.NotNull(result[2]["updateTextStyle"]);
        }

        [Fact]
        public void AppendTextStyle_NoTraitSupplied_LeavesTheBlockUnchanged()
        {
            // an empty fields mask would be rejected by the API rather than ignored
            var block = HeadingBlock();
            var result = BlockBuilders.AppendTextStyle(block, BlockBuilders.ExtractSpans(block),
                null, null, null, null, null, null, null, null, null, null);

            Assert.Equal(block.Count, result.Length);
        }

        [Fact]
        public void AppendTextStyle_OneValueCoversEveryLine_AListStylesEachLine()
        {
            var block = SharedBuilders.FlattenRequests(
                TextRequestBuilders.InsertStyledTextLines(
                    new List<string> { "One", "Two", "Three" },
                    null, null, null, null, null, null, null, null, null, null));
            var spans = BlockBuilders.ExtractSpans(block);

            var single = SharedBuilders.FlattenRequests(
                BlockBuilders.AppendTextStyle(block, spans, null, null, null, null,
                    null, null, new List<string> { "#FF0000" }, null, null, null));
            var perLine = SharedBuilders.FlattenRequests(
                BlockBuilders.AppendTextStyle(block, spans, null, null, null, null,
                    null, null, new List<string> { "#FF0000", "#00FF00", "#0000FF" }, null, null, null));

            // the source block carries its own (trait-less) updateTextStyle per line, so pick out
            // only the ones the chain appended
            List<double> Reds(JArray reqs) => reqs
                .Select(r => r["updateTextStyle"]?["textStyle"]?["foregroundColor"]?["color"]?["rgbColor"]?["red"])
                .Where(t => t != null)
                .Select(t => (double)t)
                .ToList();

            var singleColors = Reds(single);
            var perLineColors = Reds(perLine);

            Assert.Equal(new List<double> { 1.0, 1.0, 1.0 }, singleColors);          // last value repeats
            Assert.Equal(new List<double> { 1.0, 0.0, 0.0 }, perLineColors);         // index-matched
        }

        [Fact]
        public void AppendStyle_InheritsSegmentIdFromTheIncomingBlock()
        {
            var block = HeadingBlock(segmentId: "hdr123");
            var result = SharedBuilders.FlattenRequests(
                BlockBuilders.AppendTextStyle(block, BlockBuilders.ExtractSpans(block),
                    true, null, null, null, null, null, null, null, null, null));

            var styled = result.Single(r => r["updateTextStyle"] != null);
            Assert.Equal("hdr123", (string)styled["updateTextStyle"]["range"]["segmentId"]);
        }

        [Fact]
        public void AppendParagraphStyle_AddsAlignmentToAnExistingTextBlock()
        {
            var block = SharedBuilders.FlattenRequests(
                TextRequestBuilders.InsertStyledTextLines(new List<string> { "Body" },
                    true, null, null, null, null, null, null, null, null, null));

            var result = SharedBuilders.FlattenRequests(
                BlockBuilders.AppendParagraphStyle(block, BlockBuilders.ExtractSpans(block),
                    null, new List<string> { "CENTER" }, null, null, null, null));

            var para = result.Single(r => r["updateParagraphStyle"] != null);
            Assert.Equal("CENTER", (string)para["updateParagraphStyle"]["paragraphStyle"]["alignment"]);
            Assert.Equal("alignment", (string)para["updateParagraphStyle"]["fields"]);
        }

        [Fact]
        public void AppendBullets_BulletsEveryLineOfTheIncomingBlock()
        {
            var block = SharedBuilders.FlattenRequests(
                TextRequestBuilders.InsertStyledTextLines(new List<string> { "a", "b" },
                    true, null, null, null, null, null, null, null, null, null));

            var result = SharedBuilders.FlattenRequests(
                BlockBuilders.AppendBullets(block, BlockBuilders.ExtractSpans(block), "BULLET_CHECKBOX"));

            var bullets = result.Where(r => r["createParagraphBullets"] != null).ToList();
            Assert.Equal(2, bullets.Count);
            Assert.All(bullets, b => Assert.Equal("BULLET_CHECKBOX", (string)b["createParagraphBullets"]["bulletPreset"]));
        }

        #endregion

        #region The property everything rests on: chaining is index-neutral

        [Fact]
        public void ChainedBlock_DoesNotShiftAnythingDownstreamOfIt()
        {
            // Canvas: [Heading Text -> Text Color -> Bold Text] then [Insert Text "After"],
            // both into one Request Aggregator. The chained traits must not move "After".
            var heading = HeadingBlock();
            var spans = BlockBuilders.ExtractSpans(heading);

            var colored = SharedBuilders.FlattenRequests(
                BlockBuilders.AppendTextStyle(heading, spans, null, null, null, null,
                    null, null, new List<string> { "#FF0000" }, null, null, null));
            var coloredAndBold = BlockBuilders.AppendTextStyle(colored, BlockBuilders.ExtractSpans(colored),
                true, null, null, null, null, null, null, null, null, null);

            string trailing = "[" + string.Join(",",
                TextRequestBuilders.InsertTextLines(new List<string> { "After" })) + "]";

            var unchained = JArray.Parse(DocumentBuilders.ReindexBlocks(
                new List<string> { heading.ToString(Newtonsoft.Json.Formatting.None), trailing }, 1));
            var chained = JArray.Parse(DocumentBuilders.ReindexBlocks(
                new List<string> { "[" + string.Join(",", coloredAndBold) + "]", trailing }, 1));

            int unchainedAfter = unchained.Last(r => r["insertText"] != null)["insertText"]["location"].Value<int>("index");
            int chainedAfter = chained.Last(r => r["insertText"] != null)["insertText"]["location"].Value<int>("index");

            Assert.Equal(7, unchainedAfter);            // right after "Title\n"
            Assert.Equal(unchainedAfter, chainedAfter); // two chained traits changed nothing downstream
        }

        [Fact]
        public void ChainedTraits_TravelWithTheirBlockWhenRequestAggregatorShiftsIt()
        {
            // Same chained heading, but now preceded by another block so Request Aggregator has to
            // shift it — the appended style must move with the text it targets.
            var heading = HeadingBlock();
            var chained = BlockBuilders.AppendTextStyle(heading, BlockBuilders.ExtractSpans(heading),
                true, null, null, null, null, null, null, null, null, null);

            string leading = "[" + string.Join(",",
                TextRequestBuilders.InsertTextLines(new List<string> { "Intro" })) + "]";   // "Intro\n" = 6

            var result = JArray.Parse(DocumentBuilders.ReindexBlocks(
                new List<string> { leading, "[" + string.Join(",", chained) + "]" }, 1));

            var headingInsert = result.Last(r => r["insertText"] != null);
            var styled = result.Single(r => r["updateTextStyle"] != null);

            Assert.Equal(7, (int)headingInsert["insertText"]["location"]["index"]);
            Assert.Equal(7, (int)styled["updateTextStyle"]["range"]["startIndex"]);
            Assert.Equal(13, (int)styled["updateTextStyle"]["range"]["endIndex"]);   // "Title\n"
        }

        #endregion

        #region Formatted table cells from a component's raw output list

        [Fact]
        public void ExpandCells_TextColorListBecomesOneColouredCellPerLine()
        {
            // Text Color with three texts and three colours emits six requests as a list. Wired
            // into a table row, that must read as three cells, each carrying its own colour.
            var list = TextRequestBuilders.InsertStyledTextLines(
                new List<string> { "Red", "Green", "Blue" }, null, null, null, null, null, null,
                new List<string> { "#C00000", "#006600", "#000099" }, null, null, null);

            var cells = BlockBuilders.ExpandCells(list);

            Assert.Equal(3, cells.Count);
            for (int i = 0; i < 3; i++)
            {
                var content = BlockBuilders.ExtractCellContent(cells[i]);
                Assert.Equal(new[] { "Red", "Green", "Blue" }[i], content.Text);
                Assert.Single(content.Styles);
                var range = content.Styles[0]["updateTextStyle"]["range"];
                Assert.Equal(1, (int)range["startIndex"]);
                Assert.Equal(1 + content.Text.Length, (int)range["endIndex"]);
            }
            Assert.Contains("0.75", BlockBuilders.ExtractCellContent(cells[0]).Styles[0].ToString()); // #C00000 red channel
        }

        [Fact]
        public void ExpandCells_MixesPlainAndFormattedCellsInOrder()
        {
            var bold = TextRequestBuilders.InsertStyledTextLines(
                new List<string> { "Total" }, true, null, null, null, null, null, null, null, null, null);
            var items = new List<string> { "Item" };
            items.AddRange(bold);
            items.Add("42");

            var cells = BlockBuilders.ExpandCells(items);

            Assert.Equal(3, cells.Count);
            Assert.Equal("Item", cells[0]);
            Assert.Equal("Total", BlockBuilders.ExtractCellContent(cells[1]).Text);
            Assert.Equal("42", cells[2]);
        }

        [Fact]
        public void ExpandCells_TwoComponentsInOneRowStayApart()
        {
            // Two blocks back to back (both restart at index 1) — four cells, not one merged block.
            var a = TextRequestBuilders.InsertStyledTextLines(new List<string> { "A1", "A2" }, true, null, null, null, null, null, null, null, null, null);
            var b = TextRequestBuilders.InsertStyledTextLines(new List<string> { "B1", "B2" }, null, true, null, null, null, null, null, null, null, null);
            var items = new List<string>(a); items.AddRange(b);

            var cells = BlockBuilders.ExpandCells(items);

            Assert.Equal(4, cells.Count);
            Assert.Equal("B1", BlockBuilders.ExtractCellContent(cells[2]).Text);
            Assert.True((bool)BlockBuilders.ExtractCellContent(cells[3]).Styles[0]["updateTextStyle"]["textStyle"]["italic"]);
        }

        #endregion

    }
}
