using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace DocsConnect
{
    // Builds an insertText + updateTextStyle combo — any mix of bold, italic, font, size, color,
    // highlight, link and baseline in one self-contained block. No index needed. Use this when
    // combining multiple style traits on the same text; the atomic Bold/Italic/etc. components
    // insert a separate copy of the text each, so they should not be wired to the same text if
    // multiple traits are wanted. Text plus Font Size/Font Family/Text Color/Highlight/Link URL/
    // Baseline are all index-matched lists — a list of Text produces one paragraph per item by
    // default, and a shorter trait list repeats its last value. Bold/Italic/Underline/Strikethrough
    // apply uniformly to every line (they have no per-line value of their own). Set Same Paragraph
    // to join all Text items into one continuous paragraph instead — e.g. wire a Sine/Graph Mapper
    // sampled per item into Font Size, or a Gradient into Text Color, to get a wave/gradient effect
    // running across a single sentence.
    public class UpdateTextStyleJsonComponent : GH_Component
    {
        public UpdateTextStyleJsonComponent()
            : base("Styled Text", "TxtStyle",
                "Inserts text with any combination of styles applied — bold, italic, underline, strikethrough, size, font, color, highlight, link, baseline. Only the inputs you connect are applied. Text, Font Size, Font Family, Text Color, Highlight, Link URL, and Baseline are all index-matched lists (a shorter trait list repeats its last value). By default a list of Text produces one paragraph per item; turn on Same Paragraph to join them into one continuous paragraph instead (e.g. for a font-size wave or color gradient across a sentence). Bold/Italic/Underline/Strikethrough apply uniformly to every line. No index needed.",
                PluginUtilities.TabName, PluginUtilities.CategoryACRequestsText)
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "TX", "Text to insert and style. A list places each entry on its own line.", GH_ParamAccess.tree);
            pManager.AddBooleanParameter("Bold", "B", "Bold (connect to apply).", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Italic", "I", "Italic (connect to apply).", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Underline", "U", "Underline (connect to apply).", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Strikethrough", "SK", "Strikethrough (connect to apply).", GH_ParamAccess.item, false);
            pManager.AddNumberParameter("Font Size", "FS", "Font size in points (>0 to apply), index-matched to Text (a shorter list repeats its last value). All branches are flattened into one ordered list.", GH_ParamAccess.tree);
            pManager.AddTextParameter("Font Family", "FF", "Font family name (blank to skip), index-matched to Text (a shorter list repeats its last value). All branches are flattened into one ordered list.", GH_ParamAccess.tree);
            pManager.AddTextParameter("Text Color", "FC", "Foreground color as #RRGGBB (blank to skip), index-matched to Text (a shorter list repeats its last value). All branches are flattened into one ordered list.", GH_ParamAccess.tree);
            pManager.AddTextParameter("Highlight", "HC", "Highlight color as #RRGGBB (blank to skip), index-matched to Text (a shorter list repeats its last value). All branches are flattened into one ordered list.", GH_ParamAccess.tree);
            pManager.AddTextParameter("Link URL", "LK", "Hyperlink URL (blank to skip), index-matched to Text (a shorter list repeats its last value). All branches are flattened into one ordered list.", GH_ParamAccess.tree);
            pManager.AddTextParameter("Baseline", "BL", "Baseline offset: SUPERSCRIPT or SUBSCRIPT (blank to skip), index-matched to Text (a shorter list repeats its last value). All branches are flattened into one ordered list.", GH_ParamAccess.tree);
            pManager.AddTextParameter("Segment ID", "SEG", "Header/Footer/Footnote ID to target that segment instead of the body (blank = body).", GH_ParamAccess.item, "");
            pManager.AddBooleanParameter("Same Paragraph", "SP", "False (default): each Text item lands in its own paragraph, as today. True: all Text items join into ONE paragraph (separated by Separator), each keeping its own style — use this for a font-size wave or color gradient running across a single sentence.", GH_ParamAccess.item, false);
            pManager.AddTextParameter("Separator", "SEP", "Only used when Same Paragraph is true. Inserted between items — \"\" for letter-by-letter, \" \" for word-by-word.", GH_ParamAccess.item, "");
            pManager[5].Optional = true;
            pManager[6].Optional = true;
            pManager[7].Optional = true;
            pManager[8].Optional = true;
            pManager[9].Optional = true;
            pManager[10].Optional = true;
            pManager[0].Optional = true;
            pManager.AddTextParameter(BlockChaining.InputName, BlockChaining.InputNickname,
                BlockChaining.InputDescription, GH_ParamAccess.tree);
            pManager[14].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Request JSON", "RJ", "insertText + updateTextStyle batchUpdate requests.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GHDataHelpers.WarnIfIterated(this, DA);
            var texts = GHDataHelpers.FlattenText(DA, 0);
            bool bold = false, italic = false, underline = false, strikethrough = false;
            string segmentId = "";

            DA.GetData(1, ref bold);
            DA.GetData(2, ref italic);
            DA.GetData(3, ref underline);
            DA.GetData(4, ref strikethrough);
            var fontSizes = GHDataHelpers.FlattenNumber(DA, 5);
            var fontFamilies = GHDataHelpers.FlattenText(DA, 6);
            var textColors = GHDataHelpers.FlattenText(DA, 7);
            var highlights = GHDataHelpers.FlattenText(DA, 8);
            var links = GHDataHelpers.FlattenText(DA, 9);
            var baselines = GHDataHelpers.FlattenText(DA, 10);
            DA.GetData(11, ref segmentId);
            bool sameParagraph = false;
            string separator = "";
            DA.GetData(12, ref sameParagraph);
            DA.GetData(13, ref separator);
            var block = GHDataHelpers.FlattenText(DA, 14);

            // toggles apply only when connected, so a deliberate "false" can un-set a style
            bool? boldN = Params.Input[1].SourceCount > 0 ? bold : (bool?)null;
            bool? italicN = Params.Input[2].SourceCount > 0 ? italic : (bool?)null;
            bool? underlineN = Params.Input[3].SourceCount > 0 ? underline : (bool?)null;
            bool? strikeN = Params.Input[4].SourceCount > 0 ? strikethrough : (bool?)null;
            var fontSizesN = fontSizes.ConvertAll(f => f > 0 ? f : (double?)null);

            foreach (var c in textColors)
            {
                if (!string.IsNullOrWhiteSpace(c) && SharedBuilders.ParseHexRgb(c) == null)
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        $"Could not parse Text Color \"{c}\" — expected #RRGGBB hex or R,G,B (0-255). That color was skipped.");
            }
            foreach (var h in highlights)
            {
                if (!string.IsNullOrWhiteSpace(h) && SharedBuilders.ParseHexRgb(h) == null)
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        $"Could not parse Highlight \"{h}\" — expected #RRGGBB hex or R,G,B (0-255). That highlight was skipped.");
            }

            if (BlockChaining.HasBlock(block))
            {
                if (sameParagraph)
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                        "Same Paragraph has no effect while Request JSON is connected — the incoming block's paragraph structure is left as it is.");

                var chained = BlockChaining.ChainTextStyle(block, texts, segmentId, AddRuntimeMessage,
                    boldN, italicN, underlineN, strikeN,
                    fontSizesN, fontFamilies, textColors, highlights, links, baselines);
                if (chained != null) DA.SetDataList(0, chained);
                return;
            }

            if (texts.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No text supplied.");
                return;
            }

            var json = sameParagraph
                ? TextRequestBuilders.InsertStyledRun(
                    texts, separator, boldN, italicN, underlineN, strikeN,
                    fontSizesN, fontFamilies, textColors, highlights, links, baselines,
                    newParagraphAfter: true, segmentId: segmentId)
                : TextRequestBuilders.InsertStyledTextLines(
                    texts, boldN, italicN, underlineN, strikeN,
                    fontSizesN, fontFamilies, textColors, highlights, links, baselines, segmentId);

            DA.SetDataList(0, json);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_UpdateTextStyleJson;
        public override Guid ComponentGuid => new Guid("5a62a390-47af-4101-b192-58ad9d5dff69");
    }
}
