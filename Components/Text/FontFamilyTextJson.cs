using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace DocsConnect
{
    // Inserts one or more lines of text in a given font family — insertText + updateTextStyle combo
    // per line, no index needed. Text and Font Family are index-matched lists: a list of Text produces
    // one paragraph per item, and a shorter Font Family list repeats its last value.
    public class FontFamilyTextJsonComponent : GH_Component
    {
        public FontFamilyTextJsonComponent()
            : base("Font Family Text", "FontFam",
                "Inserts one or more lines of text in a given font family. Font Family is index-matched to Text (a shorter list repeats its last value). No index needed — wire it into Request Aggregator to place it in the document.",
                PluginUtilities.TabName, PluginUtilities.CategoryACRequestsText)
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "TX", "Text to insert. A list places each entry on its own line.", GH_ParamAccess.tree);
            pManager.AddTextParameter("Font Family", "FF", "Font family name, index-matched to Text (a shorter list repeats its last value). All branches are flattened into one ordered list.", GH_ParamAccess.tree);
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
            var fontFamilies = GHDataHelpers.FlattenText(DA, 1);
            string segmentId = "";
            DA.GetData(2, ref segmentId);
            var block = GHDataHelpers.FlattenText(DA, 3);

            if (fontFamilies.Count == 0) fontFamilies.Add("Arial");

            if (BlockChaining.HasBlock(block))
            {
                var chained = BlockChaining.ChainTextStyle(block, texts, segmentId, AddRuntimeMessage,
                    null, null, null, null, null, fontFamilies, null, null, null, null);
                if (chained != null) DA.SetDataList(0, chained);
                return;
            }

            if (texts.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No text supplied.");
                return;
            }

            var json = TextRequestBuilders.InsertStyledTextLines(texts, null, null, null, null,
                null, fontFamilies, null, null, null, null, segmentId);
            DA.SetDataList(0, json);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_FontFamilyTextJson;
        public override Guid ComponentGuid => new Guid("b9770efd-9c9c-4e14-8969-f2b372a11058");
    }
}
