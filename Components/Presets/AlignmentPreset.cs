using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace DocsConnect.Presets
{
    // Provides Google Docs paragraph alignment values.
    public class AlignmentPresetComponent : GH_ValueList
    {
        public AlignmentPresetComponent()
        {
            Category = PluginUtilities.TabName;
            SubCategory = PluginUtilities.CategoryZYPresets;
            Name = "Alignment";
            NickName = "AL";
            Description = "Paragraph alignment values for Update Paragraph Style.";

            ListItems.Clear();
            foreach (var kvp in Items)
                ListItems.Add(new GH_ValueListItem(kvp.Key, $"\"{kvp.Value}\""));
        }

        private static readonly Dictionary<string, string> Items = new Dictionary<string, string>
        {
            { "Start (Left)", "START"     },
            { "Center",       "CENTER"    },
            { "End (Right)",  "END"       },
            { "Justified",    "JUSTIFIED" },
        };

        protected override Bitmap Icon => Properties.Resources.GDC_AlignmentPreset;
        public override Guid ComponentGuid => new Guid("e72f1121-ff9d-4e34-9ab4-afa5caa713d3");
    }
}
