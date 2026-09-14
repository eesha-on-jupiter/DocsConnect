using System;
using Grasshopper.Kernel;

namespace DocsConnect
{
    // Builds insertPageBreak requests — inserts one or more page breaks, self-indexed from a
    // placeholder position. No index needed — wire it into Request Aggregator to place it in the document.
    public class InsertPageBreakJsonComponent : GH_Component
    {
        public InsertPageBreakJsonComponent()
            : base("Insert Page Break", "PgBreak",
                "Builds one or more insertPageBreak requests. No index needed — wire it into Request Aggregator to place it in the document.",
                PluginUtilities.TabName, PluginUtilities.CategoryAERequestsInsert)
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Count", "N", "Number of page breaks to insert in sequence.", GH_ParamAccess.item, 1);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Request JSON", "RJ", "batchUpdate request JSON.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GHDataHelpers.WarnIfIterated(this, DA);
            int count = 1;
            DA.GetData(0, ref count);

            if (count <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Count must be at least 1.");
                return;
            }

            DA.SetDataList(0, InsertRequestBuilders.InsertPageBreaksSequential(count));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_InsertPageBreakJson;
        public override Guid ComponentGuid => new Guid("532c3d17-7617-4990-95c0-66ff7d577069");
    }
}
