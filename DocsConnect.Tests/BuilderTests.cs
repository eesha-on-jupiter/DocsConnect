using System.Linq;
using System.Collections.Generic;
using Xunit;
using Newtonsoft.Json.Linq;
using DocsConnect;

namespace DocsConnect.Tests
{
    // Proves each static builder emits the JSON shape the Google Docs API expects
    // (the shapes validated live by api-tester). Pure functions in, JSON out.
    public class BuilderTests
    {
        #region Text

        [Fact]
        public void InsertText_Shape()
        {
            var o = JObject.Parse(TextRequestBuilders.InsertText("Hello", 5));
            Assert.Equal("Hello", (string)o["insertText"]["text"]);
            Assert.Equal(5, (int)o["insertText"]["location"]["index"]);
        }

        [Fact]
        public void InsertText_NullText_DoesNotThrow_EmitsEmpty()
        {
            var o = JObject.Parse(TextRequestBuilders.InsertText(null, 1));
            Assert.Equal("", (string)o["insertText"]["text"]);
        }

        [Fact]
        public void ReplaceAllText_Shape()
        {
            var o = JObject.Parse(TextRequestBuilders.ReplaceAllText("foo", "bar", true));
            Assert.Equal("foo", (string)o["replaceAllText"]["containsText"]["text"]);
            Assert.True((bool)o["replaceAllText"]["containsText"]["matchCase"]);
            Assert.Equal("bar", (string)o["replaceAllText"]["replaceText"]);
        }

        [Fact]
        public void DeleteContentRange_Shape()
        {
            var o = JObject.Parse(TextRequestBuilders.DeleteContentRange(3, 9));
            Assert.Equal(3, (int)o["deleteContentRange"]["range"]["startIndex"]);
            Assert.Equal(9, (int)o["deleteContentRange"]["range"]["endIndex"]);
        }

        [Fact]
        public void UpdateTextStyle_OnlySetFieldsAppearInMask()
        {
            // only bold supplied
            var o = JObject.Parse(TextRequestBuilders.UpdateTextStyle(
                1, 5, true, null, null, null, null, null, null, null, null, null));
            Assert.Equal("bold", (string)o["updateTextStyle"]["fields"]);
            Assert.True((bool)o["updateTextStyle"]["textStyle"]["bold"]);
            Assert.Null(o["updateTextStyle"]["textStyle"]["italic"]);
        }

        [Fact]
        public void UpdateTextStyle_ColorHexBecomesRgb()
        {
            var o = JObject.Parse(TextRequestBuilders.UpdateTextStyle(
                1, 5, null, null, null, null, null, null, "#FF0000", null, null, null));
            var rgb = o["updateTextStyle"]["textStyle"]["foregroundColor"]["color"]["rgbColor"];
            Assert.Equal(1.0, (double)rgb["red"], 3);
            Assert.Equal(0.0, (double)rgb["green"], 3);
            Assert.Contains("foregroundColor", (string)o["updateTextStyle"]["fields"]);
        }

        [Fact]
        public void UpdateTextStyle_ColorCommaRgbBecomesRgb()
        {
            var o = JObject.Parse(TextRequestBuilders.UpdateTextStyle(
                1, 5, null, null, null, null, null, null, "255,0,0", "0,255,0", null, null));
            var fg = o["updateTextStyle"]["textStyle"]["foregroundColor"]["color"]["rgbColor"];
            var bg = o["updateTextStyle"]["textStyle"]["backgroundColor"]["color"]["rgbColor"];
            Assert.Equal(1.0, (double)fg["red"], 3);
            Assert.Equal(1.0, (double)bg["green"], 3);
            Assert.Contains("foregroundColor", (string)o["updateTextStyle"]["fields"]);
            Assert.Contains("backgroundColor", (string)o["updateTextStyle"]["fields"]);
        }

