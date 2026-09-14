using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace DocsConnect
{
    // Flattens a Grasshopper data tree into one ordered list across all branches. GH_ParamAccess.list
    // solves a component once PER BRANCH when the upstream source is a multi-branch tree (common for
    // numeric/geometry results), which silently resets any running state — like the insertion cursor
    // in InsertStyledTextLines — on every branch. Reading as a tree and flattening here keeps every
    // Text/style-list unit component working as a single flat, index-matched pass no matter how
    // grafted the upstream data is.
    internal static class GHDataHelpers
    {
        // Every request-emitting component flattens its list/tree inputs itself, so it is meant to
        // solve exactly once. A second iteration means a list reached a single-value input (a list
        // of bullet presets, several Segment IDs, a list of Start Indices) and Grasshopper is
        // re-running the whole component per value — emitting the entire block again each time.
        // On the canvas that only shows as extra branches on the output, and in the document as
        // duplicated content, so name the input and say what happened.
        public static void WarnIfIterated(GH_Component component, IGH_DataAccess DA)
        {
            if (DA.Iteration == 0) return;

            var offenders = new List<string>();
            foreach (var p in component.Params.Input)
                if (p.Access == GH_ParamAccess.item && p.VolatileDataCount > 1) offenders.Add(p.Name);

            string where = offenders.Count > 0 ? string.Join(", ", offenders) : "an input that takes one value";
            component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                "This component ran " + (DA.Iteration + 1) + " times because a list reached " + where
                + ". Each run emits the whole block again, so the document would hold duplicates — supply a single value there.");
        }

        // Per-cell lookup into a format tree wired alongside a table's Data tree. Row = branch,
        // column = item, with the usual "a shorter list repeats its last value" in both directions,
        // so one item formats every cell and one branch formats every row. A tree that is a single
        // branch longer than one row is read row-major (the flat-list form Fixed Size Table also
        // accepts for Data). An empty tree yields null everywhere.
        public static T CellValue<T>(GH_Structure<T> tree, int row, int col, int cols) where T : class, IGH_Goo
        {
            if (tree == null || tree.IsEmpty) return null;

            if (tree.Branches.Count == 1 && tree.Branches[0].Count > cols)
            {
                var flat = tree.Branches[0];
                return flat[Math.Min(row * cols + col, flat.Count - 1)];
            }

            var branch = tree.Branches[Math.Min(row, tree.Branches.Count - 1)];
            if (branch == null || branch.Count == 0) return null;
            return branch[Math.Min(col, branch.Count - 1)];
        }

        // Reads a tree input without failing the solve when nothing is wired in.
        public static GH_Structure<T> TreeOrNull<T>(IGH_DataAccess DA, int paramIndex) where T : IGH_Goo
        {
            GH_Structure<T> tree;
            return DA.GetDataTree(paramIndex, out tree) ? tree : null;
        }

        public static List<string> FlattenText(IGH_DataAccess DA, int paramIndex)
        {
            var list = new List<string>();
            if (!DA.GetDataTree(paramIndex, out GH_Structure<GH_String> tree) || tree == null) return list;
            foreach (var branch in tree.Branches)
                foreach (var item in branch)
                    list.Add(SharedBuilders.NormalizeLineEndings(item?.Value));
            return list;
        }

        public static List<double> FlattenNumber(IGH_DataAccess DA, int paramIndex)
        {
            var list = new List<double>();
            if (!DA.GetDataTree(paramIndex, out GH_Structure<GH_Number> tree) || tree == null) return list;
            foreach (var branch in tree.Branches)
                foreach (var item in branch)
                    list.Add(item?.Value ?? 0.0);
            return list;
        }

        public static List<int> FlattenInteger(IGH_DataAccess DA, int paramIndex)
        {
            var list = new List<int>();
            if (!DA.GetDataTree(paramIndex, out GH_Structure<GH_Integer> tree) || tree == null) return list;
            foreach (var branch in tree.Branches)
                foreach (var item in branch)
                    list.Add(item?.Value ?? 0);
            return list;
        }
    }
}
