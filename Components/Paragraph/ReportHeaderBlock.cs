using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace DocsConnect
{
    // Builds a standard project title block: name plus project number, date, author and revision.
    public class ReportHeaderBlockComponent : GH_Component
    {
        public ReportHeaderBlockComponent()
            : base("Report Header Block", "HdrBlock",
                "Builds a project title block: name (TITLE) plus project number, date, author and revision. No index needed — wire it into Request Aggregator to place it in the document. Unrelated to page header/footer segments despite the similar name — this builds a title block inside the main body.",
                PluginUtilities.TabName, PluginUtilities.CategoryADRequestsParagraph)
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Project Name", "PN", "Project name (TITLE style).", GH_ParamAccess.item, "");
            pManager.AddTextParameter("Project Number", "PO", "Project number (blank to skip).", GH_ParamAccess.item, "");
            pManager.AddTextParameter("Date", "DT", "Date — defaults to today if blank.", GH_ParamAccess.item, "");
            pManager.AddTextParameter("Author", "AU", "Author (blank to skip).", GH_ParamAccess.item, "");
            pManager.AddTextParameter("Revision", "RV", "Revision (blank to skip).", GH_ParamAccess.item, "");
            pManager.AddTextParameter("Layout", "LY", "Layout: lines (v1).", GH_ParamAccess.item, "lines");
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Request JSON", "RJ", "batchUpdate requests array.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GHDataHelpers.WarnIfIterated(this, DA);
            string projectName = "", projectNumber = "", date = "", author = "", revision = "", layout = "lines";

            DA.GetData(0, ref projectName);
            DA.GetData(1, ref projectNumber);
            DA.GetData(2, ref date);
            DA.GetData(3, ref author);
            DA.GetData(4, ref revision);
            DA.GetData(5, ref layout);

            if (string.IsNullOrWhiteSpace(date))
                date = DateTime.Today.ToString("yyyy-MM-dd");

            DA.SetDataList(0, new List<string> { AecHelperBuilders.ReportHeaderBlock(
                projectName, projectNumber, date, author, revision, layout) });
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_ReportHeaderBlock;
        public override Guid ComponentGuid => new Guid("1b094d2e-2e88-4fb8-ae97-34560b53df0a");
    }
}
