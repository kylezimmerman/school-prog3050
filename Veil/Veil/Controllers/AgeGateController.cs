/* AgeGateController.cs
 * Purpose: Controller used to age-gate content
 * 
 * Revision History:
 *      Drew Matheson, 2015.12.04: Created
 */ 

using System;
using System.Web;
using System.Web.Mvc;
using Veil.Models;

namespace Veil.Controllers
{
    /// <summary>
    ///     Provides actions and methods related to age-gating content
    /// </summary>
    public class AgeGateController : BaseController
    {
        /// <summary>
        ///     Message which should be displayed if content is blocked due to the age-gate
        /// </summary>
        public const string AgeBlockMessage =
            "Sorry, but you're not permitted to view these materials at this time.";

        /// <summary>
        ///     The key for the date of birth cookie
        /// </summary>
        public const string DATE_OF_BIRTH_COOKIE = "DoB";

        /// <summary>
        ///     Postback method which creates a new date of birth cookie that lasts a day
        ///     using date of birth from <see cref="model"/>
        /// </summary>
        /// <param name="model">
        ///     <see cref="AgeGateViewModel"/> containing the information
        /// </param>
        /// <returns>
        ///     Redirections to the return URL if it is local, or Home Index if not.
        ///     Redisplays the page if any of the values were invalid
        /// </returns>
        [HttpPost]
        public ActionResult Index(AgeGateViewModel model)
        {
            if (DateTime.DaysInMonth(model.Year, model.Month) < model.Day)
            {
                ModelState.AddModelError(nameof(model.Day), "Invalid");
                return View(model);
            }

            if (DateTime.MinValue.Year > model.Year || DateTime.MaxValue.Year < model.Year)
            {
                ModelState.AddModelError(nameof(model.Year), "Invalid");
                return View(model);
            }

            if (DateTime.MinValue.Month > model.Month || DateTime.MaxValue.Month < model.Month)
            {
                ModelState.AddModelError(nameof(model.Month), "Invalid");
                return View(model);
            }

            DateTime dateOfBirth = new DateTime(model.Year, model.Month, model.Day);

            HttpCookie cookie = new HttpCookie(DATE_OF_BIRTH_COOKIE, dateOfBirth.ToShortDateString())
            {
                Expires = DateTime.Now.AddDays(1)
            };

            Response.Cookies.Add(cookie);

            return RedirectToLocal(model.ReturnUrl);
        }

        /// <summary>
        ///     Attempts to get the date of birth value from the date of birth cookie
        /// </summary>
        /// <param name="cookies">
        ///     The <see cref="HttpCookieCollection"/> to get the cookie from
        /// </param>
        /// <returns>
        ///     The date of birth value if the cookie existed and was valid.
        ///     Null otherwise
        /// </returns>
        public static DateTime? GetDateOfBirthValue(HttpCookieCollection cookies)
        {
            HttpCookie cookie = cookies[DATE_OF_BIRTH_COOKIE];

            if (cookie == null)
            {
                return null;
            }

            DateTime userAge;
            bool parseSuccess = DateTime.TryParse(cookie.Value, out userAge);

            return parseSuccess
                ? (DateTime?) userAge
                : null;
        }
    }
}