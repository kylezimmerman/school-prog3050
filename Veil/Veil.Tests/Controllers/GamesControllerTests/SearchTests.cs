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

namespace Veil.Tests.Controllers.GamesControllerTests
{
    public class SearchTests : GamesControllerTestsBase
    {
        [Test]
        public void Search_EmptyKeyword_DoesNotFilterByName()
        {
            List<Game> games = new List<Game>
            {
                new Game
                {
                    Name = null
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            // Calling contains on the null Name would throw
            Assert.That(async () => await controller.Search(), Throws.Nothing);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public async void Search_PageLessThan1_SetsPageTo1(int currentPage)
        {
            List<Game> games = GetGameSearchList();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Search(keyword: "", page: currentPage) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.CurrentPage, Is.EqualTo(1));
        }

        [TestCase(VeilRoles.MEMBER_ROLE)]
        [TestCase(null /* Stand-in for No Role */)]
        public async void Search_EmptyString_UnprivilegedRole_ReturnsFullListExceptNotForSale(string role)
        {
            List<Game> games = GetGameSearchList();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().IsInRole(role);

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Search() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.Games, Has.Count.EqualTo(3));
            Assert.That(model.Games, Has.None.Matches<Game>(g => g.GameAvailabilityStatus == AvailabilityStatus.NotForSale));
        }

        [TestCase(VeilRoles.EMPLOYEE_ROLE)]
        [TestCase(VeilRoles.ADMIN_ROLE)]
        public async void Search_EmptyString_PrivilegedRole_ReturnsFullList(string role)
        {
            List<Game> games = GetGameSearchList();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().IsInRole(role);

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Search() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.Games, Has.Count.EqualTo(4));
            Assert.That(model.Games, Has.Some.Matches<Game>(g => g.GameAvailabilityStatus == AvailabilityStatus.NotForSale));
        }

        [Test]
        public async void Search_DoesNotMatchAnyTitle_ReturnsEmpty()
        {
            List<Game> games = GetGameSearchList();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Search("not a match for anything") as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.Games, Is.Empty);
        }

        [Test]
        public async void Search_KeywordFullMatchOfOneTitle_ReturnsIEnumerableOfMatchingGame()
        {
            List<Game> games = GetGameSearchList();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Search(games[0].Name) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.Games, Has.Count.EqualTo(1));
            Assert.That(model.Games, Contains.Item(games[0]));
        }

        [Test]
        public async void Search_KeywordIsPartOfTitle_ReturnsIEnumerableOfMatchingGames()
        {
            List<Game> games = GetGameSearchList();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Search(TITLE_FRAGMENT_COMMON_TO_ALL_SEARCH_GAMES) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.Games, Has.Count.EqualTo(games.Count));
        }

        [TestCase(VeilRoles.MEMBER_ROLE)]
        [TestCase(null /* Stand-in for No Role */)]
        public async void Search_KeywordIsPartOfNotForSaleTitle_Member_DoesNotReturnThatGame(string role)
        {
            List<Game> games = GetGameSearchList();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().IsInRole(role);

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Search("NotForSale") as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.Games, Has.Count.EqualTo(0));
        }

        [TestCase(VeilRoles.EMPLOYEE_ROLE)]
        [TestCase(VeilRoles.ADMIN_ROLE)]
        public async void Search_KeywordIsPartOfNotForSaleTitle_Employee_ReturnsThatGame(string role)
        {
            List<Game> games = GetGameSearchList();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().IsInRole(role);

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Search("NotForSale") as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.Games, Has.Count.EqualTo(1));
        }

        [Test]
        public async void Search_EmptyString_OrdersResultByName()
        {
            List<Game> games = GetGameSearchList();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Search() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(games, Is.Not.Ordered.By(nameof(Game.Name)));
            Assert.That(model.Games, Is.Ordered.By(nameof(Game.Name)));
        }

        [Test]
        public async void Search_NonDefaultPage_SetsCurrentPageToPassedPage()
        {
            List<Game> games = GetGameSearchList();
            int currentPage = 2;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Search(keyword: "", page: currentPage) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.CurrentPage, Is.EqualTo(currentPage));
        }

        [Test]
        public async void Search_5Games_SetsTotalPagesToListCountDividedByGamesPerPage()
        {
            List<Game> games = new List<Game>
            {
                new Game(),
                new Game(),
                new Game(),
                new Game(),
                new Game()
            };

            int gamesPerPage = 1;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object,
                GamesPerPage = gamesPerPage
            };

            var result = await controller.Search() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.TotalPages, Is.EqualTo(5 /* 5 games, one per page */));
        }

        [Test]
        public async void Search_2GamesPerPage_ReturnsGamesListWith2Games()
        {
            List<Game> games = new List<Game>
            {
                new Game(),
                new Game(),
                new Game(),
                new Game(),
                new Game()
            };

            int gamesPerPage = 2;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object,
                GamesPerPage = gamesPerPage
            };

            var result = await controller.Search() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.Games, Has.Count.EqualTo(gamesPerPage));
        }

        [Test]
        public async void Search_PageGreaterThanPagesSupportedByGameCount_ReturnsEmptyGameList()
        {
            List<Game> games = new List<Game>
            {
                new Game(),
                new Game(),
                new Game(),
                new Game(),
                new Game()
            };

            int gamesPerPage = games.Count;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object,
                GamesPerPage = gamesPerPage
            };

            var result = await controller.Search(keyword: "", page: 2) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.Games, Is.Empty);
        }
    }
}
