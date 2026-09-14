using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DocsConnect
{
    // Builds Google Docs batchUpdate structure requests (header/footer/footnote, doc style)
    public static class StructureRequestBuilders
    {
        #region Structure requests

        // createHeader
        public static string CreateHeader(string headerType)
        {
            var inner = new JObject { ["type"] = string.IsNullOrWhiteSpace(headerType) ? "DEFAULT" : headerType };
            return SharedBuilders.Wrap("createHeader", inner);
        }

        // createFooter
        public static string CreateFooter(string footerType)
        {
            var inner = new JObject { ["type"] = string.IsNullOrWhiteSpace(footerType) ? "DEFAULT" : footerType };
            return SharedBuilders.Wrap("createFooter", inner);
        }

        // createFootnote
        public static string CreateFootnote(int index)
        {
            var inner = new JObject { ["location"] = SharedBuilders.Location(index) };
            return SharedBuilders.Wrap("createFootnote", inner);
        }

        // updateDocumentStyle — only sets supplied fields and builds the matching fields mask
        public static string UpdateDocumentStyle(
            double? marginTop, double? marginBottom, double? marginLeft, double? marginRight,
            double? pageWidth, double? pageHeight, string background)
        {
            var style = new JObject();
            var fields = new List<string>();

            if (marginTop.HasValue && marginTop.Value > 0) { style["marginTop"] = SharedBuilders.Pt(marginTop.Value); fields.Add("marginTop"); }
            if (marginBottom.HasValue && marginBottom.Value > 0) { style["marginBottom"] = SharedBuilders.Pt(marginBottom.Value); fields.Add("marginBottom"); }
            if (marginLeft.HasValue && marginLeft.Value > 0) { style["marginLeft"] = SharedBuilders.Pt(marginLeft.Value); fields.Add("marginLeft"); }
            if (marginRight.HasValue && marginRight.Value > 0) { style["marginRight"] = SharedBuilders.Pt(marginRight.Value); fields.Add("marginRight"); }

            if ((pageWidth.HasValue && pageWidth.Value > 0) || (pageHeight.HasValue && pageHeight.Value > 0))
            {
                var pageSize = new JObject();
                if (pageWidth.HasValue && pageWidth.Value > 0) pageSize["width"] = SharedBuilders.Pt(pageWidth.Value);
                if (pageHeight.HasValue && pageHeight.Value > 0) pageSize["height"] = SharedBuilders.Pt(pageHeight.Value);
                style["pageSize"] = pageSize;
                fields.Add("pageSize");
            }

            if (SharedBuilders.ParseHexRgb(background) != null)
            {
                style["background"] = SharedBuilders.OptColor(background);
                fields.Add("background");
            }

            var inner = new JObject
            {
                ["documentStyle"] = style,
                ["fields"] = string.Join(",", fields)
            };
            return SharedBuilders.Wrap("updateDocumentStyle", inner);
        }

        #endregion
    }
}
