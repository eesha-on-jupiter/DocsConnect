using System;
using System.Collections.Generic;
using System.Linq;

namespace DocsConnect
{
    public static class PluginUtilities
    {
        // Shared enum helper — used by any preset component backed by an enum
        public static IEnumerable<T> GetValues<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        internal static readonly string TabName = "DocsConnect";

        // Subcategory constants — sort key controls toolbar order
        internal static readonly string CategoryAAAuth               = "1. Connect";
        internal static readonly string CategoryABDocument           = "2. Document";
        internal static readonly string CategoryACRequestsText       = "3. Text";
        internal static readonly string CategoryADRequestsParagraph  = "4. Paragraph";
        internal static readonly string CategoryAERequestsInsert     = "5. Insert";
        internal static readonly string CategoryAFRequestsTable      = "6. Table";
        internal static readonly string CategoryAHRequestsAggregate  = "7. Combine";
        internal static readonly string CategoryZYPresets            = "8. Presets";
        internal static readonly string CategoryZZUtilities          = "9. Utilities";
    }
}
