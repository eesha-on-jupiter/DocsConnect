using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Newtonsoft.Json.Linq;

namespace DocsConnect
{
    // Creates one or more new blank Google Docs and returns their new document IDs. Title is a list —
    // Status/Response/Document ID are index-matched to it; one failing create does not stop the rest.
    public class CreateDocumentComponent : ButtonComponent
    {
        public CreateDocumentComponent()
            : base("Create Document", "Create",
                "Creates one or more new blank Google Docs with the given titles. Status/Response/Document ID are index-matched to Title.",
                PluginUtilities.TabName, PluginUtilities.CategoryABDocument)
        { }

        public override string ButtonLabel => "Create";

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Token", "T", "Google OAuth 2.0 access token.", GH_ParamAccess.item, "");
            pManager.AddTextParameter("Title", "TI", "Title(s) for the new document(s).", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "S", "Request status, index-matched to Title.", GH_ParamAccess.list);
            pManager.AddTextParameter("Response", "R", "Raw response JSON, index-matched to Title.", GH_ParamAccess.list);
            pManager.AddTextParameter("Document ID", "DID", "ID of the new document, index-matched to Title.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!IsTriggered) return;

            string token = "";
            var titles = new List<string>();
            DA.GetData(0, ref token);
            DA.GetDataList(1, titles);

            if (titles.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Title supplied.");
                return;
            }

            var client = new DocsConnectClient(token);
            var statuses = new List<string>();
            var responses = new List<string>();
            var documentIds = new List<string>();

            foreach (var title in titles)
            {
                var result = client.CreateDocumentAsync(title).GetAwaiter().GetResult();

                if (result.Item1)
                {
                    string documentId = "";
                    try { documentId = JObject.Parse(result.Item2).Value<string>("documentId") ?? ""; }
                    catch { /* leave blank if not JSON */ }

                    statuses.Add("Created.");
                    responses.Add(result.Item2);
                    documentIds.Add(documentId);
                }
                else
                {
                    statuses.Add("Failed.");
                    responses.Add(result.Item3);
                    documentIds.Add("");
                }
            }

            DA.SetDataList(0, statuses);
            DA.SetDataList(1, responses);
            DA.SetDataList(2, documentIds);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_CreateDocument;
        public override Guid ComponentGuid => new Guid("fb361ef0-22ba-4f94-9850-57c006b5e1bb");
    }
}
