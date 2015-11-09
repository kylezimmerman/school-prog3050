using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Models;

namespace Veil.Controllers
{
    public class TagsController : BaseController
    {
        protected readonly IVeilDataAccess db;

        public TagsController(IVeilDataAccess veilDataAccess)
        {
            db = veilDataAccess;
        }

        [ChildActionOnly]
        public ActionResult Index(List<Tag> selected)
        {
            var tagsViewModel = new TagViewModel()
            {
                AllTags = db.Tags.ToList(),
                Selected = selected
            };

            return PartialView(tagsViewModel);
        }
    }
}