        [Fact]
        public void UpdateTextStyle_UnparseableColorIsSkippedNotResetToDefault()
        {
            var o = JObject.Parse(TextRequestBuilders.UpdateTextStyle(
                1, 5, null, null, null, null, null, null, "not-a-color", "also-bad", null, null));
            Assert.Null(o["updateTextStyle"]["textStyle"]["foregroundColor"]);
            Assert.Null(o["updateTextStyle"]["textStyle"]["backgroundColor"]);
            Assert.DoesNotContain("foregroundColor", (string)o["updateTextStyle"]["fields"]);
            Assert.DoesNotContain("backgroundColor", (string)o["updateTextStyle"]["fields"]);
        }

        [Fact]
        public void UpdateTextStyle_LinkAndBaseline()
        {
            var o = JObject.Parse(TextRequestBuilders.UpdateTextStyle(
                1, 5, null, null, null, null, null, null, null, null, "https://x.com", "SUPERSCRIPT"));
            Assert.Equal("https://x.com", (string)o["updateTextStyle"]["textStyle"]["link"]["url"]);
            Assert.Equal("SUPERSCRIPT", (string)o["updateTextStyle"]["textStyle"]["baselineOffset"]);
        }

        #endregion

        #region Multi-line list builders

        [Fact]
        public void InsertStyledTextLines_ChainsIndexesAndAddsNewlinePerLine()
        {
            var reqs = TextRequestBuilders.InsertStyledTextLines(
                new List<string> { "abc", "de" },
                null, null, null, null,
                null, null, null, null, null, null, null);

            Assert.Equal(4, reqs.Length); // 2 lines x (insertText + updateTextStyle)

            var insert1 = JObject.Parse(reqs[0]);
            Assert.Equal("abc\n", (string)insert1["insertText"]["text"]);
            Assert.Equal(1, (int)insert1["insertText"]["location"]["index"]);

            var insert2 = JObject.Parse(reqs[2]);
            Assert.Equal("de\n", (string)insert2["insertText"]["text"]);
            Assert.Equal(5, (int)insert2["insertText"]["location"]["index"]); // 1 + len("abc\n")
        }

        [Fact]
        public void InsertStyledTextLines_ColorListRepeatsLastValue()
        {
            var reqs = TextRequestBuilders.InsertStyledTextLines(
                new List<string> { "a", "b", "c" },
                null, null, null, null,
                null, null, new List<string> { "#FF0000" }, null, null, null, null);

            var style1 = JObject.Parse(reqs[1]);
            var style3 = JObject.Parse(reqs[5]);
            Assert.Equal(1.0, (double)style1["updateTextStyle"]["textStyle"]["foregroundColor"]["color"]["rgbColor"]["red"], 3);
            Assert.Equal(1.0, (double)style3["updateTextStyle"]["textStyle"]["foregroundColor"]["color"]["rgbColor"]["red"], 3);
        }

        [Fact]
        public void InsertStyledTextLines_EqualLengthColorListIsOneToOne()
        {
            var reqs = TextRequestBuilders.InsertStyledTextLines(
                new List<string> { "Hello", "World" },
                null, null, null, null,
                null, null, new List<string> { "#FF0000", "#0000FF" }, null, null, null, null);

            var style1 = JObject.Parse(reqs[1]);
            var style2 = JObject.Parse(reqs[3]);
            var rgb1 = style1["updateTextStyle"]["textStyle"]["foregroundColor"]["color"]["rgbColor"];
            var rgb2 = style2["updateTextStyle"]["textStyle"]["foregroundColor"]["color"]["rgbColor"];
            Assert.Equal(1.0, (double)rgb1["red"], 3);
            Assert.Equal(0.0, (double)rgb1["blue"], 3);
            Assert.Equal(0.0, (double)rgb2["red"], 3);
            Assert.Equal(1.0, (double)rgb2["blue"], 3);
        }

        [Fact]
        public void InsertStyledTextLines_EmptyTraitListLeavesFieldUnset()
        {
            var reqs = TextRequestBuilders.InsertStyledTextLines(
                new List<string> { "a" },
                null, null, null, null,
                null, null, null, null, null, null, null);

            var style = JObject.Parse(reqs[1]);
            Assert.Equal("", (string)style["updateTextStyle"]["fields"]);
        }

