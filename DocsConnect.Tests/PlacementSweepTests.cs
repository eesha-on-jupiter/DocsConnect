using System.Collections.Generic;
using System.Linq;
using Xunit;
using Newtonsoft.Json.Linq;
using DocsConnect;

namespace DocsConnect.Tests
{
    // One sweep over EVERY builder that emits content inserts, checking the invariants Request
    // Aggregator's placement rests on. A new content-emitting builder belongs in Blocks() — if it
    // breaks any invariant below, the "inserted at the top of the document" bug is back.
    public class PlacementSweepTests
    {
        private static readonly string[] ContentInsertKeys =
            { "insertText", "insertInlineImage", "insertPageBreak", "insertSectionBreak", "insertTable" };

        // name -> block (a JSON array string, as the components hand it on)
        public static IEnumerable<object[]> Blocks()
        {
            string Arr(IEnumerable<string> reqs) => "[" + string.Join(",", reqs) + "]";

            yield return new object[] { "Insert Text", Arr(TextRequestBuilders.InsertTextLines(new List<string> { "One", "Two" })) };
            yield return new object[] { "Bold Text (styled lines)", Arr(TextRequestBuilders.InsertStyledTextLines(
                new List<string> { "A", "B" }, true, null, null, null, null, null, null, null, null, null)) };
            yield return new object[] { "Styled Text (same-paragraph run)", Arr(TextRequestBuilders.InsertStyledRun(
                new List<string> { "a", "b" }, " ", true, null, null, null, null, null, null, null, null, null, true)) };
            yield return new object[] { "Heading Text (styled paragraphs)", Arr(ParagraphRequestBuilders.InsertStyledParagraphLines(
                new List<string> { "H" }, new List<string> { "HEADING_1" }, null, null, null, null, null)) };
            yield return new object[] { "Bullet Item Text", Arr(ParagraphRequestBuilders.InsertBulletedParagraphLines(
                new List<string> { "x", "y" }, "BULLET_CHECKBOX")) };
            yield return new object[] { "Bullet List (nested)", AecHelperBuilders.BulletList(
                new List<string> { "a", "b", "c" }, new List<int> { 0, 1, 2 }, "BULLET_DISC_CIRCLE_SQUARE") };
            yield return new object[] { "Report Skeleton", AecHelperBuilders.ReportSkeleton(
                "Title", new List<string> { "S1" }, new List<int> { 1 }, new List<string> { "body" }) };
            yield return new object[] { "Report Header Block", AecHelperBuilders.ReportHeaderBlock("P", "001", "2026-09-13", "EJ", "A", "") };
            yield return new object[] { "Insert Image", Arr(InsertRequestBuilders.InsertInlineImagesSequential(
                new List<string> { "https://x/a.png", "https://x/b.png" }, new List<double?>(), new List<double?>())) };
            yield return new object[] { "Insert Image (own paragraph)", Arr(InsertRequestBuilders.InsertInlineImagesSequential(
                new List<string> { "https://x/a.png", "https://x/b.png" }, new List<double?>(), new List<double?>(), true)) };
            yield return new object[] { "Insert Page Break", Arr(InsertRequestBuilders.InsertPageBreaksSequential(2)) };
            yield return new object[] { "Insert Section Break", Arr(InsertRequestBuilders.InsertSectionBreaksSequential(new List<string> { "NEXT_PAGE" })) };
            yield return new object[] { "Data Tree To Table", AecHelperBuilders.DataTreeToTable(
                new List<IList<string>> { new List<string> { "h1", "h2" }, new List<string> { "1", "2" } }, true, true) };
            yield return new object[] { "Fixed Size Table", AecHelperBuilders.FixedSizeTable(
                new List<IList<string>> { new List<string> { "a" } }, 2, 2, false, false) };
        }

        private static int FirstInsertIndex(JArray reqs)
        {
            foreach (var r in reqs)
                foreach (var key in ContentInsertKeys)
                    if (r[key]?["location"]?["index"] != null) return (int)r[key]["location"]["index"];
            return -1;
        }

        [Theory]
        [MemberData(nameof(Blocks))]
        public void EveryBlock_IsSelfIndexedFromOne(string name, string block)
        {
            var reqs = SharedBuilders.FlattenRequests(new[] { block });
            Assert.True(FirstInsertIndex(reqs) == 1, name + " does not start at index 1");
            Assert.Single(DocumentBuilders.SplitSelfIndexedBlocks(reqs));
        }

        [Theory]
        [MemberData(nameof(Blocks))]
        public void EveryBlock_PlacedSecondLandsAfterTheFirst(string name, string block)
        {
            // Placed after "123456789\n" (10 characters), every kind of block must begin at 11 —
            // that is the whole placement contract.
            string before = "[" + TextRequestBuilders.InsertText("123456789\n", 1) + "]";
            var placed = JArray.Parse(DocumentBuilders.ReindexBlocks(new List<string> { before, block }, 1));

            var second = new JArray(placed.Skip(1));
            Assert.True(FirstInsertIndex(second) == 11, name + " placed second starts at " + FirstInsertIndex(second));
        }

        [Theory]
        [MemberData(nameof(Blocks))]
        public void EveryBlock_IsCaughtWhenItBypassesPlacement(string name, string block)
        {
            // In Fixed Requests: refused as a content insert. Next to a placed run in Batch Update:
            // counted as a second insert at the segment start.
            Assert.True(DocumentBuilders.CountContentInserts(new[] { block }) > 0, name + " not recognised as a content insert");

            string placed = DocumentBuilders.ReindexBlocks(new List<string> { "[" + TextRequestBuilders.InsertText("T\n", 1) + "]" }, 1);
            int atStart = DocumentBuilders.CountInsertsAtSegmentStart(DocumentBuilders.AssembleRequestsArray(new[] { placed, block }));
            Assert.True(atStart >= 2, name + " bypassing the aggregator was not detected");
        }

        [Fact]
        public void AllBlocks_MergedIntoOneList_PlaceExactlyAsSeparateWires()
        {
            // Everything through one Merge must come out identical to everything on its own wire:
            // as many blocks as went in, in arrival order, each starting where the previous one's
            // content ends. (Table blocks fill cells from the highest index down, so "indices only
            // grow" is not an invariant — equality with the separate-wire placement is.)
            var all = Blocks().Select(b => (string)b[1]).ToList();
            var flat = SharedBuilders.FlattenRequests(all);

            Assert.Equal(all.Count, DocumentBuilders.SplitSelfIndexedBlocks(flat).Count);

            string separate = DocumentBuilders.ReindexBlocks(all, 1);
            string merged = DocumentBuilders.ReindexBlocks(new List<string> { flat.ToString() }, 1);
            Assert.Equal(separate, merged);

            // and the first block really is at the top, the last block really is last
            var placed = JArray.Parse(separate);
            Assert.Equal(1, FirstInsertIndex(new JArray(placed.Take(2))));
            var lastBlock = SharedBuilders.FlattenRequests(new[] { all[all.Count - 1] });
            var tail = new JArray(placed.Skip(placed.Count - lastBlock.Count));
            Assert.True(FirstInsertIndex(tail) > 1, "last block was not shifted");
        }
    }
}
