using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using JetBrains.Annotations;
using Microsoft.AspNet.Identity;
using Veil.DataAccess.Interfaces;
using Veil.DataModels;
using Veil.DataModels.Models;
using Veil.Helpers;
using Veil.Models;

namespace Veil.Controllers
{
    public class ReviewsController : BaseController
    {
        private readonly IVeilDataAccess db;
        private readonly IGuidUserIdGetter idGetter;

        public ReviewsController(IVeilDataAccess veilDataAccess, IGuidUserIdGetter idGetter)
        {
            db = veilDataAccess;
            this.idGetter = idGetter;
        }

        [ChildActionOnly]
        public PartialViewResult CreateReviewForGame([NotNull]Game game)
        {
            if (!(HttpContext?.User?.Identity?.IsAuthenticated ?? false))
                return null;

            var memberId = idGetter.GetUserId(HttpContext.User.Identity);
            var previousReview = game.AllReviews.FirstOrDefault(r => r.MemberId == memberId);

            ReviewViewModel viewModel = new ReviewViewModel
            {
                GameId = game.Id,
                GameSKUSelectList = new SelectList(game.GameSKUs, "Id", "NamePlatformDistinct"),
                Review = previousReview
            };

            return PartialView(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateReviewForGameProduct([Bind] ReviewViewModel reviewViewModel)
        {
            var review = reviewViewModel.Review;

            review.MemberId = idGetter.GetUserId(HttpContext.User.Identity);

            var previousReview = await db.GameReviews.FindAsync(review.MemberId, review.ProductReviewedId);

            if (ModelState.IsValid)
            {
                //If there is review text, it's pending otherwise it's just approved.
                review.ReviewStatus = !string.IsNullOrWhiteSpace(review.ReviewText) ? ReviewStatus.Pending : ReviewStatus.Approved;

                if (previousReview == null)
                {
                    //It's a new review
                    db.GameReviews.Add(review);
                    await db.SaveChangesAsync();

                    this.AddAlert(AlertType.Success, "Your review has been saved.");
                }
                else
                {
                    //User is updating their review
                    previousReview.ReviewText = review.ReviewText;
                    previousReview.Rating = review.Rating;
                    previousReview.ReviewStatus = review.ReviewStatus;

                    db.MarkAsModified(previousReview);
                    await db.SaveChangesAsync();

                    this.AddAlert(AlertType.Success, "Your review has been updated.");
                }

                if (review.ReviewStatus == ReviewStatus.Pending)
                {
                    this.AddAlert(AlertType.Info, "Your rating will be visible immediately, but your comments will be visible pending review.");
                }
            }
            else
            {
                this.AddAlert(AlertType.Error, "There was an error saving your review. Please try again.");
            }

            return RedirectToAction("Details", "Games", new {id = reviewViewModel.GameId});
        }

        [ChildActionOnly]
        public PartialViewResult RenderReviewsForGame([NotNull] Game game)
        {
            return PartialView(game.AllReviews);
        }

        /// <summary>
        /// This method gets all of the reviews from GameReviews and passes the list into the Pending view.
        /// </summary>
        /// <returns>View for Pending reviews</returns>
        [HttpGet]
        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
        public async Task<ActionResult> Pending()
        {
            var pendingReviews = await db.GameReviews
                .Include(gr => gr.Member)
                .Where(gr => gr.ReviewStatus == ReviewStatus.Pending).ToListAsync();

            return View(pendingReviews);
        }

        /// <summary>
        /// Method to approve a review.
        /// </summary>
        /// <param name="memberId">The GUID of the member who made the review.</param>
        /// <param name="productReviewedId">The GUID of the product that was reviewed.</param>
        /// <returns>Redirect to Pending action to display reviews awaiting approval.</returns>
        [HttpGet]
        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
        public async Task<ActionResult> Approve(Guid memberId, Guid productReviewedId)
        {
            var review = await db.GameReviews
                .FirstOrDefaultAsync(gr => gr.MemberId == memberId
                                            && gr.ProductReviewedId == productReviewedId);

            if (review == null)
            {
                throw new HttpException(NotFound, nameof(GameReview));
            }

            review.ReviewStatus = ReviewStatus.Approved;

            await db.SaveChangesAsync();

            this.AddAlert(AlertType.Success, "Review approved!");

            return RedirectToAction("Pending");
        }

        /// <summary>
        /// Method to deny a review.
        /// </summary>
        /// <param name="memberId">The GUID of the member who made the review.</param>
        /// <param name="productReviewedId">The GUID of the product that was reviewed.</param>
        /// <returns>Redirect to Pending action to display reviews awaiting approval.</returns>
        [HttpGet]
        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
        public async Task<ActionResult> Deny(Guid memberId, Guid productReviewedId)
        {
            var review = await db.GameReviews
                .FirstOrDefaultAsync(gr => gr.MemberId == memberId
                                            && gr.ProductReviewedId == productReviewedId);

            if (review == null)
            {
                throw new HttpException(NotFound, nameof(GameReview));
            }

            review.ReviewStatus = ReviewStatus.Denied;

            await db.SaveChangesAsync();

            this.AddAlert(AlertType.Success, "Review denied!");

            return RedirectToAction("Pending");
        }
    }
}