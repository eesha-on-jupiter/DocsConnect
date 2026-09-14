using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Newtonsoft.Json.Linq;

namespace DocsConnect
{
    // Deletes all content from one or more documents. Full clear only — no partial range. Document ID
    // is a list — Status/Response are index-matched to it; one failing document does not stop the rest.
    public class ClearDocumentComponent : ButtonComponent
    {
        public ClearDocumentComponent()
            : base("Clear Document", "Clear",
                "Deletes all content from one or more documents. Full clear only — press to wipe, no partial range. Status/Response are index-matched to Document ID.",
                PluginUtilities.TabName, PluginUtilities.CategoryABDocument)
        { }

        public override string ButtonLabel => "Clear";

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Token", "T", "Google OAuth 2.0 access token.", GH_ParamAccess.item, "");
            pManager.AddTextParameter("Document ID", "DID", "ID(s) of the document(s) to clear.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "S", "Request status, index-matched to Document ID.", GH_ParamAccess.list);
            pManager.AddTextParameter("Response", "R", "Raw response JSON, index-matched to Document ID.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!IsTriggered) return;

            string token = "";
            var documentIds = new List<string>();
            DA.GetData(0, ref token);
            DA.GetDataList(1, documentIds);

            if (documentIds.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Document ID supplied.");
                return;
            }

            var client = new DocsConnectClient(token);
            var statuses = new List<string>();
            var responses = new List<string>();

            foreach (var documentId in documentIds)
            {
                if (string.IsNullOrWhiteSpace(documentId))
                {
                    statuses.Add("Document ID is empty.");
                    responses.Add("");
                    continue;
                }

                var doc = client.GetDocumentAsync(documentId).GetAwaiter().GetResult();
                if (!doc.Item1)
                {
                    statuses.Add("Could not read document.");
                    responses.Add(doc.Item3);
                    continue;
                }

                int bodyEnd = 1;
                try
                {
                    var content = JObject.Parse(doc.Item2)["body"]?["content"] as JArray;
                    if (content != null && content.Count > 0)
                        bodyEnd = content[content.Count - 1].Value<int>("endIndex");
                }
                catch { /* leave bodyEnd at 1 if the shape is unexpected */ }

                // the document's last character is an implicit trailing newline — it can't be deleted
                int deleteEnd = bodyEnd - 1;

                if (deleteEnd <= 1)
                {
                    statuses.Add("Already empty.");
                    responses.Add(doc.Item2);
                    continue;
                }

                string requestsArray = DocumentBuilders.AssembleRequestsArray(new[] { TextRequestBuilders.DeleteContentRange(1, deleteEnd) });
                var result = client.BatchUpdateDocumentAsync(documentId, requestsArray).GetAwaiter().GetResult();

                if (result.Item1)
                {
                    statuses.Add("Cleared.");
                    responses.Add(result.Item2);
                }
                else
                {
                    statuses.Add("Failed.");
                    responses.Add(result.Item3);
                }
            }

            DA.SetDataList(0, statuses);
            DA.SetDataList(1, responses);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_ClearDocument;
        public override Guid ComponentGuid => new Guid("56b71016-7635-4e58-a279-69bd9264a03c");
    }
}
