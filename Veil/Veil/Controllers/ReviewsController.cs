/* ReviewsController.cs
 * Purpose: Controller for product reviews
 * 
 * Revision History:
 *      Kyle Zimmerman, 2015.11.13: Created
 */ 

using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using JetBrains.Annotations;
using Veil.DataAccess.Interfaces;
using Veil.DataModels;
using Veil.DataModels.Models;
using Veil.Helpers;
using Veil.Models;

namespace Veil.Controllers
{
    /// <summary>
    ///     Controller for actions related to <see cref="Review{T}"/>
    /// </summary>
    public class ReviewsController : BaseController
    {
        private readonly IVeilDataAccess db;
        private readonly IGuidUserIdGetter idGetter;

        /// <summary>
        ///     Instantiates a new instance of ReviewsController with the provided arguments
        /// </summary>
        /// <param name="veilDataAccess">
        ///     The <see cref="IVeilDataAccess"/> to use for database access
        /// </param>
        /// <param name="idGetter">
        ///     The <see cref="IGuidUserIdGetter"/> to use for getting the current user's Id
        /// </param>
        public ReviewsController(IVeilDataAccess veilDataAccess, IGuidUserIdGetter idGetter)
        {
            db = veilDataAccess;
            this.idGetter = idGetter;
        }

        /// <summary>
        ///     Renders a partial for allowing a member to review a game.
        ///     If they have already reviewed the game, they will be able to edit it.
        /// </summary>
        /// <param name="game">
        ///     The game to be reviewed
        /// </param>
        /// <returns>
        ///     Null if the user isn't authenticated.
        ///     Partial view for reviewing if the user is authenticated
        /// </returns>
        [ChildActionOnly]
        public PartialViewResult CreateReviewForGame([NotNull] Game game)
        {
            if (!(HttpContext.User?.Identity?.IsAuthenticated ?? false))
            {
                return null;
            }

            Guid memberId = idGetter.GetUserId(HttpContext.User.Identity);
            GameReview previousReview = game.AllReviews.FirstOrDefault(r => r.MemberId == memberId);

            ReviewViewModel viewModel = new ReviewViewModel
            {
                GameId = game.Id,
                GameSKUSelectList = new SelectList(game.GameSKUs, "Id", "NamePlatformDistinct"),
                Review = previousReview
            };

            return PartialView(viewModel);
        }

        /// <summary>
        ///     Creates a review using the information in the <see cref="reviewViewModel"/>
        /// </summary>
        /// <param name="reviewViewModel">
        ///     The <see cref="ReviewViewModel"/> containing the review information
        /// </param>
        /// <returns>
        ///     Redirection back to the reviewed game's details page
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateReviewForGameProduct([Bind] ReviewViewModel reviewViewModel)
        {
            var review = reviewViewModel.Review;

            if (!User.IsInRole(VeilRoles.MEMBER_ROLE))
            {
                this.AddAlert(AlertType.Error, "Only member's can review games.");
                return RedirectToAction("Details", "Games", new { id = reviewViewModel.GameId });
            }

            review.MemberId = idGetter.GetUserId(HttpContext.User.Identity);

            var previousReview = await db.GameReviews.FindAsync(review.MemberId, review.ProductReviewedId);

            if (ModelState.IsValid)
            {
                //If there is review text, it's pending otherwise it's just approved.
                review.ReviewStatus = !string.IsNullOrWhiteSpace(review.ReviewText)
                    ? ReviewStatus.Pending
                    : ReviewStatus.Approved;

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
                    this.AddAlert(
                        AlertType.Info,
                        "Your rating will be visible immediately, but your comments will be visible pending review.");
                }
            }
            else
            {
                this.AddAlert(AlertType.Error, "There was an error saving your review. Please try again.");
            }

            return RedirectToAction("Details", "Games", new { id = reviewViewModel.GameId });
        }

        /// <summary>
        ///     Displays a partial view for all of the reviews for the given game
        /// </summary>
        /// <param name="game">
        ///     The game to displays reviews for
        /// </param>
        /// <returns>
        ///     The partial view for the game's reviews
        /// </returns>
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
                .FirstOrDefaultAsync(
                    gr => gr.MemberId == memberId
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
                .FirstOrDefaultAsync(
                    gr => gr.MemberId == memberId
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