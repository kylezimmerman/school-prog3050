/* UriExtensions.cs
 * Purpose: Extension methods for Uri
 * 
 * Revision History:
 *      Kyle Zimmerman, 2015.11.06: Created
 */

using System;
using System.Web;

namespace Veil.Extensions
{
    /// <summary>
    ///     Extension methods for <see cref="Uri"/>
    /// </summary>
    public static class UriExtensions
    {
        //This method only works on absolute URLs (which is fine for us) and was from http://stackoverflow.com/a/19679135/494356
        /// <summary>
        ///     Creates a new Uri with a query string parameter added
        ///     Note: Only works on absolute URLs
        /// </summary>
        /// <param name="url">
        ///     The original <see cref="Uri"/>
        /// </param>
        /// <param name="paramName">
        ///     The query string parameter name
        /// </param>
        /// <param name="paramValue">
        ///     The query string parameter value
        /// </param>
        /// <returns>
        ///     A new <see cref="Uri"/> with the parameter added
        /// </returns>
        public static Uri AddParameter(this Uri url, string paramName, string paramValue)
        {
            var uriBuilder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query[paramName] = paramValue;
            uriBuilder.Query = query.ToString();

            return new Uri(uriBuilder.ToString());
        }
    }
}