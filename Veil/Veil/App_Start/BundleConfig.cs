/* BundleConfig.cs
 * Purpose: Bundling and minification config for Veil
 * 
 * Revision History:
 *      Drew Matheson, 2015.09.25: Created
 */ 

using System.Web.Optimization;

namespace Veil
{
    /// <summary>
    ///     Bundling and minification configuration class
    /// </summary>
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        /// <summary>
        ///     Registers the bundles used by Veil
        /// </summary>
        /// <param name="bundles">
        ///     The <see cref="BundleCollection"/> to register with
        /// </param>
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(
                new ScriptBundle("~/bundles/jquery").Include(
                    "~/Scripts/Library/jquery-{version}.js"));

            bundles.Add(
                new ScriptBundle("~/bundles/jqueryval").Include(
                    "~/Scripts/Library/jquery.validate*"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(
                new ScriptBundle("~/bundles/modernizr").Include(
                    "~/Scripts/Library/modernizr-*"));

            bundles.Add(
                new StyleBundle("~/Content/css").Include(
                    "~/Content/FoundationSite.css"));

            #region Foundation Bundles
            bundles.Add(Foundation.Scripts());
            #endregion
        }
    }
}