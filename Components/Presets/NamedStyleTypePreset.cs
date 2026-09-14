using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace DocsConnect.Presets
{
    // Provides Google Docs named paragraph style values.
    public class NamedStyleTypePresetComponent : GH_ValueList
    {
        public NamedStyleTypePresetComponent()
        {
            Category = PluginUtilities.TabName;
            SubCategory = PluginUtilities.CategoryZYPresets;
            Name = "Named Style";
            NickName = "NS";
            Description = "Named paragraph style values for Update Paragraph Style.";

            ListItems.Clear();
            foreach (var kvp in Items)
                ListItems.Add(new GH_ValueListItem(kvp.Key, $"\"{kvp.Value}\""));
        }

        private static readonly Dictionary<string, string> Items = new Dictionary<string, string>
        {
            { "Normal Text", "NORMAL_TEXT" },
            { "Title",       "TITLE"       },
            { "Subtitle",    "SUBTITLE"    },
            { "Heading 1",   "HEADING_1"   },
            { "Heading 2",   "HEADING_2"   },
            { "Heading 3",   "HEADING_3"   },
            { "Heading 4",   "HEADING_4"   },
            { "Heading 5",   "HEADING_5"   },
            { "Heading 6",   "HEADING_6"   },
        };

        protected override Bitmap Icon => Properties.Resources.GDC_NamedStyleTypePreset;
        public override Guid ComponentGuid => new Guid("4d834055-f0be-4b9a-b339-5a4f2800e424");
    }
}
