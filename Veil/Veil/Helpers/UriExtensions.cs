using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Veil.Helpers
{
    public static class UriExtensions
    {
        //This method only works on absolute URLs (which is fine for us) and was from http://stackoverflow.com/a/19679135/494356
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