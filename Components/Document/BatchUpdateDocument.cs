using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace DocsConnect
{
    // Applies a list of batchUpdate requests to one or more existing Google Docs. Document ID is a
    // list — the same Requests JSON is applied to every document; Status/Response are index-matched
    // to Document ID, and one failing document does not stop the rest.
    public class BatchUpdateDocumentComponent : ButtonComponent
    {
        public BatchUpdateDocumentComponent()
            : base("Batch Update", "Apply",
                "Applies a list of batchUpdate requests to one or more existing Google Docs. The same Requests JSON is applied to every Document ID; Status/Response are index-matched to Document ID.",
                PluginUtilities.TabName, PluginUtilities.CategoryABDocument)
        { }

        public override string ButtonLabel => "Apply";

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Token", "T", "Google OAuth 2.0 access token.", GH_ParamAccess.item, "");
            pManager.AddTextParameter("Document ID", "DID", "ID(s) of the document(s) to update.", GH_ParamAccess.list);
            pManager.AddTextParameter("Requests JSON", "RQ", "Request JSON to apply to every document (from the aggregator or request components).", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "S", "Request status, index-matched to Document ID.", GH_ParamAccess.list);
            pManager.AddTextParameter("Response", "R", "Raw response JSON, index-matched to Document ID.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GHDataHelpers.WarnIfIterated(this, DA);
            if (!IsTriggered) return;

            string token = "";
            var documentIds = new List<string>();
            var requests = new List<string>();
            DA.GetData(0, ref token);
            DA.GetDataList(1, documentIds);
            DA.GetDataList(2, requests);

            if (documentIds.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Document ID supplied.");
                return;
            }

            // accept either one combined array (from the aggregator) or many request objects directly
            string requestsArray = DocumentBuilders.AssembleRequestsArray(requests);

            // A placed run has exactly one content insert at the start of each index space (the
            // first block's first insert). A second insert at index 1 is a block that reached
            // Batch Update without going through Request Aggregator — it would land at the top of
            // the document, above everything placed, and any paragraph style it carries would be
            // inherited by what follows.
            int unplaced = DocumentBuilders.CountInsertsAtSegmentStart(requestsArray);
            if (unplaced > 1)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    unplaced + " content inserts target the start of the document. Only the first placed block should — the others were wired straight into Batch Update (or into Fixed Requests) and were not placed. "
                    + "Route every Text/Paragraph/Table/Insert component through Request Aggregator's Request JSON input.");

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

                var result = client.BatchUpdateDocumentAsync(documentId, requestsArray).GetAwaiter().GetResult();

                if (result.Item1)
                {
                    statuses.Add("Applied.");
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

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_BatchUpdateDocument;
        public override Guid ComponentGuid => new Guid("92eab011-96e6-4c77-9d94-3afaca101350");
    }
}
