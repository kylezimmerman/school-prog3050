using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Models;

namespace Veil.Tests.Controllers
{
    [TestFixture]
    class ReviewsControllerTests
    {
        private Guid memberGuid;
        private Guid gameProductGuid;
        private GameReview pendinGameReview;
        private GameReview approGameReview;
        private GameReview deniedGameReview;

        [SetUp]
        public void Setup()
        {
            memberGuid = new Guid("2A384A50-E02A-43C9-AEB8-974DF0D32C38");
            gameProductGuid = new Guid("12803115-7E2B-4E34-A492-C388DD37E6AF");

            pendinGameReview = new GameReview()
            {
                MemberId = memberGuid,
                ProductReviewedId = gameProductGuid,
                ReviewStatus = ReviewStatus.Pending
            };

            approGameReview = new GameReview()
            {
                MemberId = memberGuid,
                ProductReviewedId = gameProductGuid,
                ReviewStatus = ReviewStatus.Approved
            };

            deniedGameReview = new GameReview()
            {
                MemberId = memberGuid,
                ProductReviewedId = gameProductGuid,
                ReviewStatus = ReviewStatus.Denied
            };
        }

        [Test]
        public async void Pending_DisplaysPendingReviews_AllReviewsPending()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<GameReview>> gameReviewsDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameReview> { pendinGameReview, approGameReview, deniedGameReview }.AsQueryable());
            dbStub.Setup(db => db.GameReviews).Returns(gameReviewsDbSetStub.Object);
            gameReviewsDbSetStub.SetupForInclude();

            ReviewsController controller = new ReviewsController(dbStub.Object);

            var result = await controller.Pending() as ViewResult;

            Assert.That(result.Model != null);

            var model = (List<GameReview>)result.Model;

            Assert.That(model, Has.All.Matches<GameReview>(gr => gr.ReviewStatus == ReviewStatus.Pending));
        }

        [Test]
        public async void Approve_EmployeeApprovesReview_ReviewStatusSetApproved()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<GameReview>> gameReviewsDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameReview> { pendinGameReview }.AsQueryable());
            dbStub.Setup(db => db.GameReviews).Returns(gameReviewsDbSetStub.Object);
            gameReviewsDbSetStub.SetupForInclude();

            ReviewsController controller = new ReviewsController(dbStub.Object);

            await controller.Approve(memberGuid, gameProductGuid);

            Assert.That(pendinGameReview.ReviewStatus == ReviewStatus.Approved);
        }

        [Test]
        public void Approve_EmployeeApprovesNullReview_Throws404()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<GameReview>> gameReviewsDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameReview>().AsQueryable());
            dbStub.Setup(db => db.GameReviews).Returns(gameReviewsDbSetStub.Object);

            ReviewsController controller = new ReviewsController(dbStub.Object);

            Assert.That(
                async () => await controller.Approve(memberGuid, gameProductGuid),
                Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404)
            );
        }

        [Test]
        public async void Deny_EmployeeDeniesReview_ReviewStatusSetDenied()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<GameReview>> gameReviewsDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameReview> { pendinGameReview }.AsQueryable());
            dbStub.Setup(db => db.GameReviews).Returns(gameReviewsDbSetStub.Object);
            gameReviewsDbSetStub.SetupForInclude();

            ReviewsController controller = new ReviewsController(dbStub.Object);

            await controller.Deny(memberGuid, gameProductGuid);

            Assert.That(pendinGameReview.ReviewStatus == ReviewStatus.Denied);
        }

        [Test]
        public void Deny_EmployeeDeniesNullReview_Throws404()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<GameReview>> gameReviewsDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameReview>().AsQueryable());
            dbStub.Setup(db => db.GameReviews).Returns(gameReviewsDbSetStub.Object);

            ReviewsController controller = new ReviewsController(dbStub.Object);

            Assert.That(
                async () => await controller.Deny(memberGuid, gameProductGuid),
                Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404)
            );
        }
    }
}
