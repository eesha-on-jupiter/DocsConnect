using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DocsConnect
{
    // Builds Google Docs batchUpdate paragraph requests
    public static class ParagraphRequestBuilders
    {
        #region Paragraph requests

        // updateParagraphStyle — only sets supplied fields and builds the matching fields mask.
        // segmentId targets a header/footer/footnote segment instead of the body.
        public static string UpdateParagraphStyle(
            int startIndex, int endIndex,
            string namedStyle, string alignment,
            double? lineSpacing, double? indent, double? spaceAbove, double? spaceBelow,
            string segmentId = null)
        {
            var style = new JObject();
            var fields = new List<string>();

            if (!string.IsNullOrWhiteSpace(namedStyle)) { style["namedStyleType"] = namedStyle; fields.Add("namedStyleType"); }
            if (!string.IsNullOrWhiteSpace(alignment)) { style["alignment"] = alignment; fields.Add("alignment"); }
            if (lineSpacing.HasValue && lineSpacing.Value > 0) { style["lineSpacing"] = lineSpacing.Value; fields.Add("lineSpacing"); }
            if (indent.HasValue && indent.Value > 0) { style["indentStart"] = SharedBuilders.Pt(indent.Value); fields.Add("indentStart"); }
            if (spaceAbove.HasValue && spaceAbove.Value > 0) { style["spaceAbove"] = SharedBuilders.Pt(spaceAbove.Value); fields.Add("spaceAbove"); }
            if (spaceBelow.HasValue && spaceBelow.Value > 0) { style["spaceBelow"] = SharedBuilders.Pt(spaceBelow.Value); fields.Add("spaceBelow"); }

            var inner = new JObject
            {
                ["range"] = SharedBuilders.Range(startIndex, endIndex, segmentId),
                ["paragraphStyle"] = style,
                ["fields"] = string.Join(",", fields)
            };
            return SharedBuilders.Wrap("updateParagraphStyle", inner);
        }

        // Every BulletGlyphType the Docs API accepts. A value outside this set (an indent level wired
        // into Bullet Preset by mistake, a typo) would be rejected by Google with a 400 — checking it
        // here lets the component say what went wrong instead.
        public static readonly string[] BulletPresets =
        {
            "BULLET_DISC_CIRCLE_SQUARE", "BULLET_DIAMONDX_ARROW3D_SQUARE", "BULLET_CHECKBOX",
            "BULLET_ARROW_DIAMOND_DISC", "BULLET_STAR_CIRCLE_SQUARE", "BULLET_ARROW3D_CIRCLE_SQUARE",
            "BULLET_LEFTTRIANGLE_DIAMOND_DISC", "BULLET_DIAMONDX_HOLLOWDIAMOND_SQUARE", "BULLET_DIAMOND_CIRCLE_SQUARE",
            "NUMBERED_DECIMAL_ALPHA_ROMAN", "NUMBERED_DECIMAL_ALPHA_ROMAN_PARENS", "NUMBERED_DECIMAL_NESTED",
            "NUMBERED_UPPERALPHA_ALPHA_ROMAN", "NUMBERED_UPPERROMAN_UPPERALPHA_DECIMAL", "NUMBERED_ZERODECIMAL_ALPHA_ROMAN",
        };

        public static bool IsBulletPreset(string preset)
        {
            if (string.IsNullOrWhiteSpace(preset)) return true; // blank falls back to the default
            return System.Array.IndexOf(BulletPresets, preset.Trim()) >= 0;
        }

        // createParagraphBullets — segmentId targets a header/footer/footnote segment instead of the body
        public static string CreateParagraphBullets(int startIndex, int endIndex, string bulletPreset, string segmentId = null)
        {
            var inner = new JObject
            {
                ["range"] = SharedBuilders.Range(startIndex, endIndex, segmentId),
                ["bulletPreset"] = string.IsNullOrWhiteSpace(bulletPreset) ? "BULLET_DISC_CIRCLE_SQUARE" : bulletPreset
            };
            return SharedBuilders.Wrap("createParagraphBullets", inner);
        }

        // deleteParagraphBullets
        public static string DeleteParagraphBullets(int startIndex, int endIndex)
        {
            var inner = new JObject { ["range"] = SharedBuilders.Range(startIndex, endIndex) };
            return SharedBuilders.Wrap("deleteParagraphBullets", inner);
        }

        // insertText + updateParagraphStyle for each entry in texts, chained sequentially — each line
        // is its own paragraph. namedStyles/alignments/lineSpacings/indents/spaceAboves/spaceBelows are
        // index-matched to texts — a shorter list repeats its last value, and an empty (or null) list
        // leaves that trait unset for every line. Self-indexed from 1 — place via Request Aggregator.
        // segmentId targets a header/footer/footnote segment instead of the body.
        public static string[] InsertStyledParagraphLines(
            IList<string> texts,
            IList<string> namedStyles, IList<string> alignments,
            IList<double?> lineSpacings, IList<double?> indents, IList<double?> spaceAboves, IList<double?> spaceBelows,
            string segmentId = null)
        {
            var reqs = new List<string>();
            int cursor = 1;
            int n = texts?.Count ?? 0;

            for (int i = 0; i < n; i++)
            {
                string line = (texts[i] ?? "") + "\n";
                string namedStyle = TextRequestBuilders.GetOrLast(namedStyles, i, "");
                string alignment = TextRequestBuilders.GetOrLast(alignments, i, "");
                double? lineSpacing = TextRequestBuilders.GetOrLast(lineSpacings, i, (double?)null);
                double? indent = TextRequestBuilders.GetOrLast(indents, i, (double?)null);
                double? spaceAbove = TextRequestBuilders.GetOrLast(spaceAboves, i, (double?)null);
                double? spaceBelow = TextRequestBuilders.GetOrLast(spaceBelows, i, (double?)null);

                int start = cursor;
                int end = start + line.Length;

                reqs.Add(TextRequestBuilders.InsertText(line, start, segmentId));
                reqs.Add(UpdateParagraphStyle(start, end, namedStyle, alignment, lineSpacing, indent, spaceAbove, spaceBelow, segmentId));

                cursor = end;
            }

            return reqs.ToArray();
        }

        // insertText + createParagraphBullets for each entry in texts, chained sequentially — each line
        // is its own bulleted paragraph. bulletPreset applies uniformly to every line. Self-indexed from
        // 1 — place via Request Aggregator. segmentId targets a header/footer/footnote segment instead of the body.
        public static string[] InsertBulletedParagraphLines(IList<string> texts, string bulletPreset, string segmentId = null)
        {
            var reqs = new List<string>();
            int cursor = 1;
            int n = texts?.Count ?? 0;

            for (int i = 0; i < n; i++)
            {
                string line = (texts[i] ?? "") + "\n";
                int start = cursor;
                int end = start + line.Length;

                reqs.Add(TextRequestBuilders.InsertText(line, start, segmentId));
                reqs.Add(CreateParagraphBullets(start, end, bulletPreset, segmentId));

                cursor = end;
            }

            return reqs.ToArray();
        }

        #endregion
    }
}
