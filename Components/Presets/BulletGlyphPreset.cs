using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace DocsConnect.Presets
{
    // Provides Google Docs bullet preset values, including checkbox and numbered lists.
    public class BulletGlyphPresetComponent : GH_ValueList
    {
        public BulletGlyphPresetComponent()
        {
            Category = PluginUtilities.TabName;
            SubCategory = PluginUtilities.CategoryZYPresets;
            Name = "Bullet Preset";
            NickName = "BP";
            Description = "Bullet preset values for Create Bullets.";

            ListItems.Clear();
            foreach (var kvp in Items)
                ListItems.Add(new GH_ValueListItem(kvp.Key, $"\"{kvp.Value}\""));
        }

        private static readonly Dictionary<string, string> Items = new Dictionary<string, string>
        {
            { "Disc / Circle / Square",   "BULLET_DISC_CIRCLE_SQUARE"        },
            { "Diamond / Arrow / Square", "BULLET_DIAMONDX_ARROW3D_SQUARE"   },
            { "Checkbox",                 "BULLET_CHECKBOX"                  },
            { "Arrow / Diamond / Disc",   "BULLET_ARROW_DIAMOND_DISC"        },
            { "Star / Circle / Square",   "BULLET_STAR_CIRCLE_SQUARE"        },
            { "Numbered (1, a, i)",       "NUMBERED_DECIMAL_ALPHA_ROMAN"     },
            { "Numbered (nested)",        "NUMBERED_DECIMAL_NESTED"          },
            { "Numbered (A, a, i)",       "NUMBERED_UPPERALPHA_ALPHA_ROMAN"  },
        };

        protected override Bitmap Icon => Properties.Resources.GDC_BulletGlyphPreset;
        public override Guid ComponentGuid => new Guid("1a2ea68a-b494-4038-973e-3b2dfd0a0475");
    }
}
