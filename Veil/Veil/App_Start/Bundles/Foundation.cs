using System.Web.Optimization;

namespace Veil
{
    public static class Foundation
    {
        public static Bundle Scripts()
        {
            return new ScriptBundle("~/bundles/foundation").Include(
                      "~/Scripts/Library/foundation/fastclick.js",
                      "~/Scripts/Library/jquery.cookie.js",
                      "~/Scripts/Library/foundation/foundation.js",
                      "~/Scripts/Library/foundation/foundation.*",
                      "~/Scripts/Library/foundation/app.js");
        }
    }
}