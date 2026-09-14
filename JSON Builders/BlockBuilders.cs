using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DocsConnect
{
    // Style chaining: lets one self-indexed block flow into another Text/Paragraph component so an
    // extra trait layers onto the SAME text instead of inserting a second copy of it. Wire Heading
    // Text's Request JSON into Text Color's Request JSON input and you get one heading paragraph
    // that is also colored, rather than two paragraphs.
    //
    // This is safe because of how Request Aggregator already works. ShiftIndices rewrites every
    // index/startIndex/endIndex in a branch by one shared offset, so appended requests travel with
    // the block they were appended to. ContentLength counts only insertText (text length),
    // insertInlineImage/insertPageBreak/insertSectionBreak (+1 each) and deleteContentRange
    // (negative) — updateTextStyle, updateParagraphStyle and createParagraphBullets all contribute
    // ZERO, so appending them leaves every downstream position untouched. Google applies requests in
    // array order, so a chained trait layers on top of whatever the upstream block already set.
    public static class BlockBuilders
    {
        #region Span extraction

        // One inserted-text run inside a self-indexed block, plus the segment it targets.
        public struct TextSpan
        {
            public int Start;
            public int End;
            public string SegmentId;
        }

        // Every insertText run in a block, in request order. Start/End are the block's own
        // self-indexed positions and the paragraph-terminating "\n" is included — the same range
        // InsertStyledTextLines / InsertStyledParagraphLines build for their own style requests, so
        // a chained trait lands on exactly the span the upstream component styled.
        //
        // A block with N lines yields N spans (Heading Text with a 3-item list, Report Skeleton,
        // Bullet List). A "Same Paragraph" run from InsertStyledRun is a single insertText and so
        // yields ONE span covering the whole run — a chained trait applies to all of it uniformly.
        public static List<TextSpan> ExtractSpans(JArray reqs)
        {
            var spans = new List<TextSpan>();
            if (reqs == null) return spans;

            foreach (var r in reqs)
            {
                var insert = r?["insertText"];
                var location = insert?["location"];
                var indexTok = location?["index"];
                if (indexTok == null || indexTok.Type != JTokenType.Integer) continue;

                string text = insert.Value<string>("text") ?? "";
                if (text.Length == 0) continue;

                int start = (int)indexTok;
                spans.Add(new TextSpan
                {
                    Start = start,
                    End = start + text.Length,
                    SegmentId = location.Value<string>("segmentId")
                });
            }

            return spans;
        }

        #endregion

        #region Cell content (formatted text inside a table cell)

        // A table cell's content: the plain text to insert, plus the style requests that came with
        // it. Styles keep the incoming block's own 1-based coordinates — BuildTable shifts them to
        // the cell's final index once it knows where the text landed.
        public struct CellContent
        {
            public string Text;
            public JArray Styles;
        }

        // Every top-level key that identifies a Docs batchUpdate request. Used to tell "this cell
        // is a Request JSON block" from "this cell is a string that happens to parse as JSON"
        // (a bare number, a quoted word, or a JSON-looking value the user actually wants printed).
        private static readonly HashSet<string> RequestKeys = new HashSet<string>
        {
            "insertText", "updateTextStyle", "updateParagraphStyle", "createParagraphBullets",
            "deleteParagraphBullets", "deleteContentRange", "replaceAllText", "insertTable",
            "insertInlineImage", "insertPageBreak", "insertSectionBreak", "replaceImage"
        };

        // True when a string is Docs request JSON — an object or array carrying at least one request
        // key — as opposed to text that merely parses as JSON (a bare number, a quoted word).
        public static bool IsRequestJson(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return false;
            JToken tok;
            try { tok = JToken.Parse(raw); }
            catch { return false; }

            IEnumerable<JToken> items = tok is JArray arr ? (IEnumerable<JToken>)arr : new[] { tok };
            foreach (var r in items)
            {
                if (!(r is JObject o)) continue;
                foreach (var prop in o.Properties())
                    if (RequestKeys.Contains(prop.Name)) return true;
            }
            return false;
        }

        // Turns the items of one table row (one Grasshopper branch) into one string per cell.
        //
        // Plain strings are cells as they are. Request JSON is what a Text/Paragraph component
        // emits: a LIST of requests (insertText, updateTextStyle, insertText, ...) — one list item
        // per request, several lines per component. Consecutive request items are combined into
        // blocks (split apart again where a fresh block restarts at index 1, exactly as Request
        // Aggregator does), and then every insertText line of a block becomes its own cell, carrying
        // the style requests that overlap that line, re-based so the cell is a single-line
        // self-indexed block. So Text Color with three texts and three colours, wired into one row,
        // yields three coloured cells — instead of six items with the text and its colour apart.
        public static List<string> ExpandCells(IList<string> items)
        {
            var cells = new List<string>();
            var run = new List<string>();

            void Flush()
            {
                if (run.Count == 0) return;
                var flat = SharedBuilders.FlattenRequests(run);
                foreach (var block in DocumentBuilders.SplitSelfIndexedBlocks(flat))
                    cells.AddRange(SplitBlockIntoLineCells(block));
                run.Clear();
            }

            if (items != null)
            {
                foreach (var raw in items)
                {
                    if (IsRequestJson(raw)) run.Add(raw);
                    else { Flush(); cells.Add(raw ?? ""); }
                }
            }
            Flush();
            return cells;
        }

        // One single-line block per insertText in the block: the line's insertText at index 1 plus
        // every style request whose range overlaps the line, clamped to it and shifted to start at 1.
        // A block with no insertText (nothing to put in a cell) is passed through whole.
        private static List<string> SplitBlockIntoLineCells(JArray block)
        {
            var result = new List<string>();
            var spans = ExtractSpans(block);
            if (spans.Count == 0) { result.Add(block.ToString(Formatting.None)); return result; }

            var styles = new List<JObject>();
            foreach (var r in block)
                if (r is JObject o && o["insertText"] == null) styles.Add(o);

            int spanIndex = 0;
            foreach (var r in block)
            {
                if (!(r is JObject o) || o["insertText"] == null) continue;
                var span = spans[spanIndex++];
                int shift = 1 - span.Start;

                var cell = new JArray();
                var insert = (JObject)o.DeepClone();
                insert["insertText"]["location"]["index"] = 1;
                cell.Add(insert);

                foreach (var style in styles)
                {
                    if (!TryGetRange(style, out JObject range)) continue;
                    int s = Math.Max((int)range["startIndex"], span.Start);
                    int e = Math.Min((int)range["endIndex"], span.End);
                    if (e <= s) continue;

                    var placed = (JObject)style.DeepClone();
                    TryGetRange(placed, out JObject placedRange);
                    placedRange["startIndex"] = s + shift;
                    placedRange["endIndex"] = e + shift;
                    cell.Add(placed);
                }

                result.Add(cell.ToString(Formatting.None));
            }
            return result;
        }

        // The "range" object of a style/bullets request, i.e. req[<requestType>]["range"].
        private static bool TryGetRange(JObject req, out JObject range)
        {
            range = null;
            foreach (var prop in req.Properties())
            {
                range = prop.Value?["range"] as JObject;
                if (range != null && range["startIndex"]?.Type == JTokenType.Integer && range["endIndex"]?.Type == JTokenType.Integer)
                    return true;
            }
            range = null;
            return false;
        }

        // Reads one cell value as either a self-indexed Request JSON block (from Bold Text, Text
        // Color, Styled Paragraph, a chain of them, ...) or as literal text.
        //
        // For a block: the text is the concatenation of its insertText runs in request order, and
        // the styles are everything else. The block's trailing paragraph break is stripped — a
        // cell already ends in its own paragraph, so keeping the "\n" would leave an empty second
        // paragraph in every cell — and any style range that ran past it is clamped back.
        public static CellContent ExtractCellContent(string raw)
        {
            var plain = new CellContent { Text = raw ?? "", Styles = null };
            if (string.IsNullOrEmpty(raw)) return plain;

            JToken tok;
            try { tok = JToken.Parse(raw); }
            catch { return plain; }

            var reqs = new JArray();
            if (tok is JArray arr) { foreach (var t in arr) reqs.Add(t); }
            else if (tok is JObject obj) { reqs.Add(obj); }
            else return plain;

            bool isBlock = false;
            foreach (var r in reqs)
            {
                if (!(r is JObject o)) continue;
                foreach (var prop in o.Properties())
                    if (RequestKeys.Contains(prop.Name)) { isBlock = true; break; }
                if (isBlock) break;
            }
            if (!isBlock) return plain;

            var text = new System.Text.StringBuilder();
            var styles = new JArray();
            foreach (var r in reqs)
            {
                if (!(r is JObject o)) continue;
                var insert = o["insertText"];
                if (insert != null) { text.Append(insert.Value<string>("text") ?? ""); continue; }
                styles.Add(o);
            }

            string cellText = text.ToString().TrimEnd('\n');
            int maxEnd = 1 + cellText.Length;

            var kept = new JArray();
            foreach (var s in styles)
            {
                if (ClampRanges(s, maxEnd)) kept.Add(s);
            }

            return new CellContent { Text = cellText, Styles = kept };
        }

        // Clamps every range in a request to maxEnd. Returns false when the request no longer
        // covers anything (its whole span was the stripped paragraph break), so it can be dropped
        // rather than sent as an empty range Google would reject.
        private static bool ClampRanges(JToken token, int maxEnd)
        {
            if (token is JObject obj)
            {
                var startTok = obj["startIndex"];
                var endTok = obj["endIndex"];
                if (startTok != null && endTok != null
                    && startTok.Type == JTokenType.Integer && endTok.Type == JTokenType.Integer)
                {
                    int start = (int)startTok;
                    int end = Math.Min((int)endTok, maxEnd);
                    if (end <= start) return false;
                    obj["endIndex"] = end;
                }

                foreach (var prop in obj.Properties())
                    if (!ClampRanges(prop.Value, maxEnd)) return false;
                return true;
            }

            if (token is JArray arr)
            {
                foreach (var item in arr)
                    if (!ClampRanges(item, maxEnd)) return false;
            }
            return true;
        }

        #endregion

        #region Appending style to an existing block

        // The incoming block's own requests, unchanged, serialised back to strings. Chained style
        // requests are appended after these so they apply on top.
        private static List<string> Passthrough(JArray reqs)
        {
            var list = new List<string>();
            if (reqs == null) return list;
            foreach (var r in reqs) list.Add(r.ToString(Formatting.None));
            return list;
        }

        // True when a built style request actually sets something. UpdateTextStyle /
        // UpdateParagraphStyle build their fields mask from whatever was supplied, so a component
        // chained in with none of its trait wired up would otherwise emit an empty-mask request —
        // which Google rejects rather than ignores.
        private static bool HasFields(string requestJson, string requestType)
        {
            try
            {
                var fields = JObject.Parse(requestJson)[requestType]?.Value<string>("fields");
                return !string.IsNullOrEmpty(fields);
            }
            catch { return false; }
        }

        // Appends one updateTextStyle per span. Per-span traits are index-matched to the spans with
        // the usual "a shorter list repeats its last value" convention, so a single color covers
        // every line of a multi-line block while a per-line list styles each line separately.
        // SegmentId is inherited from each span rather than re-supplied.
        public static string[] AppendTextStyle(
            JArray reqs, IList<TextSpan> spans,
            bool? bold, bool? italic, bool? underline, bool? strikethrough,
            IList<double?> fontSizes, IList<string> fontFamilies, IList<string> textColors,
            IList<string> highlights, IList<string> links, IList<string> baselines)
        {
            var outReqs = Passthrough(reqs);
            if (spans == null) return outReqs.ToArray();

            for (int i = 0; i < spans.Count; i++)
            {
                var s = spans[i];
                string req = TextRequestBuilders.UpdateTextStyle(
                    s.Start, s.End,
                    bold, italic, underline, strikethrough,
                    TextRequestBuilders.GetOrLast(fontSizes, i, (double?)null),
                    TextRequestBuilders.GetOrLast(fontFamilies, i, ""),
                    TextRequestBuilders.GetOrLast(textColors, i, ""),
                    TextRequestBuilders.GetOrLast(highlights, i, ""),
                    TextRequestBuilders.GetOrLast(links, i, ""),
                    TextRequestBuilders.GetOrLast(baselines, i, ""),
                    s.SegmentId);

                if (HasFields(req, "updateTextStyle")) outReqs.Add(req);
            }

            return outReqs.ToArray();
        }

        // Appends one updateParagraphStyle per span, same index-matching rules as AppendTextStyle.
        public static string[] AppendParagraphStyle(
            JArray reqs, IList<TextSpan> spans,
            IList<string> namedStyles, IList<string> alignments,
            IList<double?> lineSpacings, IList<double?> indents,
            IList<double?> spaceAboves, IList<double?> spaceBelows)
        {
            var outReqs = Passthrough(reqs);
            if (spans == null) return outReqs.ToArray();

            for (int i = 0; i < spans.Count; i++)
            {
                var s = spans[i];
                string req = ParagraphRequestBuilders.UpdateParagraphStyle(
                    s.Start, s.End,
                    TextRequestBuilders.GetOrLast(namedStyles, i, ""),
                    TextRequestBuilders.GetOrLast(alignments, i, ""),
                    TextRequestBuilders.GetOrLast(lineSpacings, i, (double?)null),
                    TextRequestBuilders.GetOrLast(indents, i, (double?)null),
                    TextRequestBuilders.GetOrLast(spaceAboves, i, (double?)null),
                    TextRequestBuilders.GetOrLast(spaceBelows, i, (double?)null),
                    s.SegmentId);

                if (HasFields(req, "updateParagraphStyle")) outReqs.Add(req);
            }

            return outReqs.ToArray();
        }

        // Appends one createParagraphBullets per span. It carries no fields mask — the preset is
        // always set — so every span gets one.
        public static string[] AppendBullets(JArray reqs, IList<TextSpan> spans, string bulletPreset)
        {
            var outReqs = Passthrough(reqs);
            if (spans == null) return outReqs.ToArray();

            foreach (var s in spans)
                outReqs.Add(ParagraphRequestBuilders.CreateParagraphBullets(s.Start, s.End, bulletPreset, s.SegmentId));

            return outReqs.ToArray();
        }

        #endregion
    }
}
