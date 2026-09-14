using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Newtonsoft.Json.Linq;

namespace DocsConnect
{
    // Retrieves one or more Google Docs by ID, or by name (resolved through the Drive API), and
    // returns their raw JSON and title. Document ID and Name are index-matched lists — for each
    // index, a non-blank Document ID is used directly, otherwise the matching Name is looked up.
    public class GetDocumentComponent : ButtonComponent
    {
        public GetDocumentComponent()
            : base("Get Document", "Get",
                "Retrieves one or more Google Docs by document ID, or finds them by name (via the Drive API), and returns the resolved Document ID, raw JSON and title. Document ID and Name are index-matched — a blank Document ID entry falls back to looking up the Name at that index. Status/Response/Title/Document ID are index-matched to the input lists.",
                PluginUtilities.TabName, PluginUtilities.CategoryABDocument)
        { }

        public override string ButtonLabel => "Get";

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Token", "T", "Google OAuth 2.0 access token. Name lookup needs a Drive-scoped token (drive.readonly / drive.metadata.readonly).", GH_ParamAccess.item, "");
            pManager.AddTextParameter("Document ID", "DID", "ID(s) of the document(s) to retrieve. Leave an entry blank to look up by the matching Name instead.", GH_ParamAccess.list);
            pManager.AddTextParameter("Name", "N", "Document name(s) to search for (substring match, via Drive), index-matched to Document ID. Used only where Document ID is blank.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "S", "Request status, index-matched to Document ID / Name.", GH_ParamAccess.list);
            pManager.AddTextParameter("Response", "R", "Raw document JSON, index-matched to Document ID / Name.", GH_ParamAccess.list);
            pManager.AddTextParameter("Title", "TI", "Document title, index-matched to Document ID / Name.", GH_ParamAccess.list);
            pManager.AddTextParameter("Document ID", "DID", "The document ID actually used (resolved from Name when looked up). Chains into Batch Update.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("End Index", "EI", "The index to append at — the end of the document body, index-matched to Document ID / Name. Wire it into Request Aggregator's Start Index to add content BELOW what is already in the document instead of at the top.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!IsTriggered) return;

            string token = "";
            var documentIdsIn = new List<string>();
            var namesIn = new List<string>();
            DA.GetData(0, ref token);
            DA.GetDataList(1, documentIdsIn);
            DA.GetDataList(2, namesIn);

            int n = Math.Max(documentIdsIn.Count, namesIn.Count);
            if (n == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Provide at least one Document ID or Name.");
                return;
            }

            var client = new DocsConnectClient(token);
            var statuses = new List<string>();
            var responses = new List<string>();
            var titles = new List<string>();
            var resolvedIds = new List<string>();
            var endIndices = new List<int>();

            for (int i = 0; i < n; i++)
            {
                string documentId = i < documentIdsIn.Count ? (documentIdsIn[i] ?? "") : "";
                string name = i < namesIn.Count ? (namesIn[i] ?? "") : "";

                // Resolve by name through Drive when no explicit Document ID was given.
                if (string.IsNullOrWhiteSpace(documentId))
                {
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        statuses.Add("Provide a Document ID or a Name.");
                        responses.Add(""); titles.Add(""); resolvedIds.Add(""); endIndices.Add(1);
                        continue;
                    }

                    var find = client.FindDocumentsByNameAsync(name).GetAwaiter().GetResult();
                    if (!find.Item1)
                    {
                        statuses.Add("Name lookup failed.");
                        responses.Add(find.Item3); titles.Add(""); resolvedIds.Add(""); endIndices.Add(1);
                        continue;
                    }

                    JArray files;
                    try { files = JObject.Parse(find.Item2).Value<JArray>("files") ?? new JArray(); }
                    catch { files = new JArray(); }

                    if (files.Count == 0)
                    {
                        statuses.Add("No document found matching \"" + name + "\".");
                        responses.Add(""); titles.Add(""); resolvedIds.Add(""); endIndices.Add(1);
                        continue;
                    }

                    if (files.Count > 1)
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                            files.Count + " documents match \"" + name + "\" — using the most recently modified. Refine the name or pass a Document ID for an exact match.");

                    documentId = files[0].Value<string>("id") ?? "";
                }

                if (string.IsNullOrWhiteSpace(documentId))
                {
                    statuses.Add("Could not resolve a Document ID.");
                    responses.Add(""); titles.Add(""); resolvedIds.Add(""); endIndices.Add(1);
                    continue;
                }

                var result = client.GetDocumentAsync(documentId).GetAwaiter().GetResult();

                if (result.Item1)
                {
                    string title = "";
                    try { title = JObject.Parse(result.Item2).Value<string>("title") ?? ""; }
                    catch { /* leave blank if not JSON */ }

                    statuses.Add("OK");
                    responses.Add(result.Item2);
                    titles.Add(title);
                    resolvedIds.Add(documentId);
                    endIndices.Add(DocumentBuilders.BodyEndIndex(result.Item2));
                }
                else
                {
                    statuses.Add("Failed.");
                    responses.Add(result.Item3);
                    titles.Add("");
                    resolvedIds.Add(documentId);
                    endIndices.Add(1);
                }
            }

            DA.SetDataList(0, statuses);
            DA.SetDataList(1, responses);
            DA.SetDataList(2, titles);
            DA.SetDataList(3, resolvedIds);
            DA.SetDataList(4, endIndices);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_GetDocument;
        public override Guid ComponentGuid => new Guid("6b83a59d-6767-4e7a-8e65-f78fd062233a");
    }
}
