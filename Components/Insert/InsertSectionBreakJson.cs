using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace DocsConnect
{
    // Builds insertSectionBreak requests — inserts one or more section breaks, self-indexed from a
    // placeholder position. No index needed — stack through Request Aggregator to place it in the
    // document. One break per Section Type entry.
    public class InsertSectionBreakJsonComponent : GH_Component
    {
        public InsertSectionBreakJsonComponent()
            : base("Insert Section Break", "SecBreak",
                "Builds one or more insertSectionBreak requests — one break per Section Type entry. No index needed — wire it into Request Aggregator to place it in the document.",
                PluginUtilities.TabName, PluginUtilities.CategoryAERequestsInsert)
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Section Type", "SE", "Section type per break: NEXT_PAGE or CONTINUOUS. A list inserts one break per entry.", GH_ParamAccess.list, "NEXT_PAGE");
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Request JSON", "RJ", "batchUpdate request JSON.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GHDataHelpers.WarnIfIterated(this, DA);
            var sectionTypes = new List<string>();
            DA.GetDataList(0, sectionTypes);

            if (sectionTypes.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Section Type supplied.");
                return;
            }

            DA.SetDataList(0, InsertRequestBuilders.InsertSectionBreaksSequential(sectionTypes));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_InsertSectionBreakJson;
        public override Guid ComponentGuid => new Guid("f5f27726-7ac7-4ae3-bcf0-a1ffc142d43d");
    }
}
