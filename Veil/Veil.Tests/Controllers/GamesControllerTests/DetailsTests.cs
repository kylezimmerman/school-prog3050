using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels;
using Veil.DataModels.Models;
using Veil.Models;

namespace Veil.Tests.Controllers.GamesControllerTests
{
    public class DetailsTests : GamesControllerTestsBase
    {
        [Test]
        public void Details_NullId_Throws404Exception()
        {
            GamesController controller = new GamesController(veilDataAccess: null, idGetter: null);

            Assert.That(async () => await controller.Details(null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }

        [Test]
        public void Details_IdNotInDb_Throws404Exception()
        {

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game>().AsQueryable());
            gameDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.Details(gameId), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }

        [Test]
        public async void Details_IdInDb_ReturnsViewWithModel()
        {
            Game matchingGame = new Game
            {
                Id = gameId,
                GameAvailabilityStatus = AvailabilityStatus.Available,
                GameSKUs = new List<GameProduct>()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { matchingGame }.AsQueryable());
            gameDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Details(matchingGame.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model != null);
            Assert.That(result.Model, Is.InstanceOf<Game>());
            Assert.That(result.Model, Is.EqualTo(matchingGame));
        }

        [Test]
        public async void Details_UserIsEmployee_ReturnsViewWithModel()
        {
            Game matchingGame = new Game
            {
                Id = gameId,
                GameAvailabilityStatus = AvailabilityStatus.Available
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { matchingGame }.AsQueryable());

            gameDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InEmployeeRole();

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Details(matchingGame.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.EqualTo(matchingGame));
        }

        [TestCase(AvailabilityStatus.PreOrder)]
        [TestCase(AvailabilityStatus.Available)]
        [TestCase(AvailabilityStatus.DiscontinuedByManufacturer)]
        public async void Details_StatusShouldBeVisibleToMember_ReturnsViewWithModel(AvailabilityStatus status)
        {
            Game matchingGame = new Game
            {
                Id = gameId,
                GameAvailabilityStatus = status,
                GameSKUs = new List<GameProduct>(),
                Rating = everyoneESRBRating
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { matchingGame }.AsQueryable());

            gameDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InMemberRole();

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Details(matchingGame.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.EqualTo(matchingGame));
        }

        [Test]
        public void Details_NotForSaleStatusShouldNotBeVisibleToMember_ReturnsErrorView()
        {
            Game matchingGame = new Game
            {
                Id = gameId,
                GameAvailabilityStatus = AvailabilityStatus.NotForSale,
                GameSKUs = new List<GameProduct>()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { matchingGame }.AsQueryable());

            gameDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InMemberRole();

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            Assert.That(async () => await controller.Details(matchingGame.Id), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }

        [Test]
        public void Details_NotForSaleStatusAndUserInNoRoles_ReturnsErrorView()
        {
            Game matchingGame = new Game
            {
                Id = gameId,
                GameAvailabilityStatus = AvailabilityStatus.NotForSale
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { matchingGame }.AsQueryable());

            gameDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InNoRoles();

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            Assert.That(async () => await controller.Details(matchingGame.Id), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }

        [TestCase(VeilRoles.MEMBER_ROLE)]
        [TestCase(null /* Stand-in for No Role */)]
        public async void Details_UserIsUnprivilegedRole_RatingMinimumAgeOfZero_ReturnsViewWithModelWithNoNotForSaleSKUs(string role)
        {
            Game matchingGame = new Game
            {
                Id = gameId,
                GameAvailabilityStatus = AvailabilityStatus.PreOrder,
                GameSKUs = GetGameSKUsListWithAllAvailabilityStatuses(),
                Rating = everyoneESRBRating
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { matchingGame }.AsQueryable());
            gameDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().IsInRole(role);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Details(matchingGame.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model != null);
            Assert.That(result.Model, Is.InstanceOf<Game>());

            var model = (Game)result.Model;

            Assert.That(model.GameSKUs, Has.All.Matches<Product>(p => p.ProductAvailabilityStatus != AvailabilityStatus.NotForSale));
        }

        [TestCase(VeilRoles.MEMBER_ROLE)]
        [TestCase(null /* Stand-in for No Role */)]
        public async void Details_UserIsUnprivilegedRole_GameWithRatingMinimumAgeGreaterThanZero_NoCookie_ReturnsAgeGateIndex(string role)
        {
            string rawUrl = "/Games/1234";

            Game matchingGame = new Game
            {
                Id = gameId,
                GameAvailabilityStatus = AvailabilityStatus.PreOrder,
                GameSKUs = GetGameSKUsListWithAllAvailabilityStatuses(),
                Rating = matureESRBRating,
                Name = "a game"
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { matchingGame }.AsQueryable());
            gameDbSetStub.SetupForInclude();

            dbStub.
                Setup(db => db.Games).
                Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.
                SetupUser().
                IsInRole(role);
            contextStub.
                Setup(c => c.HttpContext.Request.Cookies).
                Returns(new HttpCookieCollection());
            contextStub.
                Setup(c => c.HttpContext.Request.RawUrl).
                Returns(rawUrl);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Details(matchingGame.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.StringContaining("AgeGate").And.Contains("Index"));
            Assert.That(result.Model != null);
            Assert.That(result.Model, Is.InstanceOf<AgeGateViewModel>());

            var model = (AgeGateViewModel) result.Model;

            Assert.That(model.Name, Is.SameAs(matchingGame.Name));
            Assert.That(model.ReturnUrl, Is.SameAs(rawUrl));
        }

        [TestCase(VeilRoles.MEMBER_ROLE)]
        [TestCase(null /* Stand-in for No Role */)]
        public async void Details_UserIsUnprivilegedRole_GameWithRatingMinimumAgeGreaterThanZero_CookieWithAgeGreaterThanMinimumAge_ReturnViewsWithGame(string role)
        {
            Game matchingGame = new Game
            {
                Id = gameId,
                GameAvailabilityStatus = AvailabilityStatus.PreOrder,
                GameSKUs = GetGameSKUsListWithAllAvailabilityStatuses(),
                Rating = matureESRBRating,
                Name = "a game"
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { matchingGame }.AsQueryable());
            gameDbSetStub.SetupForInclude();

            dbStub.
                Setup(db => db.Games).
                Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.
                SetupUser().
                IsInRole(role);
            contextStub.
                Setup(c => c.HttpContext.Request.Cookies).
                Returns(new HttpCookieCollection
                {
                    new HttpCookie(AgeGateController.DATE_OF_BIRTH_COOKIE, DateTime.MinValue.ToShortDateString())
                });

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Details(matchingGame.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model != null);
            Assert.That(result.Model, Is.InstanceOf<Game>());
        }

        [TestCase(VeilRoles.MEMBER_ROLE)]
        [TestCase(null /* Stand-in for No Role */)]
        public async void Details_UserIsUnprivilegedRole_GameWithRatingMinimumAgeGreaterThanZero_CookieWithAgeLessThanMinimumAge_RedirectsToGamesIndex(string role)
        {
            Game matchingGame = new Game
            {
                Id = gameId,
                GameAvailabilityStatus = AvailabilityStatus.PreOrder,
                GameSKUs = GetGameSKUsListWithAllAvailabilityStatuses(),
                Rating = matureESRBRating,
                Name = "a game"
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { matchingGame }.AsQueryable());
            gameDbSetStub.SetupForInclude();

            dbStub.
                Setup(db => db.Games).
                Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.
                SetupUser().
                IsInRole(role);
            contextStub.
                Setup(c => c.HttpContext.Request.Cookies).
                Returns(new HttpCookieCollection
                {
                    new HttpCookie(AgeGateController.DATE_OF_BIRTH_COOKIE, DateTime.MaxValue.ToShortDateString())
                });

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Details(matchingGame.Id) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(HomeController.Index)));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Games"));
        }

        [TestCase(VeilRoles.EMPLOYEE_ROLE)]
        [TestCase(VeilRoles.ADMIN_ROLE)]
        public async void Details_UserIsPrivilegedRole_ReturnsViewWithModelContainingNotForSaleSKUs(string role)
        {
            Game matchingGame = new Game
            {
                Id = gameId,
                GameAvailabilityStatus = AvailabilityStatus.PreOrder,
                GameSKUs = GetGameSKUsListWithAllAvailabilityStatuses()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { matchingGame }.AsQueryable());
            gameDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().IsInRole(role);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Details(matchingGame.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model != null);
            Assert.That(result.Model, Is.InstanceOf<Game>());

            var model = (Game)result.Model;

            Assert.That(model.GameSKUs, Has.Some.Matches<Product>(p => p.ProductAvailabilityStatus == AvailabilityStatus.NotForSale));
        }

        [TestCase(VeilRoles.EMPLOYEE_ROLE)]
        [TestCase(VeilRoles.ADMIN_ROLE)]
        public async void Details_UserIsPrivilegedRoleWithMRatedGame_ReturnsViewWithModelContainingNotForSaleSKUs(string role)
        {
            Game matchingGame = new Game
            {
                Id = gameId,
                GameAvailabilityStatus = AvailabilityStatus.PreOrder,
                GameSKUs = GetGameSKUsListWithAllAvailabilityStatuses(),
                Rating = matureESRBRating
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { matchingGame }.AsQueryable());
            gameDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().IsInRole(role);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Details(matchingGame.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model != null);
            Assert.That(result.Model, Is.InstanceOf<Game>());

            var model = (Game)result.Model;

            Assert.That(model.GameSKUs, Has.Some.Matches<Product>(p => p.ProductAvailabilityStatus == AvailabilityStatus.NotForSale));
        }
    }
}
