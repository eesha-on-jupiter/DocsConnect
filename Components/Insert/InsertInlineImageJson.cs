using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;

namespace DocsConnect
{
    // Builds insertInlineImage requests — inserts one or more images, self-indexed from a placeholder
    // position. No index needed — wire it into Request Aggregator to place it in the document. Width and
    // Height are index-matched to Image URL (a shorter list repeats its last value).
    public class InsertInlineImageJsonComponent : GH_Component
    {
        public InsertInlineImageJsonComponent()
            : base("Insert Image", "InsImg",
                "Builds one or more insertInlineImage requests from public URLs — one image per Image URL entry. No index needed — wire it into Request Aggregator to place it in the document. Width and Height are index-matched to Image URL (a shorter list repeats its last value).",
                PluginUtilities.TabName, PluginUtilities.CategoryAERequestsInsert)
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Image URL", "IU", "Public URL of the image. A list inserts one image per entry.", GH_ParamAccess.list, "");
            pManager.AddNumberParameter("Width", "W", "Width in points (>0 to set), index-matched to Image URL (a shorter list repeats its last value).", GH_ParamAccess.list, 0.0);
            pManager.AddNumberParameter("Height", "H", "Height in points (>0 to set), index-matched to Image URL (a shorter list repeats its last value).", GH_ParamAccess.list, 0.0);
            pManager.AddBooleanParameter("Own Paragraph", "OP", "Put each image on its own line (a paragraph break after each). False = images sit side by side in one paragraph.", GH_ParamAccess.item, true);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Request JSON", "RJ", "batchUpdate request JSON.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GHDataHelpers.WarnIfIterated(this, DA);
            var urls = new List<string>();
            var widths = new List<double>();
            var heights = new List<double>();
            DA.GetDataList(0, urls);
            DA.GetDataList(1, widths);
            DA.GetDataList(2, heights);
            bool ownParagraph = true;
            DA.GetData(3, ref ownParagraph);

            if (urls.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Image URL supplied.");
                return;
            }

            var widthsN = widths.Select(w => w > 0 ? w : (double?)null).ToList();
            var heightsN = heights.Select(h => h > 0 ? h : (double?)null).ToList();
            DA.SetDataList(0, InsertRequestBuilders.InsertInlineImagesSequential(urls, widthsN, heightsN, ownParagraph));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_InsertInlineImageJson;
        public override Guid ComponentGuid => new Guid("397bc631-baf0-4b6b-b04e-c3207eb035ac");
    }
}
