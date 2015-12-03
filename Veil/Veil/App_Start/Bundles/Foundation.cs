/* Foundation.cs
 * Purpose: Foundation bundle config
 * 
 * Revision History:
 *      Drew Matheson, 2015.09.25: Created
 */ 

using System.Web.Optimization;

namespace Veil
{
    /// <summary>
    ///     Foundation bundle config class
    /// </summary>
    public static class Foundation
    {
        /// <summary>
        ///     Creates a script bundle for scripts needed by Foundation
        /// </summary>
        /// <returns>
        ///     The created <see cref="Bundle"/>
        /// </returns>
        public static Bundle Scripts()
        {
            return new ScriptBundle("~/bundles/foundation").Include(
                "~/Scripts/Library/foundation/fastclick.js",
                "~/Scripts/Library/foundation/jquery.cookie.js",
                "~/Scripts/Library/foundation/foundation.js",
                "~/Scripts/Library/foundation/foundation.*",
                "~/Scripts/Library/foundation/app.js");
        }
    }
}