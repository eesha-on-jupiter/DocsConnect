using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace DocsConnect
{
    // Builds a titled, styled report scaffold from parallel heading and body lists.
    public class ReportSkeletonComponent : GH_Component
    {
        public ReportSkeletonComponent()
            : base("Report Skeleton", "Report",
                "Builds a titled report scaffold: title plus per-section styled heading and body paragraphs. No index needed — wire it into Request Aggregator to place it in the document.",
                PluginUtilities.TabName, PluginUtilities.CategoryADRequestsParagraph)
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Title", "TI", "Document title (TITLE style).", GH_ParamAccess.item, "");
            pManager.AddTextParameter("Headings", "HD", "Section heading texts.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Heading Levels", "HL", "Heading level per section (1-6).", GH_ParamAccess.list, 1);
            pManager.AddTextParameter("Bodies", "BD", "Body text per section.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Request JSON", "RJ", "batchUpdate requests array.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GHDataHelpers.WarnIfIterated(this, DA);
            string title = "";
            var headings = new List<string>();
            var levels = new List<int>();
            var bodies = new List<string>();

            DA.GetData(0, ref title);
            DA.GetDataList(1, headings);
            DA.GetDataList(2, levels);
            DA.GetDataList(3, bodies);

            DA.SetDataList(0, new List<string> { AecHelperBuilders.ReportSkeleton(title, headings, levels, bodies) });
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_ReportSkeleton;
        public override Guid ComponentGuid => new Guid("ca5fbc5e-c2de-4e4f-a00d-6bd588c73954");
    }
}
