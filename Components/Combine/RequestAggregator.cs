using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json;

namespace DocsConnect
{
    // The single placement-and-gathering hub. Since no Text/Paragraph/Table/Insert component
    // carries a real index, every one of them wires straight in here (in the order they should
    // appear) and is re-indexed by tallying content length from Start Index. Index-free and
    // absolute-index requests (Replace Image, Update Document Style, Create Header/Footer,
    // Update Text Style with explicit indices, ...) go into Fixed Requests and pass through
    // untouched, appended after the placed content. Feeds Batch Update Document.
    //
    // Run one instance PER segment (body, or each header/footer/footnote) — mixing blocks that
    // target different segments in one run would cross-contaminate the content-length tally.
    public class RequestAggregatorComponent : GH_Component
    {
        public RequestAggregatorComponent()
            : base("Request Aggregator", "Aggregate",
                "Places self-indexed request blocks in wire order from a start index and gathers them, plus any fixed requests, into one requests array for Batch Update. Wire each Text/Paragraph/Table/Insert component directly into Request JSON, in the order it should appear. Run one instance per segment (body, or each header/footer/footnote).",
                PluginUtilities.TabName, PluginUtilities.CategoryAHRequestsAggregate)
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Request JSON", "RJ", "Request blocks in append order — wire each Text/Paragraph/Table/Insert component in the order it should appear — directly (one wire per block) or through a Merge (D1, D2, D3 order). Blocks are re-indexed from Start Index; several blocks arriving in one list are told apart because each starts fresh at index 1.", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Start Index", "IX", "Index the first block lands at. Leave at 1 for the top of the body or of a header/footer/footnote (segments actually start at 0 — the aggregator adjusts when its blocks carry a Segment ID). To append below existing body content, wire End Index from Get Document.", GH_ParamAccess.item, 1);
            pManager.AddTextParameter("Fixed Requests", "FX", "Optional. Requests that already carry a real position or none at all — Replace Image, Replace All Text, Update Document Style, Create Header/Footer, or Update Text/Paragraph Style with explicit indices. Passed through untouched, after the placed blocks.", GH_ParamAccess.tree);
            pManager[0].Optional = true;
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Requests JSON", "RQ", "Combined, re-indexed requests array for Batch Update.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GHDataHelpers.WarnIfIterated(this, DA);
            int startIndex = 1;
            DA.GetData(1, ref startIndex);

            // The tree DA hands us is already MERGED across every wire: Grasshopper appends the
            // items of same-path branches together, so three components each emitting a flat list
            // at {0} arrive as ONE branch. Reindexing that as a single block leaves every request
            // self-indexed at 1, which makes Google apply each insert at the top of the document —
            // the blocks come out in reverse, and each one is inserted *inside* the previous
            // block's first paragraph, so its paragraph style (heading, alignment, bullets) is
            // inherited by everything that follows. Reading the sources directly instead keeps one
            // block per wire. DA.GetDataTree is still called so the parameter is marked as read.
            GH_Structure<GH_String> mergedBlocks;
            DA.GetDataTree(0, out mergedBlocks);
            var blocks = CollectBlocks(Params.Input[0], mergedBlocks);

            GH_Structure<GH_String> mergedFixed;
            DA.GetDataTree(2, out mergedFixed);
            var fixedRequests = new List<string>();
            if (mergedFixed != null)
                foreach (var item in mergedFixed.AllData(true))
                    if (item is GH_String s) fixedRequests.Add(s.Value ?? "");

            // A content insert (text, image, break, table) in Fixed Requests is never right: it is
            // passed through with its own placeholder index, so it lands at index 1 — the top of
            // the document, above everything that was placed. Say so instead of sending it.
            int misrouted = DocumentBuilders.CountContentInserts(fixedRequests);
            if (misrouted > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "Fixed Requests holds " + misrouted + " content insert(s) (text, image, page/section break or table). "
                    + "Fixed Requests are not placed, so these would land at the top of the document. Wire that component into Request JSON instead, in the position it should appear.");
                return;
            }

            if (blocks.Count == 0 && fixedRequests.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No requests wired in.");
                return;
            }

            // Label counts the blocks after ReindexBlocks has split any list that carried several
            // self-indexed blocks (a Merge, a Relay), so it reflects what will actually be placed.
            int placed = 0;
            foreach (var b in blocks)
                placed += DocumentBuilders.SplitSelfIndexedBlocks(SharedBuilders.FlattenRequests(new[] { b })).Count;
            Message = placed + (placed == 1 ? " block" : " blocks")
                + (fixedRequests.Count > 0 ? " + " + fixedRequests.Count + " fixed" : "");

            // A header/footer/footnote segment's content starts at 0, not 1 (no section break in
            // front of it). The default Start Index of 1 means "the top" in either index space, so
            // when the blocks address a segment, 1 becomes 0; an explicit other value is honoured.
            var spaces = DocumentBuilders.IndexSpaces(blocks);
            if (spaces.Count > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "These blocks target " + spaces.Count + " different index spaces (the body and/or several header/footer/footnote segments). "
                    + "Each has its own independent indices, so one aggregator cannot place them together — use one Request Aggregator per segment and feed all of them into Batch Update.");
                return;
            }

            bool segment = DocumentBuilders.TargetsSegment(blocks);
            if (segment && startIndex == DocumentBuilders.BodyStartIndex)
                startIndex = DocumentBuilders.SegmentStartIndex;
            if (segment) Message += " (segment)";

            // isolateStyles: every placed block is reset to plain Normal text on insertion before its
            // own styling applies, so nothing inherits from the paragraph it lands in (the previous
            // run's heading at index 1, a bulleted line at End Index, ...).
            var combined = new List<string> { DocumentBuilders.ReindexBlocks(blocks, startIndex, true) };
            combined.AddRange(fixedRequests);
            DA.SetData(0, DocumentBuilders.AssembleRequestsArray(combined));
        }

        // One block per wired source, and one per branch within that source. A unit component's
        // whole output (Bold Text's insertText+updateTextStyle pair, Report Skeleton's many
        // paragraphs) is a single self-indexed block and must be reindexed by one shared shift, so
        // its items are combined here rather than passed through individually.
        private static List<string> CollectBlocks(IGH_Param param, GH_Structure<GH_String> merged)
        {
            var blocks = new List<string>();

            if (param.SourceCount > 0)
            {
                foreach (var source in param.Sources)
                    AddStructure(blocks, source.VolatileData);
            }
            else if (merged != null)
            {
                // Internalised data — no wires to read, so fall back to the branch contract.
                foreach (var branch in merged.Branches)
                {
                    var items = new List<string>();
                    foreach (var item in branch) items.Add(item?.Value ?? "");
                    AddBlock(blocks, items);
                }
            }

            return blocks;
        }

        private static void AddStructure(List<string> blocks, IGH_Structure data)
        {
            if (data == null || data.IsEmpty) return;

            foreach (var path in data.Paths)
            {
                var branch = data.get_Branch(path);
                if (branch == null) continue;

                var items = new List<string>();
                foreach (var goo in branch)
                {
                    string s;
                    if (GH_Convert.ToString(goo, out s, GH_Conversion.Both) && s != null)
                        items.Add(s);
                }
                AddBlock(blocks, items);
            }
        }

        private static void AddBlock(List<string> blocks, List<string> items)
        {
            var combined = SharedBuilders.FlattenRequests(items);
            if (combined.Count > 0) blocks.Add(combined.ToString(Formatting.None));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GDC_RequestAggregator;
        public override Guid ComponentGuid => new Guid("f7eee773-1fca-4c22-80f6-633c899159d9");
    }
}
