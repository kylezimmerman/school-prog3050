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
    public class AdvancedSearchTests : GamesControllerTestsBase
    {
        [Test]
        public async void AdvancedSearch_EmptySearchParameters_ReturnsAdvancedSearchView()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(GetGameSearchList().AsQueryable());
            Mock<DbSet<Tag>> tagDbSetStub = TestHelpers.GetFakeAsyncDbSet(GetTagList().AsQueryable());
            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(GetPlatformList().AsQueryable());

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);
            dbStub.Setup(db => db.Tags).Returns(tagDbSetStub.Object);
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            // Calling contains on the null Name would throw
            var result = await controller.AdvancedSearch(null) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.Empty);
        }

        [Test]
        public async void AdvancedSearch_EmptySearchParameters_ReturnsViewModelWithAllPlatforms()
        {
            List<Platform> platforms = GetPlatformList();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(platforms.AsQueryable());

            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            // Calling contains on the null Name would throw
            var result = await controller.AdvancedSearch(null) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<AdvancedSearchViewModel>());

            var model = (AdvancedSearchViewModel)result.Model;

            Assert.That(model.Platforms, Has.Count.EqualTo(platforms.Count));
        }

        [Test]
        public void AdvancedSearch_EmptyTitleKeyword_DoesNotFilterByName()
        {
            List<Game> games = new List<Game>
            {
                new Game
                {
                    Name = null,
                    GameSKUs = GetGameSKUsListWithAllAvailabilityStatuses()
                }
            };

            List<Platform> platforms = GetPlatformList();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(platforms.AsQueryable());

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            // Calling contains on the null Name would throw
            Assert.That(async () => await controller.AdvancedSearch(null, platform: platforms.First().PlatformCode), Throws.Nothing);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public async void AdvancedSearch_PageLessThan1_SetsPageTo1(int currentPage)
        {
            List<Game> games = GetGameSearchList();
            List<Platform> platforms = GetPlatformList();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(platforms.AsQueryable());

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.AdvancedSearch(null, platform: platforms.First().PlatformCode, page: currentPage) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.CurrentPage, Is.EqualTo(1));
        }

        [Test]
        public async void AdvancedSearch_DoesNotMatchAnyTitle_ReturnsEmpty()
        {
            List<Game> games = GetGameSearchList();
            List<Platform> platforms = GetPlatformList();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(platforms.AsQueryable());

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.AdvancedSearch(null, title: "not a match for anything") as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.Games, Is.Empty);
        }

        [Test]
        public async void AdvancedSearch_KeywordFullMatchOfOneTitle_ReturnsIEnumerableOfMatchingGame()
        {
            List<Game> games = GetGameSearchList();
            List<Platform> platforms = GetPlatformList();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(platforms.AsQueryable());

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.AdvancedSearch(null, title: games[0].Name) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.Games, Has.Count.EqualTo(1));
            Assert.That(model.Games, Contains.Item(games[0]));
        }

        [Test]
        public async void AdvancedSearch_KeywordIsPartOfTitle_ReturnsIEnumerableOfMatchingGames()
        {
            List<Game> games = GetGameSearchList();
            List<Platform> platforms = GetPlatformList();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(platforms.AsQueryable());

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.AdvancedSearch(null, title: TITLE_FRAGMENT_COMMON_TO_ALL_SEARCH_GAMES) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.Games, Has.Count.EqualTo(games.Count));
        }

        [TestCase(VeilRoles.MEMBER_ROLE)]
        [TestCase(null /* Stand-in for No Role */)]
        public async void AdvancedSearch_KeywordIsPartOfNotForSaleTitle_Member_DoesNotReturnThatGame(string role)
        {
            List<Game> games = GetGameSearchList();
            List<Platform> platforms = GetPlatformList();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(platforms.AsQueryable());

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().IsInRole(role);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.AdvancedSearch(null, title: "NotForSale") as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.Games, Has.Count.EqualTo(0));
        }

        [TestCase(VeilRoles.EMPLOYEE_ROLE)]
        [TestCase(VeilRoles.ADMIN_ROLE)]
        public async void AdvancedSearch_KeywordIsPartOfNotForSaleTitle_Employee_ReturnsThatGame(string role)
        {
            List<Game> games = GetGameSearchList();
            List<Platform> platforms = GetPlatformList();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(platforms.AsQueryable());

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().IsInRole(role);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.AdvancedSearch(null, title: "NotForSale") as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.Games, Has.Count.EqualTo(1));
        }

        [Test]
        public async void AdvancedSearch_TitleMatchingAll_OrdersResultByName()
        {
            List<Game> games = GetGameSearchList();
            List<Platform> platforms = GetPlatformList();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(platforms.AsQueryable());

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.AdvancedSearch(null, title: TITLE_FRAGMENT_COMMON_TO_ALL_SEARCH_GAMES) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(games, Is.Not.Ordered.By(nameof(Game.Name)), "Games must not be ordered for this test to be valid");
            Assert.That(model.Games, Is.Ordered.By(nameof(Game.Name)));
        }

        [Test]
        public async void AdvancedSearch_NonDefaultPage_SetsCurrentPageToPassedPage()
        {
            List<Game> games = GetGameSearchList();
            List<Platform> platforms = GetPlatformList();

            int currentPage = 2;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(platforms.AsQueryable());

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.AdvancedSearch(null, title: TITLE_FRAGMENT_COMMON_TO_ALL_SEARCH_GAMES, page: currentPage) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.CurrentPage, Is.EqualTo(currentPage));
        }

        [Test]
        public async void AdvancedSearch_4Games_SetsTotalPagesToListCountDividedByGamesPerPage()
        {
            List<Game> games = GetGameSearchList().GetRange(0, 4);
            List<Platform> platforms = GetPlatformList();

            int gamesPerPage = 1;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(platforms.AsQueryable());

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object,
                GamesPerPage = gamesPerPage
            };

            var result = await controller.AdvancedSearch(null, title: TITLE_FRAGMENT_COMMON_TO_ALL_SEARCH_GAMES) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.TotalPages, Is.EqualTo(4 /* 4 games, one per page */));
        }

        [Test]
        public async void AdvancedSearch_2GamesPerPage_ReturnsGamesListWith2Games()
        {
            List<Game> games = GetGameSearchList();
            List<Platform> platforms = GetPlatformList();

            int gamesPerPage = 2;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(platforms.AsQueryable());

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object,
                GamesPerPage = gamesPerPage
            };

            var result = await controller.AdvancedSearch(null, title: TITLE_FRAGMENT_COMMON_TO_ALL_SEARCH_GAMES) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.Games, Has.Count.EqualTo(gamesPerPage));
        }

        [Test]
        public async void AdvancedSearch_PageGreaterThanPagesSupportedByGameCount_ReturnsEmptyGameList()
        {
            List<Game> games = GetGameSearchList();
            List<Platform> platforms = GetPlatformList();

            int gamesPerPage = games.Count;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(platforms.AsQueryable());

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object,
                GamesPerPage = gamesPerPage
            };

            var result = await controller.AdvancedSearch(null, title: TITLE_FRAGMENT_COMMON_TO_ALL_SEARCH_GAMES, page: 2) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.Games, Is.Empty);
        }

        [Test]
        public async void AdvancedSearch_DoesNotMatchAnyTag_ReturnsEmpty()
        {
            List<Game> games = GetGameSearchList();
            List<Platform> platforms = GetPlatformList();
            List<Tag> tags = GetTagList();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(platforms.AsQueryable());
            Mock<DbSet<Tag>> tagDbSetStub = TestHelpers.GetFakeAsyncDbSet(tags.AsQueryable());

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);
            dbStub.Setup(db => db.Tags).Returns(tagDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.AdvancedSearch(new List<string> { "not a match to any tag" }) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.Games, Is.Empty);
        }
    }
}
