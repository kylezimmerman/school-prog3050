/* BaseController.cs
 * Purpose: Base class for all our controllers
 * 
 * Revision History:
 *      Drew Matheson, 2015.11.06: Created
 */

using System.IO;
using System.Net;
using System.Web;
using System.Web.Mvc;
using JetBrains.Annotations;
using Veil.Helpers;

namespace Veil.Controllers
{
    /// <summary>
    ///     Base controller containing things common to all methods
    /// </summary>
    public class BaseController : Controller
    {
        protected const int NotFound = (int) HttpStatusCode.NotFound;

        protected override void OnException(ExceptionContext filterContext)
        {
            HttpException httpException = filterContext.Exception as HttpException;
            if (httpException != null)
            {
                switch (httpException.GetHttpCode())
                {
                    case NotFound:
                        filterContext.ExceptionHandled = true;
                        this.NotFoundErrorResult(httpException.Message).ExecuteResult(ControllerContext);
                        break;
                }
            }

            base.OnException(filterContext);
        }

        /// <summary>
        ///     Renders the full-path viewName with the specified object and returns it as a string
        /// </summary>
        /// <param name="viewName">
        ///     The full path to including extension for the view to render
        /// </param>
        /// <param name="model">
        ///     The model to use for the view
        /// </param>
        /// <returns>
        ///     The rendered view as a string
        /// </returns>
        protected string RenderRazorPartialViewToString([NotNull]string viewName, object model)
        {
            ViewData.Model = model;

            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);
                return sw.GetStringBuilder().ToString();
            }
        }
    }
}