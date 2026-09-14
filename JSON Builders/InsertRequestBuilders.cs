using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DocsConnect
{
    // Builds Google Docs batchUpdate insert requests (image, table, breaks)
    public static class InsertRequestBuilders
    {
        #region Insert requests

        // insertInlineImage — objectSize is only included when a width or height is supplied
        public static string InsertInlineImage(int index, string imageUrl, double? width, double? height)
        {
            var inner = new JObject
            {
                ["location"] = SharedBuilders.Location(index),
                ["uri"] = imageUrl ?? ""
            };

            JObject size = new JObject();
            if (width.HasValue && width.Value > 0) size["width"] = SharedBuilders.Pt(width.Value);
            if (height.HasValue && height.Value > 0) size["height"] = SharedBuilders.Pt(height.Value);
            if (size.HasValues) inner["objectSize"] = size;

            return SharedBuilders.Wrap("insertInlineImage", inner);
        }

        // one insertInlineImage request per URL, self-indexed from placeholder 1 — each image occupies
        // DocumentBuilders.InlineImageLength indices, so sequential images land
        // back-to-back. width/height are index-matched to imageUrls — a shorter list repeats its last
        // value (an empty list leaves that dimension unset for every image). Self-indexed from 1 —
        // place it via Request Aggregator.
        public static string[] InsertInlineImagesSequential(IList<string> imageUrls, IList<double?> widths, IList<double?> heights)
        {
            return InsertInlineImagesSequential(imageUrls, widths, heights, false);
        }

        // ownParagraph: follow each image with a paragraph break so a list of images stacks one
        // per line instead of sitting side by side in one paragraph. The break is an insertText
        // "\n", which the aggregator's tally already counts.
        public static string[] InsertInlineImagesSequential(IList<string> imageUrls, IList<double?> widths, IList<double?> heights, bool ownParagraph)
        {
            int n = imageUrls?.Count ?? 0;
            var reqs = new List<string>();
            int cursor = 1;
            for (int i = 0; i < n; i++)
            {
                double? width = TextRequestBuilders.GetOrLast(widths, i, (double?)null);
                double? height = TextRequestBuilders.GetOrLast(heights, i, (double?)null);
                reqs.Add(InsertInlineImage(cursor, imageUrls[i], width, height));
                cursor += DocumentBuilders.InlineImageLength;
                if (ownParagraph)
                {
                    reqs.Add(TextRequestBuilders.InsertText("\n", cursor));
                    cursor += 1;
                }
            }
            return reqs.ToArray();
        }

        // replaceImage
        public static string ReplaceImage(string imageObjectId, string imageUrl)
        {
            var inner = new JObject
            {
                ["imageObjectId"] = imageObjectId ?? "",
                ["uri"] = imageUrl ?? "",
                ["imageReplaceMethod"] = "CENTER_CROP"
            };
            return SharedBuilders.Wrap("replaceImage", inner);
        }

        // one replaceImage request per object ID/URL pair. imageUrls is index-matched to imageObjectIds
        // — a shorter list repeats its last value.
        public static string[] ReplaceImages(IList<string> imageObjectIds, IList<string> imageUrls)
        {
            int n = imageObjectIds?.Count ?? 0;
            var reqs = new string[n];
            for (int i = 0; i < n; i++)
            {
                string url = TextRequestBuilders.GetOrLast(imageUrls, i, "");
                reqs[i] = ReplaceImage(imageObjectIds[i], url);
            }
            return reqs;
        }

        // insertTable — used internally by AecHelperBuilders.BuildTable (Data Tree To Table / Fixed
        // Size Table), which computes its own cell-offset math. There is no standalone Insert Table
        // component: Google doesn't publish an index-delta formula for table structural growth, so a
        // bare insertTable request can't be reliably auto-sequenced the way image/break requests can.
        public static string InsertTable(int index, int rows, int columns)
        {
            var inner = new JObject
            {
                ["location"] = SharedBuilders.Location(index),
                ["rows"] = rows < 1 ? 1 : rows,
                ["columns"] = columns < 1 ? 1 : columns
            };
            return SharedBuilders.Wrap("insertTable", inner);
        }

        // insertPageBreak
        public static string InsertPageBreak(int index)
        {
            var inner = new JObject { ["location"] = SharedBuilders.Location(index) };
            return SharedBuilders.Wrap("insertPageBreak", inner);
        }

        // `count` insertPageBreak requests, self-indexed from placeholder 1 — each break occupies a
        // DocumentBuilders.PageBreakLength indices (break + newline), so sequential breaks land back-to-back. Self-indexed from 1 —
        // place it via Request Aggregator.
        public static string[] InsertPageBreaksSequential(int count)
        {
            var reqs = new string[count];
            int cursor = 1;
            for (int i = 0; i < count; i++)
            {
                reqs[i] = InsertPageBreak(cursor);
                cursor += DocumentBuilders.PageBreakLength;
            }
            return reqs;
        }

        // insertSectionBreak
        public static string InsertSectionBreak(int index, string sectionType)
        {
            var inner = new JObject
            {
                ["location"] = SharedBuilders.Location(index),
                ["sectionType"] = string.IsNullOrWhiteSpace(sectionType) ? "NEXT_PAGE" : sectionType
            };
            return SharedBuilders.Wrap("insertSectionBreak", inner);
        }

        // one insertSectionBreak request per Section Type entry, self-indexed from placeholder 1 —
        // each break occupies DocumentBuilders.SectionBreakLength indices (newline + break), so sequential breaks land back-to-back.
        // Self-indexed from 1 — place it via Request Aggregator.
        public static string[] InsertSectionBreaksSequential(IList<string> sectionTypes)
        {
            int n = sectionTypes?.Count ?? 0;
            var reqs = new string[n];
            int cursor = 1;
            for (int i = 0; i < n; i++)
            {
                reqs[i] = InsertSectionBreak(cursor, sectionTypes[i]);
                cursor += DocumentBuilders.SectionBreakLength;
            }
            return reqs;
        }

        #endregion
    }
}
