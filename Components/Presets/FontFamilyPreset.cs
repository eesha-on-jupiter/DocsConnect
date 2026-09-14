using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace DocsConnect.Presets
{
    // Curated shortcut list of common font family names. Unlike the other presets, Font Family is
    // NOT a closed API enum — any installed font name string is valid, so this is a convenience
    // list, not validation; Font Family inputs remain plain free-text and can still be typed directly.
    public class FontFamilyPresetComponent : GH_ValueList
    {
        public FontFamilyPresetComponent()
        {
            Category = PluginUtilities.TabName;
            SubCategory = PluginUtilities.CategoryZYPresets;
            Name = "Font Family";
            NickName = "FF";
            Description = "Curated shortcut list of common font families for Styled Text / Font Family Text.";

            ListItems.Clear();
            foreach (var kvp in Items)
                ListItems.Add(new GH_ValueListItem(kvp.Key, $"\"{kvp.Value}\""));
        }

        private static readonly Dictionary<string, string> Items = new Dictionary<string, string>
        {
            { "Arial",           "Arial"           },
            { "Calibri",         "Calibri"         },
            { "Times New Roman", "Times New Roman" },
            { "Georgia",         "Georgia"         },
            { "Verdana",         "Verdana"         },
            { "Courier New",     "Courier New"     },
            { "Garamond",        "Garamond"        },
            { "Trebuchet MS",    "Trebuchet MS"    },
            { "Roboto",          "Roboto"          },
            { "Open Sans",       "Open Sans"       },
        };

        protected override Bitmap Icon => Properties.Resources.GDC_FontFamilyPreset;
        public override Guid ComponentGuid => new Guid("4dff2988-0cf2-4adc-a691-801d1f43a0f1");
    }
}
