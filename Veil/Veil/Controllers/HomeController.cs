/* HomeController.cs
 * Purpose: Controller for the home page
 * 
 * Revision History:
 *      Drew Matheson, 2015.09.25: Created
 */ 

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Models;

namespace Veil.Controllers
{
    /// <summary>
    ///     Controller for the home, about, and contact pages
    /// </summary>
    public class HomeController : BaseController
    {
        private const int NEW_RELEASE_COUNT = 6; // Note: This should always be a multiple of three
        private const int COMING_SOON_COUNT = 2; // Note: This should always be a multiple of two

        private readonly IVeilDataAccess db;

        /// <summary>
        ///     Instantiates a new instance of HomeController with the provided arguments
        /// </summary>
        /// <param name="veilDataAccess">
        ///     The <see cref="IVeilDataAccess"/> to use for database access
        /// </param>
        public HomeController(IVeilDataAccess veilDataAccess)
        {
            db = veilDataAccess;
        }

        /// <summary>
        /// Displays the Index page for Veil.
        /// </summary>
        /// <returns>The Index view with the processed HomePageViewModel.</returns>
        public async Task<ActionResult> Index()
        {
            List<Game> comingSoon =
                await db.Games.
                    Where(
                        g => g.GameSKUs.Any() &&
                            g.GameSKUs.Min(gp => gp.ReleaseDate) > DateTime.Now).
                    OrderBy(g => g.GameSKUs.Min(gp => gp.ReleaseDate)).
                    Take(COMING_SOON_COUNT).
                    ToListAsync();

            List<Game> newReleases =
                await db.Games.
                    Where(
                        g => g.GameSKUs.Any() &&
                            g.GameSKUs.Max(gp => gp.ReleaseDate) <= DateTime.Now).
                    OrderByDescending(g => g.GameSKUs.Min(gp => gp.ReleaseDate)).
                    Take(NEW_RELEASE_COUNT).
                    ToListAsync();

            var model = new HomePageViewModel
            {
                ComingSoon = comingSoon,
                NewReleases = newReleases
            };

            return View(model);
        }

        /// <summary>
        /// Displays the About page for Veil.
        /// </summary>
        /// <returns>The About view for the Home controller.</returns>
        public ActionResult About()
        {
            return View();
        }

        /// <summary>
        /// Displays the Contact page for Veil.
        /// </summary>
        /// <returns>The Contact view for the Home controller.</returns>
        public ActionResult Contact()
        {
            return View();
        }
    }
}