        [Fact]
        public void InsertStyledRun_JoinsSegmentsIntoOneParagraphWithPerSegmentStyle()
        {
            // "Hi" + "Bob" joined with no separator -> one insertText "HiBob\n", each word its own range
            var reqs = TextRequestBuilders.InsertStyledRun(
                new List<string> { "Hi", "Bob" }, "",
                null, null, null, null,
                null, null, new List<string> { "#FF0000", "#0000FF" }, null, null, null,
                newParagraphAfter: true);

            Assert.Equal(3, reqs.Length); // 1 insertText + 2 updateTextStyle (one per segment)

            var insert = JObject.Parse(reqs[0]);
            Assert.Equal("HiBob\n", (string)insert["insertText"]["text"]);
            Assert.Equal(1, (int)insert["insertText"]["location"]["index"]);

            var style1 = JObject.Parse(reqs[1]);
            Assert.Equal(1, (int)style1["updateTextStyle"]["range"]["startIndex"]);
            Assert.Equal(3, (int)style1["updateTextStyle"]["range"]["endIndex"]); // "Hi"
            Assert.Equal(1.0, (double)style1["updateTextStyle"]["textStyle"]["foregroundColor"]["color"]["rgbColor"]["red"], 3);

            var style2 = JObject.Parse(reqs[2]);
            Assert.Equal(3, (int)style2["updateTextStyle"]["range"]["startIndex"]);
            Assert.Equal(6, (int)style2["updateTextStyle"]["range"]["endIndex"]); // "Bob"
            Assert.Equal(1.0, (double)style2["updateTextStyle"]["textStyle"]["foregroundColor"]["color"]["rgbColor"]["blue"], 3);
        }

        [Fact]
        public void InsertStyledRun_SeparatorInsertedBetweenSegmentsOnly()
        {
            var reqs = TextRequestBuilders.InsertStyledRun(
                new List<string> { "The", "quick", "fox" }, " ",
                null, null, null, null,
                null, null, null, null, null, null,
                newParagraphAfter: false);

            var insert = JObject.Parse(reqs[0]);
            Assert.Equal("The quick fox", (string)insert["insertText"]["text"]);
        }

        [Fact]
        public void InsertTextLines_ChainsSequentially()
        {
            var reqs = TextRequestBuilders.InsertTextLines(new List<string> { "ab", "c" });
            Assert.Equal(2, reqs.Length);
            Assert.Equal(1, (int)JObject.Parse(reqs[0])["insertText"]["location"]["index"]);
            Assert.Equal(4, (int)JObject.Parse(reqs[1])["insertText"]["location"]["index"]); // 1 + len("ab\n")
        }

        [Fact]
        public void ReplaceAllTextPairs_ReplaceListRepeatsLastValue()
        {
            var reqs = TextRequestBuilders.ReplaceAllTextPairs(
                new List<string> { "a", "b", "c" }, new List<string> { "X" }, true);
            Assert.Equal(3, reqs.Length);
            Assert.Equal("X", (string)JObject.Parse(reqs[2])["replaceAllText"]["replaceText"]);
        }

        #endregion

        #region Paragraph

        [Fact]
        public void UpdateParagraphStyle_NamedStyleAndMask()
        {
            var o = JObject.Parse(ParagraphRequestBuilders.UpdateParagraphStyle(
                1, 5, "HEADING_1", "CENTER", null, null, null, null));
            Assert.Equal("HEADING_1", (string)o["updateParagraphStyle"]["paragraphStyle"]["namedStyleType"]);
            Assert.Equal("CENTER", (string)o["updateParagraphStyle"]["paragraphStyle"]["alignment"]);
            var fields = (string)o["updateParagraphStyle"]["fields"];
            Assert.Contains("namedStyleType", fields);
            Assert.Contains("alignment", fields);
        }

        [Fact]
        public void CreateParagraphBullets_Preset()
        {
            var o = JObject.Parse(ParagraphRequestBuilders.CreateParagraphBullets(1, 5, "BULLET_CHECKBOX"));
            Assert.Equal("BULLET_CHECKBOX", (string)o["createParagraphBullets"]["bulletPreset"]);
        }

