using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using JetBrains.Annotations;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Models;

namespace Veil.Controllers
{
    public class ReviewsController : BaseController
    {
        private readonly IVeilDataAccess db;

        public ReviewsController(IVeilDataAccess veilDataAccess)
        {
            db = veilDataAccess;
        }

        [ChildActionOnly]
        public PartialViewResult ListRatingsForGame(Guid id)
        {
            return PartialView();
        }

        [ChildActionOnly]
        public PartialViewResult ListRatingsForGameProduct(Guid id)
        {
            return PartialView();
        }

        [ChildActionOnly]
        public async Task<PartialViewResult> CreateReviewForGame([NotNull]Game game)
        {
            ReviewViewModel viewModel = new ReviewViewModel
            {
                Game = game,
                GameSKUSelectList = new SelectList(game.GameSKUs, "Id", "Name")
            };
            return PartialView(viewModel);
        }

        [HttpPost]
        [ChildActionOnly]
        [ValidateAntiForgeryToken]
        public PartialViewResult CreateReviewForGameProduct(Guid id, [Bind] Review<GameProduct> review)
        {
            return PartialView("CreateReviewForGame");
        }
    }
}