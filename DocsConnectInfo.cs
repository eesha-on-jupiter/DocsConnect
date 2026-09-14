using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;

namespace DocsConnect
{
    // assembly metadata shown in the Grasshopper ribbon and Yak package
    public class DocsConnectInfo : GH_AssemblyInfo
    {
        public override string Name => "DocsConnect";

        public override Bitmap Icon => null;

        public override string Description =>
            "Grasshopper components for the Google Docs API — create, read, and batch-update Google Docs from Rhino/Grasshopper. Made with the PleaseREST Claude Code plugin.";

        public override Guid Id => new Guid("d2559914-baca-40bd-99e6-7022e6376ea5");

        public override string AuthorName => "Eesha Jain";

        public override string AuthorContact => "toeesha@gmail.com";

        public override string AssemblyVersion => "1.0.0";
    }

    // sets the ribbon category icon and priority at load time
    public class DocsConnectPriority : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            Instances.ComponentServer.AddCategoryIcon(PluginUtilities.TabName, Properties.Resources.GDC_DocsConnectLogo);
            return GH_LoadingInstruction.Proceed;
        }
    }
}
