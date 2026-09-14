using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace DocsConnect
{
    // Captures the active (or named) Rhino viewport to a PNG file on disk. Does not upload or host
    // the image anywhere — the output File Path is meant to be wired into whatever you use to get
    // the image somewhere Google Docs can fetch it from (e.g. upload it to Drive yourself, then pass
    // the resulting URL into Insert Image). Button-triggered like the other Rhino/network-facing
    // components, since capturing the viewport is a "do it now" action, not a pure data transform.
    public class CaptureRhinoViewComponent : ButtonComponent
    {
        public CaptureRhinoViewComponent()
            : base("Capture Rhino View", "Capture",
                "Captures the active (or named) Rhino viewport to a PNG file and outputs its file path. Does not upload anywhere — host the file yourself and feed the resulting URL into Insert Image.",
                PluginUtilities.TabName, PluginUtilities.CategoryAERequestsInsert)
        { }

        public override string ButtonLabel => "Capture";

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("View Name", "VN", "Named Rhino viewport(s) to capture — a list captures each in turn (blank = the currently active viewport).", GH_ParamAccess.list);
            pManager.AddTextParameter("File Path", "FP", "Where to save the PNGs: a folder (each capture is auto-named after its viewport inside it), or for a single capture a full file path. Blank = a temp folder.", GH_ParamAccess.item, "");
            pManager.AddIntegerParameter("Width", "W", "Capture width in pixels (0 = current viewport size).", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("Height", "H", "Capture height in pixels (0 = current viewport size).", GH_ParamAccess.item, 0);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("File Path", "FP", "Path of each saved PNG, index-matched to View Name.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!IsTriggered) return;

            var viewNames = new List<string>();
            string filePath = "";
            int width = 0, height = 0;
            DA.GetDataList(0, viewNames);
            DA.GetData(1, ref filePath);
            DA.GetData(2, ref width);
            DA.GetData(3, ref height);
            if (viewNames.Count == 0) viewNames.Add("");

            var doc = Rhino.RhinoDoc.ActiveDoc;
            if (doc == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No active Rhino document.");
                return;
            }

            // Where the files go. Several captures need a folder; a full file path only makes
            // sense for exactly one capture.
            bool explicitFile = !string.IsNullOrWhiteSpace(filePath) && !System.IO.Directory.Exists(filePath)
                && !string.IsNullOrEmpty(System.IO.Path.GetExtension(filePath));
            string folder;
            if (string.IsNullOrWhiteSpace(filePath))
                folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DocsConnect");
            else if (explicitFile)
                folder = System.IO.Path.GetDirectoryName(filePath);
            else
                folder = filePath;
            if (explicitFile && viewNames.Count > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    "File Path names one file but " + viewNames.Count + " views were given — saving auto-named files in its folder instead.");
                explicitFile = false;
            }
            try { if (!string.IsNullOrWhiteSpace(folder)) System.IO.Directory.CreateDirectory(folder); }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Could not create folder: {ex.Message}");
                return;
            }

            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var saved = new List<string>();
            for (int i = 0; i < viewNames.Count; i++)
            {
                string viewName = viewNames[i] ?? "";
                var view = string.IsNullOrWhiteSpace(viewName) ? doc.Views.ActiveView : doc.Views.Find(viewName, false);
                if (view == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, string.IsNullOrWhiteSpace(viewName)
                        ? "No active Rhino viewport found."
                        : $"No viewport named \"{viewName}\" found.");
                    continue;
                }

                var bitmap = (width > 0 && height > 0)
                    ? view.CaptureToBitmap(new System.Drawing.Size(width, height))
                    : view.CaptureToBitmap();

                if (bitmap == null || bitmap.Width <= 0 || bitmap.Height <= 0)
                {
                    bitmap?.Dispose();
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Capture of \"{view.ActiveViewport.Name}\" failed (empty or zero-size result — is the viewport minimized?).");
                    continue;
                }

                // one distinct file per capture: viewport name + timestamp + position, so a list
                // captured in the same second never overwrites itself
                string safeName = string.Join("_", view.ActiveViewport.Name.Split(System.IO.Path.GetInvalidFileNameChars()));
                string target = explicitFile ? filePath : System.IO.Path.Combine(folder, $"{safeName}_{stamp}_{i:00}.png");

                try
                {
                    // view.CaptureToBitmap() can hand back a bitmap whose pixel format / underlying GDI
                    // handle the PNG encoder chokes on when writing straight to disk ("A generic error
                    // occurred in GDI+."). Re-drawing onto a clean 32bppArgb bitmap and encoding to a
                    // MemoryStream first, then writing the bytes, sidesteps both the format and the
                    // direct-to-file-stream issues.
                    using (var clean = new System.Drawing.Bitmap(bitmap.Width, bitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    {
                        using (var g = System.Drawing.Graphics.FromImage(clean))
                            g.DrawImageUnscaled(bitmap, 0, 0);

                        using (var ms = new System.IO.MemoryStream())
                        {
                            clean.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            System.IO.File.WriteAllBytes(target, ms.ToArray());
                        }
                    }
                    saved.Add(target);
                }
                catch (Exception ex)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Could not save image: {ex.Message}");
                }
                finally
                {
                    bitmap.Dispose();
                }
            }

            Message = saved.Count + (saved.Count == 1 ? " capture" : " captures");
            DA.SetDataList(0, saved);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_CaptureRhinoView;
        public override Guid ComponentGuid => new Guid("6f2a3b1c-9d4e-4f5a-8c6b-1e7d2a9f4c3b");
    }
}
