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
        private Game game;
        private Guid memberId;
        private PhysicalGameProduct physicalGameProduct;

        private GameReview ratingOnlyReview;
        private GameReview fullReview;

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

            physicalGameProduct = new PhysicalGameProduct
            {
                Reviews = new List<GameReview>()
            };

            game = new Game()
            {
                GameSKUs = new List<GameProduct> {physicalGameProduct},
            };

            memberId = new Guid("DB6EF48B-839E-4A54-AFA2-B772738D01DB");

            ratingOnlyReview = new GameReview
            {
                ProductReviewed = physicalGameProduct,
                Rating = 5
            };

            fullReview = new GameReview
            {
                ProductReviewed = physicalGameProduct,
                Rating = 4,
                ReviewText = "Test"
            };
        }

        [Test]
        public void CreateReviewForGame_GET_Authenticated_ReturnsView()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUserAuthenticated(true);

            var idGetter = TestHelpers.GetSetupIUserIdGetterFake(memberId);

            ReviewsController controller = new ReviewsController(dbStub.Object, idGetter.Object)
            {
                ControllerContext = contextStub.Object,
            };

            var result = controller.CreateReviewForGame(game);

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<ReviewViewModel>());
        }

        [Test]
        public void CreateReviewForGame_GET_NotAuthenticated_ReturnsNull()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUserAuthenticated(false);

            var idGetter = TestHelpers.GetSetupIUserIdGetterFake(memberId);

            ReviewsController controller = new ReviewsController(dbStub.Object, idGetter.Object)
            {
                ControllerContext = contextStub.Object,
            };

            var result = controller.CreateReviewForGame(game);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void CreateReviewForGameProduct_POST_Invalid_SaveChangesNotCalled()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            dbStub.Setup(db => db.SaveChangesAsync()).ReturnsAsync(0).Verifiable();


            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUserAuthenticated(false);

            var idGetter = TestHelpers.GetSetupIUserIdGetterFake(memberId);

            ReviewsController controller = new ReviewsController(dbStub.Object, idGetter.Object)
            {
                ControllerContext = contextStub.Object,
            };
            controller.ModelState.AddModelError("GameId", "GameId is required");

            ReviewViewModel model = new ReviewViewModel()
            {
                GameId = game.Id,
                GameSKUSelectList = null,
                Review = fullReview
            };

            var result = controller.CreateReviewForGameProduct(model);

            Assert.That(result != null);
            Assert.That(() => dbStub.Verify(db => db.SaveChangesAsync(), Times.Never), Throws.Nothing);
        }

        [Test]
        public void CreateReviewForGameProduct_POST_ValidNew_AddCalledOnce()
        {
            Mock<DbSet<GameReview>> gameReviewsStub = TestHelpers.GetFakeAsyncDbSet(new List<GameReview>().AsQueryable());
            gameReviewsStub.Setup(rdb => rdb.FindAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(null);
            gameReviewsStub.Setup(rdb => rdb.Add(fullReview)).Returns(fullReview).Verifiable();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            dbStub.Setup(db => db.GameReviews).Returns(gameReviewsStub.Object);
            dbStub.Setup(db => db.SaveChangesAsync()).ReturnsAsync(0).Verifiable();

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUserAuthenticated(false);

            var idGetter = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            
            ReviewsController controller = new ReviewsController(dbStub.Object, idGetter.Object)
            {
                ControllerContext = contextStub.Object,
            };

            ReviewViewModel model = new ReviewViewModel()
            {
                GameId = game.Id,
                GameSKUSelectList = null,
                Review = fullReview
            };

            var result = controller.CreateReviewForGameProduct(model);

            Assert.That(result != null);
            Assert.That(() => gameReviewsStub.Verify(rdb => rdb.Add(fullReview), Times.Once), Throws.Nothing);
            Assert.That(() => dbStub.Verify(db => db.SaveChangesAsync(), Times.Once), Throws.Nothing);
        }

        [Test]
        public async void Pending_DisplaysPendingReviews_AllReviewsPending()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<GameReview>> gameReviewsDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameReview> { pendinGameReview, approGameReview, deniedGameReview }.AsQueryable());
            dbStub.Setup(db => db.GameReviews).Returns(gameReviewsDbSetStub.Object);
            gameReviewsDbSetStub.SetupForInclude();

            ReviewsController controller = new ReviewsController(dbStub.Object, idGetter: null);

            var result = await controller.Pending() as ViewResult;

            Assert.That(result.Model != null);

            var model = (List<GameReview>)result.Model;

            Assert.That(model, Has.All.Matches<GameReview>(gr => gr.ReviewStatus == ReviewStatus.Pending));
        }
        [Test]
        public void CreateReviewForGameProduct_ValidExisting_MarkAsChangedCalledOnce()
        {
            var existingReviews = new List<GameReview>
            {
                ratingOnlyReview
            };

            Mock<DbSet<GameReview>> gameReviewsStub = TestHelpers.GetFakeAsyncDbSet(existingReviews.AsQueryable());
            gameReviewsStub.Setup(rdb => rdb.FindAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(fullReview);
        
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            dbStub.Setup(db => db.GameReviews).Returns(gameReviewsStub.Object);
            dbStub.Setup(db => db.MarkAsModified(It.IsAny<GameReview>())).Verifiable();
            dbStub.Setup(db => db.SaveChangesAsync()).ReturnsAsync(0).Verifiable();

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUserAuthenticated(false);

            var idGetter = TestHelpers.GetSetupIUserIdGetterFake(memberId);

            ReviewsController controller = new ReviewsController(dbStub.Object, idGetter.Object)
            {
                ControllerContext = contextStub.Object,
            };

            ReviewViewModel model = new ReviewViewModel()
            {
                GameId = game.Id,
                GameSKUSelectList = null,
                Review = fullReview
            };

            var result = controller.CreateReviewForGameProduct(model);

            Assert.That(result != null);
            Assert.That(() => dbStub.Verify(db => db.MarkAsModified(fullReview), Times.Once), Throws.Nothing);
            Assert.That(() => dbStub.Verify(db => db.SaveChangesAsync(), Times.Once), Throws.Nothing);
        }


        [Test]
        public void RenderReviewsForGame_GET_ReturnsView()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUserAuthenticated(true);

            var idGetter = TestHelpers.GetSetupIUserIdGetterFake(memberId);

            ReviewsController controller = new ReviewsController(dbStub.Object, idGetter.Object)
            {
                ControllerContext = contextStub.Object,
            };

            var result = controller.RenderReviewsForGame(game);

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<IEnumerable<GameReview>>());
        }

        [Test]
        public async void Approve_EmployeeApprovesReview_ReviewStatusSetApproved()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<GameReview>> gameReviewsDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameReview> { pendinGameReview }.AsQueryable());
            dbStub.Setup(db => db.GameReviews).Returns(gameReviewsDbSetStub.Object);
            gameReviewsDbSetStub.SetupForInclude();

            ReviewsController controller = new ReviewsController(dbStub.Object, idGetter: null);

            await controller.Approve(memberGuid, gameProductGuid);

            Assert.That(pendinGameReview.ReviewStatus == ReviewStatus.Approved);
        }

        [Test]
        public void Approve_EmployeeApprovesNullReview_Throws404()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<GameReview>> gameReviewsDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameReview>().AsQueryable());
            dbStub.Setup(db => db.GameReviews).Returns(gameReviewsDbSetStub.Object);

            ReviewsController controller = new ReviewsController(dbStub.Object, idGetter: null);

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

            ReviewsController controller = new ReviewsController(dbStub.Object, idGetter: null);

            await controller.Deny(memberGuid, gameProductGuid);

            Assert.That(pendinGameReview.ReviewStatus == ReviewStatus.Denied);
        }

        [Test]
        public void Deny_EmployeeDeniesNullReview_Throws404()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<GameReview>> gameReviewsDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameReview>().AsQueryable());
            dbStub.Setup(db => db.GameReviews).Returns(gameReviewsDbSetStub.Object);

            ReviewsController controller = new ReviewsController(dbStub.Object, idGetter: null);

            Assert.That(
                async () => await controller.Deny(memberGuid, gameProductGuid),
                Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404)
            );
        }
    }
}
