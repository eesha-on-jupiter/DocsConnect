using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;

namespace DocsConnect
{
    // Inserts one or more paragraphs with given space below — insertText + updateParagraphStyle
    // combo per paragraph, no index needed. Text and Space Below are index-matched lists.
    public class SpaceBelowTextJsonComponent : GH_Component
    {
        public SpaceBelowTextJsonComponent()
            : base("Space Below Text", "SpaceBlw",
                "Inserts one or more paragraphs with given space below. Space Below is index-matched to Text (a shorter list repeats its last value). No index needed — wire it into Request Aggregator to place it in the document.",
                PluginUtilities.TabName, PluginUtilities.CategoryADRequestsParagraph)
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "TX", "Text to insert as a paragraph. A list places each entry in its own paragraph.", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Space Below", "SB", "Space below paragraph in points. Index-matched to Text (a shorter list repeats its last value). All branches are flattened into one ordered list.", GH_ParamAccess.tree);
            pManager.AddTextParameter("Segment ID", "SEG", "Header/Footer/Footnote ID to target that segment instead of the body (blank = body).", GH_ParamAccess.item, "");
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager.AddTextParameter(BlockChaining.InputName, BlockChaining.InputNickname,
                BlockChaining.InputDescription, GH_ParamAccess.tree);
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Request JSON", "RJ", "insertText + updateParagraphStyle batchUpdate requests.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GHDataHelpers.WarnIfIterated(this, DA);
            var texts = GHDataHelpers.FlattenText(DA, 0);
            var spaceBelows = GHDataHelpers.FlattenNumber(DA, 1);
            string segmentId = "";
            DA.GetData(2, ref segmentId);
            var block = GHDataHelpers.FlattenText(DA, 3);

            var spaceBelowsN = spaceBelows.Select(v => v > 0 ? v : (double?)null).ToList();

            if (BlockChaining.HasBlock(block))
            {
                var chained = BlockChaining.ChainParagraphStyle(block, texts, segmentId, AddRuntimeMessage,
                    null, null, null, null, null, spaceBelowsN);
                if (chained != null) DA.SetDataList(0, chained);
                return;
            }

            if (texts.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No text supplied.");
                return;
            }

            var json = ParagraphRequestBuilders.InsertStyledParagraphLines(texts, null, null, null, null, null, spaceBelowsN, segmentId);
            DA.SetDataList(0, json);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_SpaceBelowTextJson;
        public override Guid ComponentGuid => new Guid("47454a44-d013-465f-ac3f-e65a02a86d44");
    }
}
