using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace DocsConnect
{
    // Inserts one or more lines of text and makes them italic — insertText + updateTextStyle combo per
    // line, no index needed. A list of Text produces one paragraph per item.
    public class ItalicTextJsonComponent : GH_Component
    {
        public ItalicTextJsonComponent()
            : base("Italic Text", "Italic",
                "Inserts one or more lines of text and makes them italic. A list of Text produces one paragraph per item. No index needed — wire it into Request Aggregator to place it in the document.",
                PluginUtilities.TabName, PluginUtilities.CategoryACRequestsText)
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "TX", "Text to insert. A list places each entry on its own line.", GH_ParamAccess.tree);
            pManager.AddTextParameter("Segment ID", "SEG", "Header/Footer/Footnote ID to target that segment instead of the body (blank = body).", GH_ParamAccess.item, "");
            pManager[0].Optional = true;
            pManager.AddTextParameter(BlockChaining.InputName, BlockChaining.InputNickname,
                BlockChaining.InputDescription, GH_ParamAccess.tree);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Request JSON", "RJ", "insertText + updateTextStyle batchUpdate requests.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GHDataHelpers.WarnIfIterated(this, DA);
            var texts = GHDataHelpers.FlattenText(DA, 0);
            string segmentId = "";
            DA.GetData(1, ref segmentId);
            var block = GHDataHelpers.FlattenText(DA, 2);

            if (BlockChaining.HasBlock(block))
            {
                var chained = BlockChaining.ChainTextStyle(block, texts, segmentId, AddRuntimeMessage,
                    null, true, null, null, null, null, null, null, null, null);
                if (chained != null) DA.SetDataList(0, chained);
                return;
            }

            if (texts.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No text supplied.");
                return;
            }

            var json = TextRequestBuilders.InsertStyledTextLines(texts, null, true, null, null,
                null, null, null, null, null, null, segmentId);
            DA.SetDataList(0, json);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_ItalicTextJson;
        public override Guid ComponentGuid => new Guid("98b2a845-9a79-4464-b1e1-82eee3e0ccb6");
    }
}
