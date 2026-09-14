using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DocsConnect
{
    // Builds Google Docs batchUpdate text requests
    public static class TextRequestBuilders
    {
        #region Text requests

        // insertText — segmentId targets a header/footer/footnote segment instead of the body
        public static string InsertText(string text, int index, string segmentId = null)
        {
            var inner = new JObject
            {
                ["location"] = SharedBuilders.Location(index, segmentId),
                ["text"] = SharedBuilders.NormalizeLineEndings(text)
            };
            return SharedBuilders.Wrap("insertText", inner);
        }

        // one insertText request per entry in texts, chained sequentially with a paragraph break ("\n")
        // after every line so each line lands in its own paragraph. Self-indexed from 1 — place via
        // Request Aggregator. segmentId targets a header/footer/footnote segment instead of the body.
        public static string[] InsertTextLines(IList<string> texts, string segmentId = null)
        {
            int n = texts?.Count ?? 0;
            var reqs = new string[n];
            int cursor = 1;
            for (int i = 0; i < n; i++)
            {
                string line = (texts[i] ?? "") + "\n";
                reqs[i] = InsertText(line, cursor, segmentId);
                cursor += line.Length;
            }
            return reqs;
        }

        // replaceAllText
        public static string ReplaceAllText(string find, string replace, bool matchCase)
        {
            var inner = new JObject
            {
                ["containsText"] = new JObject
                {
                    ["text"] = find ?? "",
                    ["matchCase"] = matchCase
                },
                ["replaceText"] = replace ?? ""
            };
            return SharedBuilders.Wrap("replaceAllText", inner);
        }

        // one replaceAllText request per find/replace pair. Find and Replace are index-matched — a
        // shorter Replace list repeats its last value. matchCase applies uniformly to every pair.
        public static string[] ReplaceAllTextPairs(IList<string> finds, IList<string> replaces, bool matchCase)
        {
            int n = finds?.Count ?? 0;
            var reqs = new string[n];
            for (int i = 0; i < n; i++)
            {
                string replace = GetOrLast(replaces, i, "");
                reqs[i] = ReplaceAllText(finds[i], replace, matchCase);
            }
            return reqs;
        }

        // deleteContentRange
        public static string DeleteContentRange(int startIndex, int endIndex)
        {
            var inner = new JObject { ["range"] = SharedBuilders.Range(startIndex, endIndex) };
            return SharedBuilders.Wrap("deleteContentRange", inner);
        }

        // updateTextStyle — only sets the fields that were supplied (non-null), and builds the matching fields mask.
        // segmentId targets a header/footer/footnote segment instead of the body.
        public static string UpdateTextStyle(
            int startIndex, int endIndex,
            bool? bold, bool? italic, bool? underline, bool? strikethrough,
            double? fontSize, string fontFamily, string textColor, string highlight,
            string link, string baseline, string segmentId = null)
        {
            var style = new JObject();
            var fields = new List<string>();

            if (bold.HasValue) { style["bold"] = bold.Value; fields.Add("bold"); }
            if (italic.HasValue) { style["italic"] = italic.Value; fields.Add("italic"); }
            if (underline.HasValue) { style["underline"] = underline.Value; fields.Add("underline"); }
            if (strikethrough.HasValue) { style["strikethrough"] = strikethrough.Value; fields.Add("strikethrough"); }
            if (fontSize.HasValue && fontSize.Value > 0) { style["fontSize"] = SharedBuilders.Pt(fontSize.Value); fields.Add("fontSize"); }
            if (!string.IsNullOrWhiteSpace(fontFamily)) { style["weightedFontFamily"] = new JObject { ["fontFamily"] = fontFamily }; fields.Add("weightedFontFamily"); }
            // an unparseable color is skipped rather than sent as an empty OptionalColor — Google Docs
            // treats an empty OptionalColor as "reset to default," which would silently no-op the request
            // instead of erroring, masking the bad input.
            if (SharedBuilders.ParseHexRgb(textColor) != null) { style["foregroundColor"] = SharedBuilders.OptColor(textColor); fields.Add("foregroundColor"); }
            if (SharedBuilders.ParseHexRgb(highlight) != null) { style["backgroundColor"] = SharedBuilders.OptColor(highlight); fields.Add("backgroundColor"); }
            if (!string.IsNullOrWhiteSpace(link)) { style["link"] = new JObject { ["url"] = link }; fields.Add("link"); }
            if (!string.IsNullOrWhiteSpace(baseline)) { style["baselineOffset"] = baseline; fields.Add("baselineOffset"); }

            var inner = new JObject
            {
                ["range"] = SharedBuilders.Range(startIndex, endIndex, segmentId),
                ["textStyle"] = style,
                ["fields"] = string.Join(",", fields)
            };
            return SharedBuilders.Wrap("updateTextStyle", inner);
        }

        // insertText + updateTextStyle for each entry in texts, chained sequentially with a paragraph
        // break ("\n") after every line so each line lands in its own paragraph. fontSizes/fontFamilies/
        // textColors/highlights/links/baselines are index-matched to texts — a shorter list repeats its
        // last value for the remaining lines, and an empty (or null) list leaves that trait unset for
        // every line. bold/italic/underline/strikethrough apply uniformly to every line (they have no
        // per-line value of their own). Self-indexed from 1 — place via Request Aggregator. segmentId
        // targets a header/footer/footnote segment instead of the body.
        public static string[] InsertStyledTextLines(
            IList<string> texts,
            bool? bold, bool? italic, bool? underline, bool? strikethrough,
            IList<double?> fontSizes, IList<string> fontFamilies, IList<string> textColors,
            IList<string> highlights, IList<string> links, IList<string> baselines,
            string segmentId = null)
        {
            var reqs = new List<string>();
            int cursor = 1;
            int n = texts?.Count ?? 0;

            for (int i = 0; i < n; i++)
            {
                string line = (texts[i] ?? "") + "\n";
                double? fontSize = GetOrLast(fontSizes, i, (double?)null);
                string fontFamily = GetOrLast(fontFamilies, i, "");
                string textColor = GetOrLast(textColors, i, "");
                string highlight = GetOrLast(highlights, i, "");
                string link = GetOrLast(links, i, "");
                string baseline = GetOrLast(baselines, i, "");

                int start = cursor;
                int end = start + line.Length;

                reqs.Add(InsertText(line, start, segmentId));
                reqs.Add(UpdateTextStyle(start, end, bold, italic, underline, strikethrough,
                    fontSize, fontFamily, textColor, highlight, link, baseline, segmentId));

                cursor = end;
            }

            return reqs.ToArray();
        }

        // one insertText for the whole joined run, plus one updateTextStyle per segment — segments land
        // in a SINGLE paragraph (joined by separator, no line break between them), each independently
        // styled. This is the per-character/per-word counterpart to InsertStyledTextLines (which puts
        // each item in its own paragraph): wire a Sine/Graph Mapper/Gradient sampled per segment into
        // fontSizes/textColors/etc. to build a wave or gradient effect across one word or sentence.
        // fontSizes/fontFamilies/textColors/highlights/links/baselines are index-matched to segments —
        // a shorter list repeats its last value, and an empty (or null) list leaves that trait unset.
        // bold/italic/underline/strikethrough apply uniformly to the whole run. Self-indexed from 1 —
        // place via Request Aggregator. segmentId targets a header/footer/footnote segment instead of the body.
        public static string[] InsertStyledRun(
            IList<string> segments, string separator,
            bool? bold, bool? italic, bool? underline, bool? strikethrough,
            IList<double?> fontSizes, IList<string> fontFamilies, IList<string> textColors,
            IList<string> highlights, IList<string> links, IList<string> baselines,
            bool newParagraphAfter, string segmentId = null)
        {
            int n = segments?.Count ?? 0;
            if (n == 0) return new string[0];

            var sb = new System.Text.StringBuilder();
            var spans = new (int start, int end)[n];
            for (int i = 0; i < n; i++)
            {
                if (i > 0 && !string.IsNullOrEmpty(separator)) sb.Append(separator);
                int segStart = sb.Length;
                sb.Append(segments[i] ?? "");
                spans[i] = (segStart, sb.Length);
            }
            if (newParagraphAfter) sb.Append("\n");

            const int start = 1;
            var reqs = new List<string> { InsertText(sb.ToString(), start, segmentId) };

            for (int i = 0; i < n; i++)
            {
                if (spans[i].end == spans[i].start) continue; // nothing to style on an empty segment

                double? fontSize = GetOrLast(fontSizes, i, (double?)null);
                string fontFamily = GetOrLast(fontFamilies, i, "");
                string textColor = GetOrLast(textColors, i, "");
                string highlight = GetOrLast(highlights, i, "");
                string link = GetOrLast(links, i, "");
                string baseline = GetOrLast(baselines, i, "");

                reqs.Add(UpdateTextStyle(start + spans[i].start, start + spans[i].end,
                    bold, italic, underline, strikethrough,
                    fontSize, fontFamily, textColor, highlight, link, baseline, segmentId));
            }

            return reqs.ToArray();
        }

        // index-matched list lookup: out-of-range repeats the last entry; a null/empty list yields fallback.
        internal static T GetOrLast<T>(IList<T> list, int i, T fallback)
        {
            if (list == null || list.Count == 0) return fallback;
            return list[Math.Min(i, list.Count - 1)];
        }

        #endregion
    }
}
