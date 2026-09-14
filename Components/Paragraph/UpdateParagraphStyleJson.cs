using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;

namespace DocsConnect
{
    // Builds an insertText + updateParagraphStyle combo — named style, alignment, spacing and
    // indent, all in one self-contained block. No index needed. Use this for combining multiple
    // paragraph traits on the same text; the atomic components below insert a separate copy of
    // the text each. All inputs are index-matched lists — a list of Text produces one paragraph
    // per item, and a shorter trait list repeats its last value.
    public class UpdateParagraphStyleJsonComponent : GH_Component
    {
        public UpdateParagraphStyleJsonComponent()
            : base("Styled Paragraph", "ParaStyle",
                "Inserts one or more paragraphs with any combination of paragraph-level styles applied — named style, alignment, line spacing, indent, space above/below. Only supplied properties are applied. All inputs are index-matched lists (a list of Text produces one paragraph per item; a shorter trait list repeats its last value). No index needed.",
                PluginUtilities.TabName, PluginUtilities.CategoryADRequestsParagraph)
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "TX", "Text to insert as a paragraph. A list places each entry in its own paragraph.", GH_ParamAccess.list);
            pManager.AddTextParameter("Named Style", "NS", "Named style type, e.g. HEADING_1 (blank to skip). Index-matched to Text (a shorter list repeats its last value).", GH_ParamAccess.list, "");
            pManager.AddTextParameter("Alignment", "AL", "Alignment: START, CENTER, END, JUSTIFIED (blank to skip). Index-matched to Text (a shorter list repeats its last value).", GH_ParamAccess.list, "");
            pManager.AddNumberParameter("Line Spacing", "LS", "Line spacing percent, e.g. 115 (>0 to apply). Index-matched to Text (a shorter list repeats its last value).", GH_ParamAccess.list, 0.0);
            pManager.AddNumberParameter("Indent", "IN", "Start indent in points (>0 to apply). Index-matched to Text (a shorter list repeats its last value).", GH_ParamAccess.list, 0.0);
            pManager.AddNumberParameter("Space Above", "SA", "Space above paragraph in points (>0 to apply). Index-matched to Text (a shorter list repeats its last value).", GH_ParamAccess.list, 0.0);
            pManager.AddNumberParameter("Space Below", "SB", "Space below paragraph in points (>0 to apply). Index-matched to Text (a shorter list repeats its last value).", GH_ParamAccess.list, 0.0);
            pManager.AddTextParameter("Segment ID", "SEG", "Header/Footer/Footnote ID to target that segment instead of the body (blank = body).", GH_ParamAccess.item, "");
            pManager[0].Optional = true;
            pManager.AddTextParameter(BlockChaining.InputName, BlockChaining.InputNickname,
                BlockChaining.InputDescription, GH_ParamAccess.tree);
            pManager[8].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Request JSON", "RJ", "insertText + updateParagraphStyle batchUpdate requests.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GHDataHelpers.WarnIfIterated(this, DA);
            var texts = new List<string>();
            var namedStyles = new List<string>();
            var alignments = new List<string>();
            var lineSpacings = new List<double>();
            var indents = new List<double>();
            var spaceAboves = new List<double>();
            var spaceBelows = new List<double>();
            string segmentId = "";

            DA.GetDataList(0, texts);
            DA.GetDataList(1, namedStyles);
            DA.GetDataList(2, alignments);
            DA.GetDataList(3, lineSpacings);
            DA.GetDataList(4, indents);
            DA.GetDataList(5, spaceAboves);
            DA.GetDataList(6, spaceBelows);
            DA.GetData(7, ref segmentId);
            var block = GHDataHelpers.FlattenText(DA, 8);

            var lineSpacingsN = lineSpacings.Select(v => v > 0 ? v : (double?)null).ToList();
            var indentsN = indents.Select(v => v > 0 ? v : (double?)null).ToList();
            var spaceAbovesN = spaceAboves.Select(v => v > 0 ? v : (double?)null).ToList();
            var spaceBelowsN = spaceBelows.Select(v => v > 0 ? v : (double?)null).ToList();

            if (BlockChaining.HasBlock(block))
            {
                var chained = BlockChaining.ChainParagraphStyle(block, texts, segmentId, AddRuntimeMessage,
                    namedStyles, alignments, lineSpacingsN, indentsN, spaceAbovesN, spaceBelowsN);
                if (chained != null) DA.SetDataList(0, chained);
                return;
            }

            if (texts.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No text supplied.");
                return;
            }

            var json = ParagraphRequestBuilders.InsertStyledParagraphLines(
                texts, namedStyles, alignments,
                lineSpacingsN, indentsN, spaceAbovesN, spaceBelowsN,
                segmentId);

            DA.SetDataList(0, json);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_UpdateParagraphStyleJson;
        public override Guid ComponentGuid => new Guid("143e274a-20f5-40eb-9159-189188988da4");
    }
}
