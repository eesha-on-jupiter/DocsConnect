using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DocsConnect
{
    // Shared JSON helpers for all Google Docs batchUpdate request builders
    public static class SharedBuilders
    {
        #region Text normalisation

        // Google Docs drops every "\r" from inserted text, so a Windows "\r\n" (a multi-line Panel,
        // a file read on Windows) lands two characters shorter than the plugin counted - every
        // index after that block is then off, and a style/bullet range can run past the end of the
        // segment ("Index N must be less than the end index of the referenced segment"). Every
        // text that reaches a builder goes through this first so the tally matches what Docs keeps.
        public static string NormalizeLineEndings(string text)
        {
            if (string.IsNullOrEmpty(text)) return text ?? "";
            return text.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        #endregion

        #region Private helpers

        // wraps a request inner object as { "<type>": { ... } } and serialises
        public static string Wrap(string type, JObject inner)
        {
            return new JObject { [type] = inner ?? new JObject() }.ToString(Formatting.None);
        }

        // a Docs Range { startIndex, endIndex[, segmentId] } — segmentId targets a header/footer/footnote
        // segment instead of the main body; leave null (default) for body content.
        public static JObject Range(int startIndex, int endIndex, string segmentId = null)
        {
            var range = new JObject { ["startIndex"] = startIndex, ["endIndex"] = endIndex };
            if (!string.IsNullOrWhiteSpace(segmentId)) range["segmentId"] = segmentId;
            return range;
        }

        // a Docs Location { index[, segmentId] } — segmentId targets a header/footer/footnote segment
        // instead of the main body; leave null (default) for body content.
        public static JObject Location(int index, string segmentId = null)
        {
            var loc = new JObject { ["index"] = index };
            if (!string.IsNullOrWhiteSpace(segmentId)) loc["segmentId"] = segmentId;
            return loc;
        }

        // a Docs Dimension { magnitude, unit: PT }
        public static JObject Pt(double magnitude)
        {
            return new JObject { ["magnitude"] = magnitude, ["unit"] = "PT" };
        }

        // an OptionalColor { color: { rgbColor: { red, green, blue } } } from a #RRGGBB hex
        public static JObject OptColor(string hex)
        {
            var rgb = ParseHexRgb(hex);
            if (rgb == null) return new JObject();
            return new JObject { ["color"] = new JObject { ["rgbColor"] = rgb } };
        }

        // parses a color as either #RRGGBB (or RRGGBB) hex, or comma-separated "R,G,B" / "R,G,B,A"
        // 0-255 integers (the format Grasshopper's native Colour Swatch produces when cast to text).
        // Returns an rgbColor with 0..1 float channels; null if unparseable in either form.
        public static JObject ParseHexRgb(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return null;
            string h = hex.Trim();

            if (h.Contains(","))
            {
                var parts = h.Split(',');
                if (parts.Length != 3 && parts.Length != 4) return null;
                if (!TryParseByte(parts[0], out int r)) return null;
                if (!TryParseByte(parts[1], out int g)) return null;
                if (!TryParseByte(parts[2], out int b)) return null;
                return new JObject { ["red"] = r / 255.0, ["green"] = g / 255.0, ["blue"] = b / 255.0 };
            }

            h = h.TrimStart('#');
            if (h.Length != 6) return null;
            if (!int.TryParse(h.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out int hr)) return null;
            if (!int.TryParse(h.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out int hg)) return null;
            if (!int.TryParse(h.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out int hb)) return null;
            return new JObject
            {
                ["red"] = hr / 255.0,
                ["green"] = hg / 255.0,
                ["blue"] = hb / 255.0
            };
        }

        // strict 0-255 integer parse for one comma-separated color channel
        private static bool TryParseByte(string s, out int value)
        {
            value = 0;
            if (!int.TryParse(s.Trim(), out value)) return false;
            return value >= 0 && value <= 255;
        }

        // a tableCellLocation { tableStartLocation: {index}, rowIndex, columnIndex }
        public static JObject TableCellLocation(int tableStartIndex, int rowIndex, int columnIndex)
        {
            return new JObject
            {
                ["tableStartLocation"] = Location(tableStartIndex),
                ["rowIndex"] = rowIndex,
                ["columnIndex"] = columnIndex
            };
        }

        #endregion

        #region Parse helpers

        // flattens a sequence of request JSON strings (each a single request object OR a JSON array
        // of request objects) into one JArray of request objects
        public static JArray FlattenRequests(IEnumerable<string> requestJson)
        {
            var arr = new JArray();
            if (requestJson == null) return arr;
            foreach (var s in requestJson)
            {
                if (string.IsNullOrWhiteSpace(s)) continue;
                JToken tok;
                try { tok = JToken.Parse(s); }
                catch { continue; }
                if (tok is JArray a) { foreach (var t in a) arr.Add(t); }
                else if (tok is JObject o) { arr.Add(o); }
            }
            return arr;
        }

        #endregion
    }
}
