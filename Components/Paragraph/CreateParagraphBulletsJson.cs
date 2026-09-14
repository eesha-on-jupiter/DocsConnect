using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace DocsConnect
{
    // Builds an insertText + createParagraphBullets combo — one or more bulleted lines sharing a
    // single preset, no index needed. For per-item indent levels, use Bullet List (Report Tools) instead.
    public class CreateParagraphBulletsJsonComponent : GH_Component
    {
        public CreateParagraphBulletsJsonComponent()
            : base("Bullet Item Text", "Bullets",
                "Inserts one or more bulleted lines of text using a bullet preset. A list of Text produces one bulleted line per item. No index needed.",
                PluginUtilities.TabName, PluginUtilities.CategoryADRequestsParagraph)
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "TX", "Text of the bulleted line(s). A list places each entry on its own line.", GH_ParamAccess.tree);
            pManager.AddTextParameter("Bullet Preset", "BP", "Bullet preset, e.g. BULLET_CHECKBOX.", GH_ParamAccess.item, "BULLET_DISC_CIRCLE_SQUARE");
            pManager.AddTextParameter("Segment ID", "SEG", "Header/Footer/Footnote ID to target that segment instead of the body (blank = body).", GH_ParamAccess.item, "");
            pManager[0].Optional = true;
            pManager.AddTextParameter(BlockChaining.InputName, BlockChaining.InputNickname,
                BlockChaining.InputDescription, GH_ParamAccess.tree);
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Request JSON", "RJ", "insertText + createParagraphBullets batchUpdate requests.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GHDataHelpers.WarnIfIterated(this, DA);
            var texts = GHDataHelpers.FlattenText(DA, 0);
            string preset = "BULLET_DISC_CIRCLE_SQUARE";
            string segmentId = "";
            DA.GetData(1, ref preset);
            DA.GetData(2, ref segmentId);

            if (!ParagraphRequestBuilders.IsBulletPreset(preset))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "Bullet Preset \"" + preset + "\" is not a Google Docs bullet preset (e.g. BULLET_DISC_CIRCLE_SQUARE, BULLET_CHECKBOX, NUMBERED_DECIMAL_ALPHA_ROMAN — use the Bullet Glyph preset dropdown). "
                    + "If you meant nesting levels, those go into Bullet List's Indent Levels input, not here.");
                return;
            }
            var block = GHDataHelpers.FlattenText(DA, 3);

            if (BlockChaining.HasBlock(block))
            {
                var chained = BlockChaining.ChainBullets(block, texts, segmentId, AddRuntimeMessage, preset);
                if (chained != null) DA.SetDataList(0, chained);
                return;
            }

            if (texts.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No text supplied.");
                return;
            }

            var json = ParagraphRequestBuilders.InsertBulletedParagraphLines(texts, preset, segmentId);
            DA.SetDataList(0, json);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_CreateParagraphBulletsJson;
        public override Guid ComponentGuid => new Guid("0dff2228-bb99-4e86-ba86-d45f924579b0");
    }
}
