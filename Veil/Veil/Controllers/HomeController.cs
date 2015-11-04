using System.Linq;
using System.Web.Mvc;
using Veil.DataAccess.Interfaces;
using Veil.Models;

namespace Veil.Controllers
{
    public class HomeController : Controller
    {
        private readonly IVeilDataAccess db;

        public HomeController(IVeilDataAccess dataAccess)
        {
            db = dataAccess;
        }

        public ActionResult Index()
        {
            var games = db.Games.ToList();

            var model = new HomePageViewModel()
            {
                ComingSoon = games.Where(g => g.GameSKUs.Min(gp => gp.ReleaseDate) > System.DateTime.Now)
                    .OrderBy(g => g.GameSKUs.Min(gp => gp.ReleaseDate)).Take(2),
                NewReleases = games.Where(g => g.GameSKUs.Max(gp => gp.ReleaseDate) <= System.DateTime.Now)
                    .OrderByDescending(g => g.GameSKUs.Min(gp => gp.ReleaseDate)).Take(6)
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