using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace DocsConnect
{
    // Resolves an image to the public URL that Insert Image / Replace Image need — the Docs API's
    // insertInlineImage fetches its "uri" unauthenticated, so a local Bitmap or File Path (e.g. from
    // Capture Rhino View) has no way into a document without first being hosted somewhere public.
    // Priority: URL passthrough (no upload) > Bitmap > File Path. Bitmap/File Path are uploaded to
    // Google Drive and made link-public via DocsConnectClient.UploadImageAndGetUrlAsync.
    public class GetImageUrlComponent : ButtonComponent
    {
        public GetImageUrlComponent()
            : base("Get Image URL", "ImgURL",
                "Resolves an image to a public URL usable by Insert Image / Replace Image. Priority: URL (passthrough) > Bitmap > File Path — Bitmap/File Path are uploaded to Google Drive and made link-public, since the Docs API can only fetch a public URL.",
                PluginUtilities.TabName, PluginUtilities.CategoryAERequestsInsert)
        { }

        public override string ButtonLabel => "Get URL";

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Token", "T", "Google OAuth 2.0 access token with the drive.file scope (needed only when uploading Bitmaps or File Paths; not used for URL passthrough).", GH_ParamAccess.item, "");
            pManager.AddGenericParameter("Bitmap", "BM", "In-memory image(s) to host (e.g. from another plugin). A list uploads each.", GH_ParamAccess.list);
            pManager.AddTextParameter("File Path", "FP", "Local path(s) of images to host (e.g. from Capture Rhino View). A list uploads each.", GH_ParamAccess.list);
            pManager.AddTextParameter("URL", "U", "Already-public image URL(s) — passed through unchanged with no upload.", GH_ParamAccess.list);
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Image URL", "IU", "Publicly accessible URL of each image, in input order (URLs, then Bitmaps, then File Paths) — wire into Insert Image / Replace Image's Image URL input.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!IsTriggered) return;

            string token = "";
            var bitmapObjs = new List<object>();
            var filePaths = new List<string>();
            var urls = new List<string>();
            DA.GetData(0, ref token);
            DA.GetDataList(1, bitmapObjs);
            DA.GetDataList(2, filePaths);
            DA.GetDataList(3, urls);

            // everything to upload: (bytes, file name), in input order after the passthrough URLs
            var uploads = new List<Tuple<byte[], string>>();
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            int n = 0;
            foreach (var obj in bitmapObjs)
            {
                var bitmap = obj as Bitmap ?? (obj as GH_ObjectWrapper)?.Value as Bitmap;
                if (bitmap == null) continue;
                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    uploads.Add(Tuple.Create(ms.ToArray(), $"capture_{stamp}_{n++:00}.png"));
                }
            }
            foreach (var filePath in filePaths)
            {
                if (string.IsNullOrWhiteSpace(filePath)) continue;
                if (!File.Exists(filePath))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"File not found: {filePath}");
                    continue;
                }
                try { uploads.Add(Tuple.Create(File.ReadAllBytes(filePath), Path.GetFileName(filePath))); }
                catch (Exception ex)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Could not read {filePath}: {ex.Message}");
                }
            }

            var results = new List<string>();
            foreach (var url in urls)
                if (!string.IsNullOrWhiteSpace(url)) results.Add(url);

            if (uploads.Count == 0 && results.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Supply a Bitmap, File Path, or URL.");
                return;
            }

            if (uploads.Count > 0)
            {
                var client = new DocsConnectClient(token);
                foreach (var upload in uploads)
                {
                    var result = client.UploadImageAndGetUrlAsync(upload.Item1, upload.Item2, MimeTypeFor(upload.Item2)).GetAwaiter().GetResult();
                    if (result.Item1) results.Add(result.Item2);
                    else AddRuntimeMessage(GH_RuntimeMessageLevel.Error, upload.Item2 + ": " + result.Item3);
                }
            }

            Message = results.Count + (results.Count == 1 ? " image" : " images");
            DA.SetDataList(0, results);
        }

        private static string MimeTypeFor(string fileName)
        {
            switch (Path.GetExtension(fileName).ToLowerInvariant())
            {
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".gif": return "image/gif";
                default: return "image/png";
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_GetImageUrl;
        public override Guid ComponentGuid => new Guid("953864c3-338a-4b86-8eea-792d5d57c877");
    }
}
