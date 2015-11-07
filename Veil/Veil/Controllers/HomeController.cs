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
    public class HomeController : BaseController
    {
        private const int NEW_RELEASE_COUNT = 6; // Note: This should always be a multiple of three

        private readonly IVeilDataAccess db;

        public HomeController(IVeilDataAccess dataAccess)
        {
            db = dataAccess;
        }

        public async Task<ActionResult> Index()
        {
            List<Game> comingSoon =
                await db.Games.
                    Where(
                        g => g.GameSKUs.Any() &&
                            g.GameSKUs.Min(gp => gp.ReleaseDate) > DateTime.Now).
                    OrderBy(g => g.GameSKUs.Min(gp => gp.ReleaseDate)).
                    Take(2).
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

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}