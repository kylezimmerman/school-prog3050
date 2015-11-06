/* BaseController.cs
 * Purpose: Base class for all our controllers
 * 
 * Revision History:
 *      Drew Matheson, 2015.11.06: Created
 */ 

using System.Web;
using System.Web.Mvc;
using Veil.Helpers;

namespace Veil.Controllers
{
    /// <summary>
    ///     Base controller containing things common to all methods
    /// </summary>
    public class BaseController : Controller
    {
        protected override void OnException(ExceptionContext filterContext)
        {
            HttpException httpException = filterContext.Exception as HttpException;
            if (httpException != null)
            {
                switch (httpException.GetHttpCode())
                {
                    case 404:
                        filterContext.ExceptionHandled = true;
                        this.NotFoundErrorResult(httpException.Message).ExecuteResult(ControllerContext);
                        break;
                }
            }

            base.OnException(filterContext);
        }
    }
}