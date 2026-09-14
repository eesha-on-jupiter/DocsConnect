using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace DocsConnect
{
    // Builds a bulleted, numbered or checkbox list from a list of strings.
    public class BulletListComponent : GH_Component
    {
        public BulletListComponent()
            : base("Bullet List", "BulletList",
                "Builds the requests for a bulleted, numbered or checkbox list from a list of strings. No index needed — wire it into Request Aggregator to place it in the document. For a single bulleted line, use Bullet Item Text instead.",
                PluginUtilities.TabName, PluginUtilities.CategoryADRequestsParagraph)
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Items", "IT", "List item texts.", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Indent Levels", "IL", "Optional indent level per item (0-based). All branches are flattened into one ordered list.", GH_ParamAccess.tree);
            pManager.AddTextParameter("Bullet Preset", "BP", "Bullet preset, e.g. BULLET_CHECKBOX.", GH_ParamAccess.item, "BULLET_DISC_CIRCLE_SQUARE");
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Request JSON", "RJ", "batchUpdate requests array.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GHDataHelpers.WarnIfIterated(this, DA);
            var items = GHDataHelpers.FlattenText(DA, 0);
            var indents = GHDataHelpers.FlattenInteger(DA, 1);
            string preset = "BULLET_DISC_CIRCLE_SQUARE";

            DA.GetData(2, ref preset);

            if (!ParagraphRequestBuilders.IsBulletPreset(preset))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "Bullet Preset \"" + preset + "\" is not a Google Docs bullet preset (e.g. BULLET_DISC_CIRCLE_SQUARE, BULLET_CHECKBOX, NUMBERED_DECIMAL_ALPHA_ROMAN — use the Bullet Glyph preset dropdown). "
                    + "If you meant nesting levels, those go into Bullet List's Indent Levels input, not here.");
                return;
            }

            if (items.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No items supplied.");
                return;
            }

            DA.SetDataList(0, new List<string> { AecHelperBuilders.BulletList(items, indents, preset) });
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_BulletList;
        public override Guid ComponentGuid => new Guid("c86bbaf6-de2e-4265-8012-ef788084b143");
    }
}