        [Fact]
        public void CreateParagraphBullets_BlankPreset_Defaults()
        {
            var o = JObject.Parse(ParagraphRequestBuilders.CreateParagraphBullets(1, 5, ""));
            Assert.Equal("BULLET_DISC_CIRCLE_SQUARE", (string)o["createParagraphBullets"]["bulletPreset"]);
        }

        [Fact]
        public void InsertStyledParagraphLines_ChainsIndexesPerParagraph()
        {
            var reqs = ParagraphRequestBuilders.InsertStyledParagraphLines(
                new List<string> { "abc", "de" },
                null, null, null, null, null, null);

            Assert.Equal(4, reqs.Length);
            Assert.Equal(1, (int)JObject.Parse(reqs[0])["insertText"]["location"]["index"]);
            Assert.Equal(5, (int)JObject.Parse(reqs[2])["insertText"]["location"]["index"]);
        }

        [Fact]
        public void InsertBulletedParagraphLines_OneBulletRequestPerLine()
        {
            var reqs = ParagraphRequestBuilders.InsertBulletedParagraphLines(
                new List<string> { "a", "b" }, "BULLET_CHECKBOX");

            Assert.Equal(4, reqs.Length);
            Assert.Equal("BULLET_CHECKBOX", (string)JObject.Parse(reqs[1])["createParagraphBullets"]["bulletPreset"]);
            Assert.Equal("BULLET_CHECKBOX", (string)JObject.Parse(reqs[3])["createParagraphBullets"]["bulletPreset"]);
        }

        #endregion

        #region Insert

        [Fact]
        public void InsertInlineImage_NoSizeWhenZero()
        {
            var o = JObject.Parse(InsertRequestBuilders.InsertInlineImage(1, "https://img", null, null));
            Assert.Equal("https://img", (string)o["insertInlineImage"]["uri"]);
            Assert.Null(o["insertInlineImage"]["objectSize"]);
        }

        [Fact]
        public void InsertInlineImage_SizeWhenSupplied()
        {
            var o = JObject.Parse(InsertRequestBuilders.InsertInlineImage(1, "https://img", 100, 50));
            Assert.Equal(100.0, (double)o["insertInlineImage"]["objectSize"]["width"]["magnitude"], 3);
            Assert.Equal("PT", (string)o["insertInlineImage"]["objectSize"]["width"]["unit"]);
        }

        [Fact]
        public void InsertTable_ClampsToMinimumOne()
        {
            var o = JObject.Parse(InsertRequestBuilders.InsertTable(1, 0, 0));
            Assert.Equal(1, (int)o["insertTable"]["rows"]);
            Assert.Equal(1, (int)o["insertTable"]["columns"]);
        }

        [Fact]
        public void InsertSectionBreak_DefaultType()
        {
            var o = JObject.Parse(InsertRequestBuilders.InsertSectionBreak(1, ""));
            Assert.Equal("NEXT_PAGE", (string)o["insertSectionBreak"]["sectionType"]);
        }

        [Fact]
        public void InsertInlineImagesSequential_AdvancesOneIndexPerImage()
        {
            var reqs = InsertRequestBuilders.InsertInlineImagesSequential(
                new List<string> { "https://a", "https://b" }, new List<double?>(), new List<double?>());

            Assert.Equal(1, (int)JObject.Parse(reqs[0])["insertInlineImage"]["location"]["index"]);
            Assert.Equal(2, (int)JObject.Parse(reqs[1])["insertInlineImage"]["location"]["index"]);
        }

        [Fact]
        public void InsertPageBreaksSequential_AdvancesTwoIndicesPerBreak()
        {
            var reqs = InsertRequestBuilders.InsertPageBreaksSequential(3);

            Assert.Equal(1, (int)JObject.Parse(reqs[0])["insertPageBreak"]["location"]["index"]);
            Assert.Equal(5, (int)JObject.Parse(reqs[2])["insertPageBreak"]["location"]["index"]); // break + newline each
        }

