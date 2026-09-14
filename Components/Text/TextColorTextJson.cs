using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace DocsConnect
{
    // Inserts one or more lines of text, each in its own foreground color — insertText + updateTextStyle
    // pairs, no index needed. Text and Color are index-matched lists: a shorter Color list repeats its
    // last color for the remaining lines. Multiple lines land as separate paragraphs.
    public class TextColorTextJsonComponent : GH_Component
    {
        public TextColorTextJsonComponent()
            : base("Text Color", "TxtColor",
                "Inserts one or more lines of text, each in a given foreground color. A list of Text produces one paragraph per item; Color is index-matched to Text (a shorter list repeats its last color). No index needed — wire it into Request Aggregator to place it in the document.",
                PluginUtilities.TabName, PluginUtilities.CategoryACRequestsText)
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "TX", "Text to insert. A list places each entry on its own line.", GH_ParamAccess.tree);
            pManager.AddTextParameter("Color", "FC", "Foreground color as #RRGGBB, index-matched to Text (a shorter list repeats its last color). All branches are flattened into one ordered list.", GH_ParamAccess.tree);
            pManager.AddTextParameter("Segment ID", "SEG", "Header/Footer/Footnote ID to target that segment instead of the body (blank = body).", GH_ParamAccess.item, "");
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager.AddTextParameter(BlockChaining.InputName, BlockChaining.InputNickname,
                BlockChaining.InputDescription, GH_ParamAccess.tree);
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Request JSON", "RJ", "insertText + updateTextStyle batchUpdate requests.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GHDataHelpers.WarnIfIterated(this, DA);
            var texts = GHDataHelpers.FlattenText(DA, 0);
            var colors = GHDataHelpers.FlattenText(DA, 1);
            string segmentId = "";
            DA.GetData(2, ref segmentId);
            var block = GHDataHelpers.FlattenText(DA, 3);

            if (colors.Count == 0) colors.Add("#000000");

            foreach (var c in colors)
            {
                if (!string.IsNullOrWhiteSpace(c) && SharedBuilders.ParseHexRgb(c) == null)
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        $"Could not parse color \"{c}\" — expected #RRGGBB hex or R,G,B (0-255). That color was skipped.");
            }

            if (BlockChaining.HasBlock(block))
            {
                var chained = BlockChaining.ChainTextStyle(block, texts, segmentId, AddRuntimeMessage,
                    null, null, null, null, null, null, colors, null, null, null);
                if (chained != null) DA.SetDataList(0, chained);
                return;
            }

            if (texts.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No text supplied.");
                return;
            }

            var json = TextRequestBuilders.InsertStyledTextLines(texts, null, null, null, null,
                null, null, colors, null, null, null, segmentId);
            DA.SetDataList(0, json);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_TextColorTextJson;
        public override Guid ComponentGuid => new Guid("b258a591-204a-4a3a-b619-dae902804fb2");
    }
}
