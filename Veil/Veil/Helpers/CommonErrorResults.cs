/* CommonErrorResults.cs
 * Purpose:
 * 
 * Revision History:
 *      Drew Matheson, 2015.11.06: Created
 */ 

using System.Net;
using System.Web.Mvc;
using Veil.Models;

namespace Veil.Helpers
{
    /// <summary>
    ///     Contains methods for generating views for errors common to many controllers
    /// </summary>
    public static class CommonErrorResults
    {
        private const string NOT_FOUND_TITLE = " Not Found";

        /// <summary>
        /// Returns the error page with a item not found error message
        /// </summary>
        /// <param name="controller">The controller executing this result</param>
        /// <param name="modelName">User-friendly model name to display in the title and message</param>
        /// <returns>Error page for a item not found error</returns>
        public static ViewResult NotFoundErrorResult(this Controller controller, string modelName)
        {
            string message = $"The {modelName} you requested could not be found.";

            controller.AddAlert(AlertType.Error, message);
            controller.Response.StatusCode = (int) HttpStatusCode.NotFound;

            controller.ViewData.Model = new ErrorViewModel
            {
                Title = modelName + NOT_FOUND_TITLE,
                Message = message
            };

            var result = new ViewResult()
            {
                ViewName = "NotFound",
                ViewData = controller.ViewData,
                TempData = controller.TempData,
            };

            return result;
        }
    }
}