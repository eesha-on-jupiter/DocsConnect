using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Newtonsoft.Json.Linq;

namespace DocsConnect
{
    // Creates one or more footnote references, appended at the end of the document's body — no index
    // needed. Since this is a live write against a real document (not a stackable content block routed
    // through Request Aggregator), "append" means reading the document first to find the current end of
    // its body (same content[].endIndex - 1 pattern as ClearDocument), then anchoring the footnote
    // marker there. Document ID and Text are index-matched lists (a shorter Text list repeats its last
    // value); Status/Response/Footnote ID are index-matched to Document ID.
    public class CreateFootnoteJsonComponent : ButtonComponent
    {
        public CreateFootnoteJsonComponent()
            : base("Create Footnote", "Footnote",
                "Creates one or more footnote references, appended at the end of each document's body. If Text is supplied, also inserts it into each footnote in the same step; leave it blank to just create the empty footnote. Text is index-matched to Document ID (a shorter list repeats its last value). Footnote ID is always returned so richer content can be layered in afterward via any Text/Paragraph component's Segment ID input.",
                PluginUtilities.TabName, PluginUtilities.CategoryABDocument)
        { }

        public override string ButtonLabel => "Create";

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Token", "T", "Google OAuth 2.0 access token.", GH_ParamAccess.item, "");
            pManager.AddTextParameter("Document ID", "DID", "ID(s) of the document(s) to add a footnote to.", GH_ParamAccess.list);
            pManager.AddTextParameter("Text", "TX", "Text to set in the footnote (optional — leave blank to just create the empty footnote). Index-matched to Document ID (a shorter list repeats its last value).", GH_ParamAccess.list, "");
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "S", "Request status, index-matched to Document ID.", GH_ParamAccess.list);
            pManager.AddTextParameter("Response", "R", "Raw response JSON, index-matched to Document ID.", GH_ParamAccess.list);
            pManager.AddTextParameter("Footnote ID", "FNID", "ID of the created footnote segment, index-matched to Document ID.", GH_ParamAccess.list);
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
            var footnoteIds = new List<string>();

            for (int i = 0; i < documentIds.Count; i++)
            {
                string documentId = documentIds[i];
                string text = TextRequestBuilders.GetOrLast(texts, i, "");

                if (string.IsNullOrWhiteSpace(documentId))
                {
                    statuses.Add("Document ID is empty.");
                    responses.Add(""); footnoteIds.Add("");
                    continue;
                }

                var doc = client.GetDocumentAsync(documentId).GetAwaiter().GetResult();
                if (!doc.Item1)
                {
                    statuses.Add("Could not read document.");
                    responses.Add(doc.Item3); footnoteIds.Add("");
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

                // the document's last character is an implicit trailing newline — anchor before it
                int index = System.Math.Max(1, bodyEnd - 1);

                string createRequests = DocumentBuilders.AssembleRequestsArray(new[] { StructureRequestBuilders.CreateFootnote(index) });
                var createResult = client.BatchUpdateDocumentAsync(documentId, createRequests).GetAwaiter().GetResult();

                if (!createResult.Item1)
                {
                    statuses.Add("Failed to create footnote.");
                    responses.Add(createResult.Item3); footnoteIds.Add("");
                    continue;
                }

                string footnoteId = "";
                try { footnoteId = JObject.Parse(createResult.Item2)["replies"]?[0]?["createFootnote"]?.Value<string>("footnoteId") ?? ""; }
                catch { /* leave blank if the response shape is unexpected */ }

                if (string.IsNullOrWhiteSpace(text))
                {
                    statuses.Add("Created.");
                    responses.Add(createResult.Item2); footnoteIds.Add(footnoteId);
                    continue;
                }

                string insertRequests = DocumentBuilders.AssembleRequestsArray(new[] { TextRequestBuilders.InsertText(text, DocumentBuilders.FootnoteStartIndex, footnoteId) });
                var insertResult = client.BatchUpdateDocumentAsync(documentId, insertRequests).GetAwaiter().GetResult();

                statuses.Add(insertResult.Item1 ? "Created." : "Footnote created, but text insert failed.");
                responses.Add(insertResult.Item1 ? insertResult.Item2 : insertResult.Item3);
                footnoteIds.Add(footnoteId);
            }

            DA.SetDataList(0, statuses);
            DA.SetDataList(1, responses);
            DA.SetDataList(2, footnoteIds);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_CreateFootnoteJson;
        public override Guid ComponentGuid => new Guid("457d3e0c-e0f1-42eb-96df-3b6a00a9492d");
    }
}
