using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DocsConnect
{
    // Assembly + index helpers for the batchUpdate requests array
    public static class DocumentBuilders
    {
        #region Requests array

        // gathers many request JSON strings (objects or arrays) into one requests-array JSON string
        public static string AssembleRequestsArray(IEnumerable<string> requestJson)
        {
            return SharedBuilders.FlattenRequests(requestJson).ToString(Formatting.None);
        }

        #endregion

        #region Document growth per request

        // How many UTF-16 indices each structural insert adds, per the Docs API reference
        // (developers.google.com/docs/api/reference/rest/v1/documents/request). Every builder that
        // self-sequences several of these, and ContentLength below, read from here — there is
        // exactly one place to be wrong.
        //   insertInlineImage: "Inserts an InlineObject containing an image" — the object alone.
        //   insertPageBreak:   "Inserts a page break followed by a newline" — break + "\n".
        //   insertSectionBreak:"A newline character will be inserted before the section break" —
        //                      "\n" + break.
        //   insertTable:       "A newline character will be inserted before the inserted table",
        //                      then the structure — see TableLength.
        public const int InlineImageLength = 1;
        public const int PageBreakLength = 2;
        public const int SectionBreakLength = 2;

        // A table's structural footprint on top of the text its cells hold. Live-verified (dogfood
        // F5): the newline before the table (1) plus the table start marker (1), then every row
        // costs one row marker plus two indices per cell (cell marker + the cell's empty
        // paragraph), and the table end costs one more.
        public static int TableLength(int rows, int cols)
        {
            return rows > 0 && cols > 0 ? rows * (2 * cols + 1) + 3 : 0;
        }

        #endregion

        #region Placement sanity checks

        private static readonly string[] ContentInsertKeys =
            { "insertText", "insertInlineImage", "insertPageBreak", "insertSectionBreak", "insertTable" };

        // Number of content-inserting requests across the given request JSON strings.
        public static int CountContentInserts(IEnumerable<string> requestJson)
        {
            int n = 0;
            foreach (var req in SharedBuilders.FlattenRequests(requestJson))
                foreach (var key in ContentInsertKeys)
                    if (req[key] != null) { n++; break; }
            return n;
        }

        // Number of content inserts aimed at the first insertable index of their segment — 1 for
        // the body, 0 for a header/footer/footnote. A correctly placed run has at most one per
        // index space; more means blocks that bypassed Request Aggregator.
        public static int CountInsertsAtSegmentStart(string requestsArrayJson)
        {
            int n = 0;
            foreach (var req in SharedBuilders.FlattenRequests(new[] { requestsArrayJson }))
            {
                foreach (var key in ContentInsertKeys)
                {
                    var location = req[key]?["location"];
                    var idx = location?["index"];
                    if (idx == null || idx.Type != JTokenType.Integer) continue;
                    bool segment = !string.IsNullOrWhiteSpace(location.Value<string>("segmentId"));
                    if ((int)idx == (segment ? SegmentStartIndex : BodyStartIndex)) n++;
                    break;
                }
            }
            return n;
        }

        #endregion

        #region Append position

        // Where content starts in each index space. The body's index 0 is its section break, so
        // the first insertable position is 1; a header, footer or footnote segment has no section
        // break — a fresh one is a single empty paragraph at [0,1) — so its first position is 0.
        // Inserting at 1 in a fresh segment fails with "Index 1 must be less than the end index of
        // the referenced segment, 1".
        public const int BodyStartIndex = 1;
        public const int SegmentStartIndex = 0;
        // A new footnote segment "will contain a space followed by a newline character" — text
        // inserted at 1 lands after that space rather than in front of it.
        public const int FootnoteStartIndex = 1;

        // True when the blocks address a header/footer/footnote segment: any insert or range in
        // them carries a segmentId. (Mixing segment and body blocks in one run is unsupported —
        // their index spaces are independent — so the first hit decides.)
        public static bool TargetsSegment(IEnumerable<string> blocks)
        {
            if (blocks == null) return false;
            foreach (var block in blocks)
            {
                if (string.IsNullOrWhiteSpace(block)) continue;
                JArray reqs;
                try { reqs = SharedBuilders.FlattenRequests(new[] { block }); }
                catch { continue; }
                foreach (var req in reqs)
                    if (HasSegmentId(req)) return true;
            }
            return false;
        }

        // Every index space the blocks address: "" for the body, otherwise the segment ID. More
        // than one means the running content tally would be applied across independent index
        // spaces — the documented "one Request Aggregator per segment" rule — so the aggregator
        // refuses rather than emitting indices that are wrong for every block after the first.
        public static HashSet<string> IndexSpaces(IEnumerable<string> blocks)
        {
            var spaces = new HashSet<string>();
            if (blocks == null) return spaces;
            foreach (var block in blocks)
            {
                if (string.IsNullOrWhiteSpace(block)) continue;
                JArray reqs;
                try { reqs = SharedBuilders.FlattenRequests(new[] { block }); }
                catch { continue; }
                foreach (var req in reqs) CollectIndexSpaces(req, spaces);
            }
            return spaces;
        }

        // A "location" or "range" object names an index space; a request with neither (replaceAllText,
        // updateDocumentStyle, replaceImage) belongs to none.
        private static void CollectIndexSpaces(JToken token, HashSet<string> spaces)
        {
            if (token is JObject obj)
            {
                foreach (var prop in obj.Properties())
                {
                    if ((prop.Name == "location" || prop.Name == "range") && prop.Value is JObject where)
                        spaces.Add(where.Value<string>("segmentId") ?? "");
                    else
                        CollectIndexSpaces(prop.Value, spaces);
                }
            }
            else if (token is JArray arr)
            {
                foreach (var item in arr) CollectIndexSpaces(item, spaces);
            }
        }

        private static bool HasSegmentId(JToken token)
        {
            if (token is JObject obj)
            {
                foreach (var prop in obj.Properties())
                {
                    if (prop.Name == "segmentId" && prop.Value.Type == JTokenType.String
                        && !string.IsNullOrWhiteSpace((string)prop.Value)) return true;
                    if (HasSegmentId(prop.Value)) return true;
                }
            }
            else if (token is JArray arr)
            {
                foreach (var item in arr) if (HasSegmentId(item)) return true;
            }
            return false;
        }

        // The index to append at, read from a Get Document response. body.content is a list of
        // structural elements; the body always ends with a final paragraph break that nothing can
        // be inserted at or after (Docs rejects it), so the append position is the last element's
        // endIndex minus one. Returns 1 — the top of an empty body — when the JSON has no usable
        // body, so a fresh document still places correctly.
        public static int BodyEndIndex(string documentJson)
        {
            if (string.IsNullOrWhiteSpace(documentJson)) return 1;

            JArray content;
            try { content = JObject.Parse(documentJson)["body"]?["content"] as JArray; }
            catch { return 1; }

            if (content == null || content.Count == 0) return 1;

            int end = 0;
            foreach (var element in content)
            {
                var endIndex = element?["endIndex"];
                if (endIndex != null && endIndex.Type == JTokenType.Integer)
                    end = System.Math.Max(end, (int)endIndex);
            }

            return end > 1 ? end - 1 : 1;
        }

        #endregion

        #region Index sequencing (AppendCursor)

        // re-indexes a series of request blocks so they append in order from startIndex.
        // Each block is shifted by (startIndex - 1) plus the running content length of all prior blocks:
        // insertText contributes its text length, insertInlineImage/insertPageBreak/insertSectionBreak each
        // contribute a single placeholder character, insertTable contributes its structural size
        // (rows * (2 * columns + 1) + 3, on top of the cell text its fill requests carry), and
        // deleteContentRange contributes a negative shift equal to the size of the deleted range.
        public static string ReindexBlocks(IEnumerable<string> blocks, int startIndex)
        {
            return ReindexBlocks(blocks, startIndex, false);
        }

        // isolateStyles: follow every insertText with a reset of the inserted range (see
        // StyleResetRequests) so a block never inherits the paragraph style, bullets or text style
        // of whatever it was inserted into — the first paragraph of an earlier run, a heading the
        // Start Index points at, a bulleted line. The reset lands after the insert and before the
        // block's own style requests, so those still layer on top.
        public static string ReindexBlocks(IEnumerable<string> blocks, int startIndex, bool isolateStyles)
        {
            var outArr = new JArray();
            int baseShift = startIndex - 1;
            int running = 0;

            if (blocks != null)
            {
                foreach (var block in blocks)
                {
                    if (string.IsNullOrWhiteSpace(block)) continue;

                    JArray flat;
                    try { flat = SharedBuilders.FlattenRequests(new[] { block }); }
                    catch { continue; }

                    foreach (var reqs in SplitSelfIndexedBlocks(flat))
                    {
                        int shift = baseShift + running;
                        foreach (var req in reqs)
                        {
                            ShiftIndices(req, shift);
                            outArr.Add(req);
                            if (isolateStyles)
                                foreach (var reset in StyleResetRequests(req)) outArr.Add(reset);
                        }
                        running += ContentLength(reqs);
                    }
                }
            }

            return outArr.ToString(Formatting.None);
        }

        // Splits one flat request list back into the self-indexed blocks it was built from. Every
        // block authors its first insert at index 1 and its indices only grow from there, so an
        // insert landing at 1 after the current block has already inserted something can only be
        // the start of the next block. This is what lets several components arrive on ONE wire —
        // through a Merge, a Relay, or internalised data — and still be placed one after another
        // instead of all at the top of the document. An already-placed block (indices real, only
        // the first at 1) never re-inserts at 1, so it passes through as a single unit.
        public static List<JArray> SplitSelfIndexedBlocks(JArray flat)
        {
            var blocks = new List<JArray>();
            var current = new JArray();
            bool currentHasInsert = false;

            foreach (var req in flat)
            {
                int? insertAt = InsertIndex(req);
                if (insertAt == 1 && currentHasInsert)
                {
                    blocks.Add(current);
                    current = new JArray();
                    currentHasInsert = false;
                }
                current.Add(req);
                if (insertAt.HasValue) currentHasInsert = true;
            }

            if (current.Count > 0) blocks.Add(current);
            return blocks;
        }

        // The location index of a content-inserting request, or null for anything else (style,
        // bullets, delete, replace, ...).
        private static int? InsertIndex(JToken req)
        {
            foreach (var key in new[] { "insertText", "insertInlineImage", "insertPageBreak", "insertSectionBreak", "insertTable" })
            {
                var idx = req?[key]?["location"]?["index"];
                if (idx != null && idx.Type == JTokenType.Integer) return (int)idx;
            }
            return null;
        }

        // The requests that strip inherited formatting from one insertText's range. Google Docs gives
        // inserted text the paragraph style, bullets and text style of the position it lands at, so
        // a block inserted at the top of a document that already holds a centered H1 comes out as a
        // centered H1 too. Each field named in a mask but left unset in the style object reverts to
        // its default (the documented way to reset), so:
        //   - updateParagraphStyle: back to NORMAL_TEXT, with every paragraph property the plugin's
        //     components set (alignment, spacing, indents) cleared — only when the text carries a
        //     paragraph break, since a same-paragraph run must not restyle the paragraph it joins;
        //   - deleteParagraphBullets: same condition;
        //   - updateTextStyle with "*": every text property back to the named style's default.
        // Style requests contribute nothing to ContentLength, so nothing downstream moves.
        private static IEnumerable<JToken> StyleResetRequests(JToken req)
        {
            var insert = req?["insertText"];
            var location = insert?["location"];
            var indexTok = location?["index"];
            if (indexTok == null || indexTok.Type != JTokenType.Integer) yield break;

            string text = insert.Value<string>("text") ?? "";
            if (text.Length == 0) yield break;

            int start = (int)indexTok;
            int end = start + text.Length;
            string segmentId = location.Value<string>("segmentId");

            if (text.IndexOf('\n') >= 0)
            {
                yield return new JObject
                {
                    ["updateParagraphStyle"] = new JObject
                    {
                        ["range"] = SharedBuilders.Range(start, end, segmentId),
                        ["paragraphStyle"] = new JObject { ["namedStyleType"] = "NORMAL_TEXT" },
                        ["fields"] = "namedStyleType,alignment,lineSpacing,spaceAbove,spaceBelow,indentFirstLine,indentStart,indentEnd"
                    }
                };
                yield return new JObject
                {
                    ["deleteParagraphBullets"] = new JObject { ["range"] = SharedBuilders.Range(start, end, segmentId) }
                };
            }

            yield return new JObject
            {
                ["updateTextStyle"] = new JObject
                {
                    ["range"] = SharedBuilders.Range(start, end, segmentId),
                    ["textStyle"] = new JObject(),
                    ["fields"] = "*"
                }
            };
        }

        // recursively adds shift to every integer "index" / "startIndex" / "endIndex" property
        private static void ShiftIndices(JToken token, int shift)
        {
            if (token is JObject obj)
            {
                foreach (var prop in obj.Properties())
                {
                    if ((prop.Name == "index" || prop.Name == "startIndex" || prop.Name == "endIndex")
                        && prop.Value.Type == JTokenType.Integer)
                    {
                        prop.Value = (int)prop.Value + shift;
                    }
                    else
                    {
                        ShiftIndices(prop.Value, shift);
                    }
                }
            }
            else if (token is JArray arr)
            {
                foreach (var item in arr) ShiftIndices(item, shift);
            }
        }

        // net character-count change across a set of requests (drives cursor advance)
        private static int ContentLength(JArray reqs)
        {
            int len = 0;
            foreach (var r in reqs)
            {
                var insertText = r["insertText"];
                if (insertText != null)
                {
                    len += (insertText.Value<string>("text") ?? "").Length;
                    continue;
                }

                if (r["insertInlineImage"] != null) { len += InlineImageLength; continue; }
                if (r["insertPageBreak"] != null) { len += PageBreakLength; continue; }
                if (r["insertSectionBreak"] != null) { len += SectionBreakLength; continue; }

                // an inserted table's own structure, on top of whatever text its cell-fill
                // insertText requests add (those are counted above). Derived from the same live-
                // verified layout AecHelperBuilders uses: Docs adds a paragraph before the table
                // (1) plus the table start marker (1), then every row costs one row marker plus
                // two indices per cell (the cell marker and the cell's empty paragraph), and the
                // table end costs one more.
                var insertTable = r["insertTable"];
                if (insertTable != null)
                {
                    len += TableLength(insertTable["rows"]?.Value<int>() ?? 0, insertTable["columns"]?.Value<int>() ?? 0);
                    continue;
                }

                var deleteRange = r["deleteContentRange"]?["range"];
                if (deleteRange != null)
                {
                    len -= deleteRange.Value<int>("endIndex") - deleteRange.Value<int>("startIndex");
                    continue;
                }

                // createParagraphBullets reads each paragraph's nesting level from its leading tabs
                // and then REMOVES those tabs from the document, so a nested list ends up shorter than
                // the text that was inserted — by one character per tab. Without this, every block
                // placed after a nested Bullet List lands past the end of the segment.
                var bulletRange = r["createParagraphBullets"]?["range"];
                if (bulletRange != null)
                {
                    len -= LeadingTabsInRange(reqs, bulletRange.Value<int>("startIndex"), bulletRange.Value<int>("endIndex"));
                }
            }
            return len;
        }

        // Counts the leading tabs of every paragraph that starts inside [start, end), reading the
        // paragraphs back out of the block's own insertText requests: a paragraph starts at each
        // insert's index and after every "\n" inside its text. Indices here are whatever the block
        // currently carries (ReindexBlocks calls this after shifting), and the bullets range uses
        // the same frame, so they compare directly.
        private static int LeadingTabsInRange(JArray reqs, int start, int end)
        {
            int tabs = 0;
            foreach (var r in reqs)
            {
                var insert = r["insertText"];
                var indexTok = insert?["location"]?["index"];
                if (indexTok == null || indexTok.Type != JTokenType.Integer) continue;

                int index = (int)indexTok;
                string text = insert.Value<string>("text") ?? "";
                int paragraphStart = 0;
                while (paragraphStart < text.Length)
                {
                    int docIndex = index + paragraphStart;
                    int newline = text.IndexOf('\n', paragraphStart);
                    if (docIndex >= start && docIndex < end)
                    {
                        int k = paragraphStart;
                        while (k < text.Length && text[k] == '\t') { k++; tabs++; }
                    }
                    if (newline < 0) break;
                    paragraphStart = newline + 1;
                }
            }
            return tabs;
        }

        #endregion
    }
}
