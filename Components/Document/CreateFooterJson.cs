using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Newtonsoft.Json.Linq;

namespace DocsConnect
{
    // Creates a document footer (type DEFAULT) in one or more documents, optionally setting its text
    // in the same step. Document ID and Text are index-matched lists (a shorter Text list repeats its
    // last value); Status/Response/Footer ID are index-matched to Document ID.
    public class CreateFooterJsonComponent : ButtonComponent
    {
        public CreateFooterJsonComponent()
            : base("Create Footer", "Footer",
                "Creates a document footer in one or more documents. If Text is supplied, also inserts it into each footer in the same step; leave it blank to just create the empty footer. Text is index-matched to Document ID (a shorter list repeats its last value). Footer ID is always returned so richer content (bold, multiple lines, alignment) can be layered in afterward via any Text/Paragraph component's Segment ID input.",
                PluginUtilities.TabName, PluginUtilities.CategoryABDocument)
        { }

        public override string ButtonLabel => "Create";

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Token", "T", "Google OAuth 2.0 access token.", GH_ParamAccess.item, "");
            pManager.AddTextParameter("Document ID", "DID", "ID(s) of the document(s) to add a footer to.", GH_ParamAccess.list);
            pManager.AddTextParameter("Text", "TX", "Text to set in the footer (optional — leave blank to just create the empty footer). Index-matched to Document ID (a shorter list repeats its last value).", GH_ParamAccess.list, "");
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "S", "Request status, index-matched to Document ID.", GH_ParamAccess.list);
            pManager.AddTextParameter("Response", "R", "Raw response JSON, index-matched to Document ID.", GH_ParamAccess.list);
            pManager.AddTextParameter("Footer ID", "FID", "ID of the created footer segment, index-matched to Document ID.", GH_ParamAccess.list);
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
            var footerIds = new List<string>();

            for (int i = 0; i < documentIds.Count; i++)
            {
                string documentId = documentIds[i];
                string text = TextRequestBuilders.GetOrLast(texts, i, "");

                if (string.IsNullOrWhiteSpace(documentId))
                {
                    statuses.Add("Document ID is empty.");
                    responses.Add(""); footerIds.Add("");
                    continue;
                }

                string createRequests = DocumentBuilders.AssembleRequestsArray(new[] { StructureRequestBuilders.CreateFooter("DEFAULT") });
                var createResult = client.BatchUpdateDocumentAsync(documentId, createRequests).GetAwaiter().GetResult();

                if (!createResult.Item1)
                {
                    statuses.Add("Failed to create footer.");
                    responses.Add(createResult.Item3); footerIds.Add("");
                    continue;
                }

                string footerId = "";
                try { footerId = JObject.Parse(createResult.Item2)["replies"]?[0]?["createFooter"]?.Value<string>("footerId") ?? ""; }
                catch { /* leave blank if the response shape is unexpected */ }

                if (string.IsNullOrWhiteSpace(text))
                {
                    statuses.Add("Created.");
                    responses.Add(createResult.Item2); footerIds.Add(footerId);
                    continue;
                }

                string insertRequests = DocumentBuilders.AssembleRequestsArray(new[] { TextRequestBuilders.InsertText(text, DocumentBuilders.SegmentStartIndex, footerId) });
                var insertResult = client.BatchUpdateDocumentAsync(documentId, insertRequests).GetAwaiter().GetResult();

                statuses.Add(insertResult.Item1 ? "Created." : "Footer created, but text insert failed.");
                responses.Add(insertResult.Item1 ? insertResult.Item2 : insertResult.Item3);
                footerIds.Add(footerId);
            }

            DA.SetDataList(0, statuses);
            DA.SetDataList(1, responses);
            DA.SetDataList(2, footerIds);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_CreateFooterJson;
        public override Guid ComponentGuid => new Guid("0d466b85-3cdd-4b0e-be69-ea05f573c518");
    }
}
