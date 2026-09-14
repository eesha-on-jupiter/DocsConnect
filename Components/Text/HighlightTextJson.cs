using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace DocsConnect
{
    // Inserts one or more lines of text with a given highlight color — insertText + updateTextStyle
    // combo per line, no index needed. Text and Highlight are index-matched lists: a list of Text
    // produces one paragraph per item, and a shorter Highlight list repeats its last value.
    public class HighlightTextJsonComponent : GH_Component
    {
        public HighlightTextJsonComponent()
            : base("Highlight Text", "Highlight",
                "Inserts one or more lines of text with a given highlight color. Highlight is index-matched to Text (a shorter list repeats its last value). No index needed — wire it into Request Aggregator to place it in the document.",
                PluginUtilities.TabName, PluginUtilities.CategoryACRequestsText)
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "TX", "Text to insert. A list places each entry on its own line.", GH_ParamAccess.tree);
            pManager.AddTextParameter("Highlight", "HC", "Highlight color as #RRGGBB, index-matched to Text (a shorter list repeats its last value). All branches are flattened into one ordered list.", GH_ParamAccess.tree);
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
            var highlights = GHDataHelpers.FlattenText(DA, 1);
            string segmentId = "";
            DA.GetData(2, ref segmentId);
            var block = GHDataHelpers.FlattenText(DA, 3);

            if (highlights.Count == 0) highlights.Add("#FFFF00");

            foreach (var h in highlights)
            {
                if (!string.IsNullOrWhiteSpace(h) && SharedBuilders.ParseHexRgb(h) == null)
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        $"Could not parse highlight color \"{h}\" — expected #RRGGBB hex or R,G,B (0-255). That highlight was skipped.");
            }

            if (BlockChaining.HasBlock(block))
            {
                var chained = BlockChaining.ChainTextStyle(block, texts, segmentId, AddRuntimeMessage,
                    null, null, null, null, null, null, null, highlights, null, null);
                if (chained != null) DA.SetDataList(0, chained);
                return;
            }

            if (texts.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No text supplied.");
                return;
            }

            var json = TextRequestBuilders.InsertStyledTextLines(texts, null, null, null, null,
                null, null, null, highlights, null, null, segmentId);
            DA.SetDataList(0, json);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_HighlightTextJson;
        public override Guid ComponentGuid => new Guid("f7e52019-9ea3-4fab-98d5-a8c83f9c6295");
    }
}
