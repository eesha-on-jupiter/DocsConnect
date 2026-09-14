using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace DocsConnect
{
    // Builds replaceImage requests — swaps one or more existing images for new ones. Image Object ID
    // and Image URL are index-matched lists: a shorter Image URL list repeats its last value.
    public class ReplaceImageJsonComponent : GH_Component
    {
        public ReplaceImageJsonComponent()
            : base("Replace Image", "RepImg",
                "Builds a replaceImage request per Image Object ID that swaps an existing image for a new URL. Image URL is index-matched to Image Object ID (a shorter list repeats its last value).",
                PluginUtilities.TabName, PluginUtilities.CategoryAERequestsInsert)
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Image Object ID", "IO", "Object ID(s) of the image(s) to replace.", GH_ParamAccess.list);
            pManager.AddTextParameter("Image URL", "IU", "Public URL of the new image, index-matched to Image Object ID (a shorter list repeats its last value).", GH_ParamAccess.list, "");
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Request JSON", "RJ", "batchUpdate request JSON.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GHDataHelpers.WarnIfIterated(this, DA);
            var objectIds = new List<string>();
            var urls = new List<string>();
            DA.GetDataList(0, objectIds);
            DA.GetDataList(1, urls);

            if (objectIds.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Image Object ID supplied.");
                return;
            }

            DA.SetDataList(0, InsertRequestBuilders.ReplaceImages(objectIds, urls));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_ReplaceImageJson;
        public override Guid ComponentGuid => new Guid("d71ce5cc-20dc-435d-9db4-bf98c144325d");
    }
}
