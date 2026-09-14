using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace DocsConnect
{
    // Builds an updateDocumentStyle request — margins, page size and background.
    public class UpdateDocumentStyleJsonComponent : GH_Component
    {
        public UpdateDocumentStyleJsonComponent()
            : base("Update Document Style", "DocStyle",
                "Builds an updateDocumentStyle request; only supplied properties are applied.",
                PluginUtilities.TabName, PluginUtilities.CategoryABDocument)
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Margin Top", "MT", "Top margin in points (>0 to apply).", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("Margin Bottom", "MB", "Bottom margin in points (>0 to apply).", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("Margin Left", "ML", "Left margin in points (>0 to apply).", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("Margin Right", "MR", "Right margin in points (>0 to apply).", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("Page Width", "PW", "Page width in points (>0 to apply).", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("Page Height", "PH", "Page height in points (>0 to apply).", GH_ParamAccess.item, 0.0);
            pManager.AddTextParameter("Background", "BG", "Background color as #RRGGBB (blank to skip).", GH_ParamAccess.item, "");
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Request JSON", "RJ", "batchUpdate request JSON.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GHDataHelpers.WarnIfIterated(this, DA);
            double mt = 0, mb = 0, ml = 0, mr = 0, pw = 0, ph = 0;
            string background = "";
            DA.GetData(0, ref mt);
            DA.GetData(1, ref mb);
            DA.GetData(2, ref ml);
            DA.GetData(3, ref mr);
            DA.GetData(4, ref pw);
            DA.GetData(5, ref ph);
            DA.GetData(6, ref background);

            if (!string.IsNullOrWhiteSpace(background) && SharedBuilders.ParseHexRgb(background) == null)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"Could not parse Background \"{background}\" — expected #RRGGBB hex or R,G,B (0-255). It was skipped.");

            string json = StructureRequestBuilders.UpdateDocumentStyle(
                mt > 0 ? mt : (double?)null,
                mb > 0 ? mb : (double?)null,
                ml > 0 ? ml : (double?)null,
                mr > 0 ? mr : (double?)null,
                pw > 0 ? pw : (double?)null,
                ph > 0 ? ph : (double?)null,
                background);

            DA.SetDataList(0, new List<string> { json });
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_UpdateDocumentStyleJson;
        public override Guid ComponentGuid => new Guid("dd536a23-6c78-4824-8e6b-4571196630da");
    }
}
