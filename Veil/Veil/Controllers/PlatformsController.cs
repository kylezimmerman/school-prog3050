using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Models;

namespace Veil.Controllers
{
    public class PlatformsController : BaseController
    {
        protected readonly IVeilDataAccess db;

        public PlatformsController(IVeilDataAccess veilDataAccess)
        {
            db = veilDataAccess;
        }

        [ChildActionOnly]
        public ActionResult Index(List<Platform> selected)
        {
            var platformsViewModel = new PlatformViewModel()
            {
                AllPlatforms = db.Platforms.ToList(),
                Selected = selected
            };

            return PartialView(platformsViewModel);
        }
    }
}