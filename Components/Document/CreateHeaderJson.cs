using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Newtonsoft.Json.Linq;

namespace DocsConnect
{
    // Creates a document header (type DEFAULT) in one or more documents, optionally setting its text
    // in the same step. Document ID and Text are index-matched lists (a shorter Text list repeats its
    // last value); Status/Response/Header ID are index-matched to Document ID.
    public class CreateHeaderJsonComponent : ButtonComponent
    {
        public CreateHeaderJsonComponent()
            : base("Create Header", "Header",
                "Creates a document header in one or more documents. If Text is supplied, also inserts it into each header in the same step; leave it blank to just create the empty header. Text is index-matched to Document ID (a shorter list repeats its last value). Header ID is always returned so richer content (bold, multiple lines, alignment) can be layered in afterward via any Text/Paragraph component's Segment ID input.",
                PluginUtilities.TabName, PluginUtilities.CategoryABDocument)
        { }

        public override string ButtonLabel => "Create";

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Token", "T", "Google OAuth 2.0 access token.", GH_ParamAccess.item, "");
            pManager.AddTextParameter("Document ID", "DID", "ID(s) of the document(s) to add a header to.", GH_ParamAccess.list);
            pManager.AddTextParameter("Text", "TX", "Text to set in the header (optional — leave blank to just create the empty header). Index-matched to Document ID (a shorter list repeats its last value).", GH_ParamAccess.list, "");
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "S", "Request status, index-matched to Document ID.", GH_ParamAccess.list);
            pManager.AddTextParameter("Response", "R", "Raw response JSON, index-matched to Document ID.", GH_ParamAccess.list);
            pManager.AddTextParameter("Header ID", "HID", "ID of the created header segment, index-matched to Document ID.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!IsTriggered) return;

            string token = "";
            var documentIds = new List<string>();
            var texts = new List<string>();
            DA.GetData(0, ref token);
            DA.GetDataList(1, documentIds);
            DA.GetDataList(2, texts);

            if (documentIds.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Document ID supplied.");
                return;
            }

            var client = new DocsConnectClient(token);
            var statuses = new List<string>();
            var responses = new List<string>();
            var headerIds = new List<string>();

            for (int i = 0; i < documentIds.Count; i++)
            {
                string documentId = documentIds[i];
                string text = TextRequestBuilders.GetOrLast(texts, i, "");

                if (string.IsNullOrWhiteSpace(documentId))
                {
                    statuses.Add("Document ID is empty.");
                    responses.Add(""); headerIds.Add("");
                    continue;
                }

                string createRequests = DocumentBuilders.AssembleRequestsArray(new[] { StructureRequestBuilders.CreateHeader("DEFAULT") });
                var createResult = client.BatchUpdateDocumentAsync(documentId, createRequests).GetAwaiter().GetResult();

                if (!createResult.Item1)
                {
                    statuses.Add("Failed to create header.");
                    responses.Add(createResult.Item3); headerIds.Add("");
                    continue;
                }

                string headerId = "";
                try { headerId = JObject.Parse(createResult.Item2)["replies"]?[0]?["createHeader"]?.Value<string>("headerId") ?? ""; }
                catch { /* leave blank if the response shape is unexpected */ }

                if (string.IsNullOrWhiteSpace(text))
                {
                    statuses.Add("Created.");
                    responses.Add(createResult.Item2); headerIds.Add(headerId);
                    continue;
                }

                string insertRequests = DocumentBuilders.AssembleRequestsArray(new[] { TextRequestBuilders.InsertText(text, DocumentBuilders.SegmentStartIndex, headerId) });
                var insertResult = client.BatchUpdateDocumentAsync(documentId, insertRequests).GetAwaiter().GetResult();

                statuses.Add(insertResult.Item1 ? "Created." : "Header created, but text insert failed.");
                responses.Add(insertResult.Item1 ? insertResult.Item2 : insertResult.Item3);
                headerIds.Add(headerId);
            }

            DA.SetDataList(0, statuses);
            DA.SetDataList(1, responses);
            DA.SetDataList(2, headerIds);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_CreateHeaderJson;
        public override Guid ComponentGuid => new Guid("d8ce9b6b-deea-4deb-a360-4611a0365b9e");
    }
}