        [Fact]
        public void InsertSectionBreaksSequential_AdvancesTwoIndicesPerBreak()
        {
            var reqs = InsertRequestBuilders.InsertSectionBreaksSequential(new List<string> { "NEXT_PAGE", "CONTINUOUS" });

            Assert.Equal(1, (int)JObject.Parse(reqs[0])["insertSectionBreak"]["location"]["index"]);
            Assert.Equal(3, (int)JObject.Parse(reqs[1])["insertSectionBreak"]["location"]["index"]); // newline + break each
            Assert.Equal("CONTINUOUS", (string)JObject.Parse(reqs[1])["insertSectionBreak"]["sectionType"]);
        }

        #endregion

        #region Structure

        [Fact]
        public void CreateHeader_DefaultType()
        {
            var o = JObject.Parse(StructureRequestBuilders.CreateHeader(""));
            Assert.Equal("DEFAULT", (string)o["createHeader"]["type"]);
        }

        [Fact]
        public void UpdateDocumentStyle_PageSizeGroupsWidthHeight()
        {
            var o = JObject.Parse(StructureRequestBuilders.UpdateDocumentStyle(
                72, null, null, null, 612, 792, null));
            Assert.Equal(72.0, (double)o["updateDocumentStyle"]["documentStyle"]["marginTop"]["magnitude"], 3);
            Assert.Equal(612.0, (double)o["updateDocumentStyle"]["documentStyle"]["pageSize"]["width"]["magnitude"], 3);
            var fields = (string)o["updateDocumentStyle"]["fields"];
            Assert.Contains("marginTop", fields);
            Assert.Contains("pageSize", fields);
        }

        #endregion

        #region Shared helpers

        [Theory]
        [InlineData("#FFFFFF", 1.0, 1.0, 1.0)]
        [InlineData("#000000", 0.0, 0.0, 0.0)]
        [InlineData("3399FF", 0.2, 0.6, 1.0)]
        public void ParseHexRgb_Channels(string hex, double r, double g, double b)
        {
            var rgb = SharedBuilders.ParseHexRgb(hex);
            Assert.Equal(r, (double)rgb["red"], 2);
            Assert.Equal(g, (double)rgb["green"], 2);
            Assert.Equal(b, (double)rgb["blue"], 2);
        }

        [Theory]
        [InlineData("notacolor")]
        [InlineData("")]
        [InlineData("#GGGGGG")]
        [InlineData("255,0,0,0,0")]
        [InlineData("255,256,0")]
        [InlineData("255,-1,0")]
        public void ParseHexRgb_InvalidReturnsNull(string hex)
        {
            Assert.Null(SharedBuilders.ParseHexRgb(hex));
        }

        [Theory]
        [InlineData("255,255,255", 1.0, 1.0, 1.0)]
        [InlineData("0,0,0", 0.0, 0.0, 0.0)]
        [InlineData("51,153,255", 0.2, 0.6, 1.0)]
        [InlineData("51,153,255,255", 0.2, 0.6, 1.0)]
        [InlineData(" 51 , 153 , 255 ", 0.2, 0.6, 1.0)]
        public void ParseHexRgb_CommaRgbChannels(string csv, double r, double g, double b)
        {
            var rgb = SharedBuilders.ParseHexRgb(csv);
            Assert.Equal(r, (double)rgb["red"], 2);
            Assert.Equal(g, (double)rgb["green"], 2);
            Assert.Equal(b, (double)rgb["blue"], 2);
        }

        [Fact]
        public void ParseHexRgb_NullReturnsNull()
        {
            Assert.Null(SharedBuilders.ParseHexRgb(null));
        }

        #endregion

        #region Line endings

        [Fact]
        public void InsertText_DropsCarriageReturns_SoIndexTallyMatchesWhatDocsKeeps()
        {
            // Docs strips CR from inserted text. A Windows CRLF must be normalised before the
            // length is counted, or every index after the block is off by one per line.
            string crlf = "Line one.\r\n\r\nLine three:\n";
            string normalised = SharedBuilders.NormalizeLineEndings(crlf);

            Assert.Equal("Line one.\n\nLine three:\n", normalised);
            Assert.DoesNotContain("\r", JObject.Parse(TextRequestBuilders.InsertText(crlf, 1))["insertText"].Value<string>("text"));
        }

