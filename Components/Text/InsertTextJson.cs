using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace DocsConnect
{
    // Builds insertText requests — no index needed; wire them into Request Aggregator to place them. A
    // list of Text produces one paragraph per item.
    public class InsertTextJsonComponent : GH_Component
    {
        public InsertTextJsonComponent()
            : base("Insert Text", "InsTxt",
                "Builds an insertText request for one or more lines of text. A list of Text produces one paragraph per item. No index needed — wire it (with your other blocks, in the order they should appear) into Request Aggregator to compute real document positions.",
                PluginUtilities.TabName, PluginUtilities.CategoryACRequestsText)
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "TX", "Text to insert. A list places each entry on its own line.", GH_ParamAccess.tree);
            pManager.AddTextParameter("Segment ID", "SEG", "Header/Footer/Footnote ID to target that segment instead of the body (blank = body).", GH_ParamAccess.item, "");
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Request JSON", "RJ", "batchUpdate request JSON.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GHDataHelpers.WarnIfIterated(this, DA);
            var texts = GHDataHelpers.FlattenText(DA, 0);
            string segmentId = "";
            DA.GetData(1, ref segmentId);

            if (texts.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No text supplied.");
                return;
            }

            DA.SetDataList(0, TextRequestBuilders.InsertTextLines(texts, segmentId));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_InsertTextJson;
        public override Guid ComponentGuid => new Guid("bd60e3f3-8628-4df3-a53e-487ebdef12b3");
    }
}
