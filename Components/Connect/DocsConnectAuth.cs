using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Newtonsoft.Json.Linq;

namespace DocsConnect
{
    // Validates one or more Google OAuth access tokens and reports the granted scopes for each.
    // Status/Response are index-matched to Token; one invalid token does not stop the rest.
    public class DocsConnectAuthComponent : ButtonComponent
    {
        public DocsConnectAuthComponent()
            : base("Connect", "Auth",
                "Validates one or more Google OAuth access tokens via Google tokeninfo and reports the granted scopes for each. Status/Response are index-matched to Token.",
                PluginUtilities.TabName, PluginUtilities.CategoryAAAuth)
        { }

        public override string ButtonLabel => "Connect";

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Token", "T", "Google OAuth 2.0 access token(s) (Bearer).", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "S", "Connection status, index-matched to Token.", GH_ParamAccess.list);
            pManager.AddTextParameter("Response", "R", "Raw tokeninfo response, index-matched to Token.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!IsTriggered) return;

            var tokens = new List<string>();
            DA.GetDataList(0, tokens);

            if (tokens.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No token supplied.");
                return;
            }

            var statuses = new List<string>();
            var responses = new List<string>();

            foreach (var token in tokens)
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    statuses.Add("No token supplied.");
                    responses.Add("");
                    continue;
                }

                var client = new DocsConnectClient(token);
                var result = client.ValidateTokenAsync().GetAwaiter().GetResult();

                if (result.Item1)
                {
                    string response = result.Item2;
                    string scope = "";
                    try { scope = JObject.Parse(response).Value<string>("scope") ?? ""; }
                    catch { /* leave scope blank if response is not JSON */ }

                    statuses.Add(string.IsNullOrWhiteSpace(scope) ? "Connected." : "Connected — scopes: " + scope);
                    responses.Add(response);
                }
                else
                {
                    statuses.Add("Failed to authenticate.");
                    responses.Add(result.Item3);
                }
            }

            DA.SetDataList(0, statuses);
            DA.SetDataList(1, responses);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_DocsConnectAuth;
        public override Guid ComponentGuid => new Guid("16492490-cf84-4d86-8d21-a80f31a96622");
    }
}