        [Theory]
        [InlineData("BULLET_DISC_CIRCLE_SQUARE", true)]
        [InlineData("NUMBERED_DECIMAL_ALPHA_ROMAN", true)]
        [InlineData("", true)]
        [InlineData("0", false)]
        [InlineData("1", false)]
        [InlineData("bullet", false)]
        public void IsBulletPreset_RejectsIndentLevelsAndTypos(string preset, bool expected)
        {
            Assert.Equal(expected, ParagraphRequestBuilders.IsBulletPreset(preset));
        }

        #endregion

        #region Table cell formatting

        [Fact]
        public void DataTreeToTable_CellFormats_EmitTextStyleAndFillPerCell()
        {
            // 1 x 2 table: "A" red + grey fill, "B" bold. Text style ranges land on each cell's
            // final text; the fill addresses the cell by row/column from the table start marker.
            var rows = new List<IList<string>> { new List<string> { "A", "B" } };
            var formats = new List<IList<AecHelperBuilders.CellFormat>>
            {
                new List<AecHelperBuilders.CellFormat>
                {
                    new AecHelperBuilders.CellFormat { TextColor = "#FF0000", Fill = "#EEEEEE" },
                    new AecHelperBuilders.CellFormat { Bold = true },
                }
            };

            var reqs = JArray.Parse(AecHelperBuilders.DataTreeToTable(rows, false, false, formats));

            var styles = reqs.Where(r => r["updateTextStyle"] != null).ToList();
            Assert.Equal(2, styles.Count);
            // cells of a 1x2 table at insert index 1 sit at 5 and 7; "A" fills 5..6, pushing "B" to 8..9
            Assert.Equal(5, (int)styles[0]["updateTextStyle"]["range"]["startIndex"]);
            Assert.Equal("foregroundColor", (string)styles[0]["updateTextStyle"]["fields"]);
            Assert.Equal(8, (int)styles[1]["updateTextStyle"]["range"]["startIndex"]);
            Assert.True((bool)styles[1]["updateTextStyle"]["textStyle"]["bold"]);

            var fill = reqs.Single(r => r["updateTableCellStyle"] != null)["updateTableCellStyle"];
            var loc = fill["tableRange"]["tableCellLocation"];
            Assert.Equal(2, (int)loc["tableStartLocation"]["index"]); // insert index 1 + the newline before the table
            Assert.Equal(0, (int)loc["rowIndex"]);
            Assert.Equal(0, (int)loc["columnIndex"]);
            Assert.Equal("backgroundColor", (string)fill["fields"]);
        }

        [Fact]
        public void DataTreeToTable_CellFormats_ShiftWithTheBlock()
        {
            // Placed after a 10-character block, the fill's tableStartLocation and the text style
            // ranges move by 10 like every other index in the block.
            var rows = new List<IList<string>> { new List<string> { "A" } };
            var formats = new List<IList<AecHelperBuilders.CellFormat>>
                { new List<AecHelperBuilders.CellFormat> { new AecHelperBuilders.CellFormat { Bold = true, Fill = "#00FF00" } } };
            string table = AecHelperBuilders.DataTreeToTable(rows, false, false, formats);
            string before = "[" + TextRequestBuilders.InsertText("123456789\n", 1) + "]";

            var placed = JArray.Parse(DocumentBuilders.ReindexBlocks(new List<string> { before, table }, 1));

            var fill = placed.Single(r => r["updateTableCellStyle"] != null);
            Assert.Equal(12, (int)fill["updateTableCellStyle"]["tableRange"]["tableCellLocation"]["tableStartLocation"]["index"]);
            var bold = placed.Single(r => r["updateTextStyle"] != null);
            Assert.Equal(15, (int)bold["updateTextStyle"]["range"]["startIndex"]);
        }

        #endregion
    }
}
