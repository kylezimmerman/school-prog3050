/* FilterConfig.cs
 * Purpose: Filter configuration for Veil
 * 
 * Revision History:
 *      Drew Matheson, 2015.12.03: Created
 */ 

using System.Web.Mvc;

namespace Veil
{
    /// <summary>
    ///     Filter configuration class
    /// </summary>
    public class FilterConfig
    {
        /// <summary>
        ///     Registers all the filters used by Veil
        /// </summary>
        /// <param name="filters">
        ///     The <see cref="GlobalFilterCollection"/> to register with
        /// </param>
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}