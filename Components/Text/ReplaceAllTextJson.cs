using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace DocsConnect
{
    // Builds replaceAllText requests — replaces every occurrence of a string. Find and Replace are
    // index-matched lists: a shorter Replace list repeats its last value.
    public class ReplaceAllTextJsonComponent : GH_Component
    {
        public ReplaceAllTextJsonComponent()
            : base("Replace All Text", "Replace",
                "Builds a replaceAllText request per Find/Replace pair, replacing every occurrence of each. Replace is index-matched to Find (a shorter list repeats its last value).",
                PluginUtilities.TabName, PluginUtilities.CategoryACRequestsText)
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Find", "FT", "Text to find. A list builds one request per item.", GH_ParamAccess.tree);
            pManager.AddTextParameter("Replace", "RT", "Replacement text, index-matched to Find (a shorter list repeats its last value). All branches are flattened into one ordered list.", GH_ParamAccess.tree);
            pManager.AddBooleanParameter("Match Case", "MC", "Whether the search is case-sensitive.", GH_ParamAccess.item, true);
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Request JSON", "RJ", "batchUpdate request JSON.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GHDataHelpers.WarnIfIterated(this, DA);
            var finds = GHDataHelpers.FlattenText(DA, 0);
            var replaces = GHDataHelpers.FlattenText(DA, 1);
            bool matchCase = true;
            DA.GetData(2, ref matchCase);

            if (finds.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Find text supplied.");
                return;
            }

            DA.SetDataList(0, TextRequestBuilders.ReplaceAllTextPairs(finds, replaces, matchCase));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_ReplaceAllTextJson;
        public override Guid ComponentGuid => new Guid("183efc4b-ccfa-45ed-a1ba-c8293d19ee5d");
    }
}
