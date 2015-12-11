using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels;
using Veil.DataModels.Models;
using Veil.Models;
using Veil.Tests.Controllers.GameProductsControllerTests;

namespace Veil.Tests.Controllers.GamesControllerTests
{
    public class IndexTests : GameProductsControllerTestsBase
    {
        [TestCase(null)] //Unauthenticated
        [TestCase(VeilRoles.MEMBER_ROLE)]
        public async void Index_Unprivileged_NotForSaleGame(string role)
        {
            List<Game> games = new List<Game>
            {
                new Game { GameAvailabilityStatus = AvailabilityStatus.NotForSale },
                new Game { GameAvailabilityStatus = AvailabilityStatus.Available }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().IsInRole(role);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object,
            };

            var result = await controller.Index() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.Games, Has.Count.EqualTo(1));
            Assert.That(model.Games.FirstOrDefault(), Is.EqualTo(games[1]));
        }

        [TestCase(VeilRoles.EMPLOYEE_ROLE)]
        [TestCase(VeilRoles.ADMIN_ROLE)]
        public async void Index_Privileged_NotForSaleGame(string role)
        {
            List<Game> games = new List<Game>
            {
                new Game { GameAvailabilityStatus = AvailabilityStatus.NotForSale },
                new Game { GameAvailabilityStatus = AvailabilityStatus.Available }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().IsInRole(role);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Index() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.Games, Has.Count.EqualTo(2));
            Assert.That(model.Games, Is.EqualTo(games));
        }

        [Test]
        public async void Index_Pagination_NegativePage_SetsTo1()
        {
            List<Game> games = new List<Game>
            {
                new Game()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object,
            };

            var result = await controller.Index(-1) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.CurrentPage, Is.EqualTo(1));
        }

        [Test]
        public async void Index_NoGames()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game>().AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InNoRoles();

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object,
                GamesPerPage = 1
            };

            var result = await controller.Index(2) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.Games, Is.Empty);
        }

        [Test]
        public async void Index_Pagination_ValidPages([Range(1, 3)] int page)
        {
            List<Game> games = new List<Game>
            {
                new Game(),
                new Game(),
                new Game(),
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object,
                GamesPerPage = 1
            };

            var result = await controller.Index(page) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.Games, Has.Count.EqualTo(1));
            Assert.That(model.Games, Has.Member(games[page - 1]));
        }

        [Test]
        public async void Index_Pagination_PagePastMaxPages_EmptyResult()
        {
            List<Game> games = new List<Game>
            {
                new Game(),
                new Game(),
                new Game(),
                new Game()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object,
                GamesPerPage = 1
            };

            var result = await controller.Index(5) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.Games, Is.Empty);
        }
    }
}
