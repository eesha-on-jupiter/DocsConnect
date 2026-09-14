using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Newtonsoft.Json.Linq;

namespace DocsConnect
{
    // Canvas-side glue for BlockBuilders. Every chainable Text/Paragraph component registers the
    // same optional "Request JSON" input LAST (so saved definitions keep matching their existing
    // wires by parameter index), then in SolveInstance does:
    //
    //     if (BlockChaining.HasBlock(block))
    //     {
    //         var chained = BlockChaining.ChainTextStyle(block, texts, segmentId, AddRuntimeMessage, ...);
    //         if (chained != null) DA.SetDataList(0, chained);
    //         return;
    //     }
    //
    // falling through to its normal author-my-own-text path when nothing is wired in.
    internal static class BlockChaining
    {
        public const string InputName = "Request JSON";
        public const string InputNickname = "RJ";
        public const string InputDescription =
            "Optional. Wire another Text or Paragraph component's Request JSON in to add this trait to that block's existing text instead of inserting new text. " +
            "Text is ignored while this is connected, and Segment ID is inherited from the incoming block. Chain as many traits as you like before Request Aggregator.";

        // Each component registers the input itself — GH_InputParamManager is protected, so it
        // can't be passed to a helper. Register it LAST (so saved definitions keep matching their
        // existing wires by parameter index), as a tree read back through GHDataHelpers.FlattenText
        // for the same reason every other list input here is a tree: a grafted upstream must not
        // re-solve the component once per branch. Mark it, and Text, Optional:
        //
        //     pManager.AddTextParameter(BlockChaining.InputName, BlockChaining.InputNickname,
        //         BlockChaining.InputDescription, GH_ParamAccess.tree);
        //     pManager[<its index>].Optional = true;

        // True when something is actually wired into the chain input.
        public static bool HasBlock(IList<string> blockJson)
        {
            if (blockJson == null) return false;
            foreach (var s in blockJson)
                if (!string.IsNullOrWhiteSpace(s)) return true;
            return false;
        }

        // Parses the incoming block, finds its text spans, and emits the warnings shared by every
        // chainable component. Returns null when the block can't be styled and the component should
        // output nothing.
        private static List<BlockBuilders.TextSpan> Preflight(
            IList<string> blockJson, IList<string> texts, string segmentId,
            Action<GH_RuntimeMessageLevel, string> report, out JArray reqs)
        {
            reqs = SharedBuilders.FlattenRequests(blockJson);

            if (reqs.Count == 0)
            {
                report(GH_RuntimeMessageLevel.Error,
                    "Request JSON could not be parsed as Google Docs requests. Wire in another Text or Paragraph component's Request JSON output.");
                return null;
            }

            var spans = BlockBuilders.ExtractSpans(reqs);
            if (spans.Count == 0)
            {
                report(GH_RuntimeMessageLevel.Error,
                    "The incoming Request JSON inserts no text, so there is nothing to style. Chaining works on blocks that insert text (Insert Text, Bold Text, Heading Text, Report Skeleton, and so on) — Replace All Text, Insert Image and the break components have no text span to target.");
                return null;
            }

            if (texts != null && texts.Count > 0)
                report(GH_RuntimeMessageLevel.Warning,
                    "Text is ignored while Request JSON is connected — this component is styling the incoming block's existing text. Disconnect Request JSON to insert new text instead.");

            if (!string.IsNullOrWhiteSpace(segmentId))
                report(GH_RuntimeMessageLevel.Remark,
                    "Segment ID is ignored while Request JSON is connected — the segment is inherited from the incoming block.");

            return spans;
        }

        // Warns when the trait inputs left nothing to apply, so a silently unchanged passthrough
        // doesn't read as a working chain.
        private static string[] Finish(
            string[] result, JArray reqs, Action<GH_RuntimeMessageLevel, string> report)
        {
            if (result.Length == reqs.Count)
                report(GH_RuntimeMessageLevel.Warning,
                    "Nothing to apply — the block passed through unchanged. Supply this component's style input to add a trait to the incoming text.");
            return result;
        }

        // Layers text traits (bold, color, font, link, ...) onto the incoming block's spans.
        public static string[] ChainTextStyle(
            IList<string> blockJson, IList<string> texts, string segmentId,
            Action<GH_RuntimeMessageLevel, string> report,
            bool? bold, bool? italic, bool? underline, bool? strikethrough,
            IList<double?> fontSizes, IList<string> fontFamilies, IList<string> textColors,
            IList<string> highlights, IList<string> links, IList<string> baselines)
        {
            var spans = Preflight(blockJson, texts, segmentId, report, out JArray reqs);
            if (spans == null) return null;

            var result = BlockBuilders.AppendTextStyle(reqs, spans,
                bold, italic, underline, strikethrough,
                fontSizes, fontFamilies, textColors, highlights, links, baselines);

            return Finish(result, reqs, report);
        }

        // Layers paragraph traits (named style, alignment, spacing, indent) onto the incoming
        // block's spans.
        public static string[] ChainParagraphStyle(
            IList<string> blockJson, IList<string> texts, string segmentId,
            Action<GH_RuntimeMessageLevel, string> report,
            IList<string> namedStyles, IList<string> alignments,
            IList<double?> lineSpacings, IList<double?> indents,
            IList<double?> spaceAboves, IList<double?> spaceBelows)
        {
            var spans = Preflight(blockJson, texts, segmentId, report, out JArray reqs);
            if (spans == null) return null;

            var result = BlockBuilders.AppendParagraphStyle(reqs, spans,
                namedStyles, alignments, lineSpacings, indents, spaceAboves, spaceBelows);

            return Finish(result, reqs, report);
        }

        // Turns the incoming block's paragraphs into a bulleted list.
        public static string[] ChainBullets(
            IList<string> blockJson, IList<string> texts, string segmentId,
            Action<GH_RuntimeMessageLevel, string> report, string bulletPreset)
        {
            var spans = Preflight(blockJson, texts, segmentId, report, out JArray reqs);
            if (spans == null) return null;

            return BlockBuilders.AppendBullets(reqs, spans, bulletPreset);
        }
    }
}
