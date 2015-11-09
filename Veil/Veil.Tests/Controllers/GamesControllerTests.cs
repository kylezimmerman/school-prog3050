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

namespace Veil.Tests.Controllers
{
    [TestFixture]
    public class GamesControllerTests
    {
        private PhysicalGameProduct notForSaleSKU;
        private PhysicalGameProduct availableSKU;
        private PhysicalGameProduct preOrderSKU;
        private PhysicalGameProduct discontinuedSKU;

        private Guid Id;

        private const string TITLE_FRAGMENT_COMMON_TO_ALL_SEARCH_GAMES = "atch";

        [SetUp]
        public void Setup()
        {
            notForSaleSKU = new PhysicalGameProduct
            {
                ProductAvailabilityStatus = AvailabilityStatus.NotForSale,
            };

            availableSKU = new PhysicalGameProduct
            {
                ProductAvailabilityStatus = AvailabilityStatus.Available,
            };

            preOrderSKU = new PhysicalGameProduct
            {
                ProductAvailabilityStatus = AvailabilityStatus.PreOrder
            };

            discontinuedSKU = new PhysicalGameProduct
            {
                ProductAvailabilityStatus = AvailabilityStatus.DiscontinuedByManufacturer
            };

            Id = new Guid("44B0752E-998B-466A-AAAD-3ED535BA3559");
        }

        private List<GameProduct> GetGameSKUsListWithAllAvailabilityStatuses()
        {
            return new List<GameProduct>
            {
                notForSaleSKU,
                availableSKU,
                preOrderSKU,
                discontinuedSKU
            };
        }

        private List<Game> GetGameSearchList()
        {
            return new List<Game>
            {
                new Game
                {
                    GameAvailabilityStatus = AvailabilityStatus.NotForSale,
                    Name = "No Match NotForSale",
                    GameSKUs = GetGameSKUsListWithAllAvailabilityStatuses(),
                    Tags = GetTagList()
                },
                new Game
                {
                    GameAvailabilityStatus = AvailabilityStatus.Available,
                    Name = "Batch Available",
                    GameSKUs = GetGameSKUsListWithAllAvailabilityStatuses(),
                    Tags = new List<Tag>()
                },
                new Game
                {
                    GameAvailabilityStatus = AvailabilityStatus.PreOrder,
                    Name = "Game Match PreOrder",
                    GameSKUs = GetGameSKUsListWithAllAvailabilityStatuses(),
                    Tags = new List<Tag>()
                },
                new Game
                {
                    GameAvailabilityStatus = AvailabilityStatus.DiscontinuedByManufacturer,
                    Name = "Title Patch DiscontinuedByManufacturer",
                    GameSKUs = GetGameSKUsListWithAllAvailabilityStatuses(),
                    Tags = GetTagList()
                }
            };
        }

        private List<Tag> GetTagList()
        {
            return new List<Tag>
            {
                new Tag { Name = "Shooter" },
                new Tag { Name = "Simulation" },
                new Tag { Name = "RPG" },
                new Tag { Name = "3D" }
            };
        }
        private List<Platform> GetPlatformList()
        {
            return new List<Platform>
            {
                new Platform { PlatformCode = "XONE", PlatformName = "Xbox One" },
                new Platform { PlatformCode = "PS4", PlatformName = "PlayStation 4" },
                new Platform { PlatformCode = "WIIU", PlatformName = "Wii U" },
                new Platform { PlatformCode = "PC", PlatformName = "PC" }
            };
        }

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

        private Mock<IVeilDataAccess> GetDbFakeForAdvancedSearch()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(GetGameSearchList().AsQueryable());
            Mock<DbSet<Tag>> tagDbSetStub = TestHelpers.GetFakeAsyncDbSet(GetTagList().AsQueryable());
            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(GetPlatformList().AsQueryable());

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);
            dbStub.Setup(db => db.Tags).Returns(tagDbSetStub.Object);
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            return dbStub;
        }

        [Test]
        public async void AdvancedSearch_EmptySearchParameters_ReturnsAdvancedSearchView()
        {
            Mock<IVeilDataAccess> dbStub = GetDbFakeForAdvancedSearch();

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object)
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

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            // Calling contains on the null Name would throw
            var result = await controller.AdvancedSearch(null) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<AdvancedSearchViewModel>());

            var model = (AdvancedSearchViewModel) result.Model;

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

            GamesController controller = new GamesController(dbStub.Object)
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

            GamesController controller = new GamesController(dbStub.Object)
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

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.AdvancedSearch(null, title:"not a match for anything") as ViewResult;

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

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.AdvancedSearch(null, title:games[0].Name) as ViewResult;

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

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.AdvancedSearch(null, title:TITLE_FRAGMENT_COMMON_TO_ALL_SEARCH_GAMES) as ViewResult;

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

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.AdvancedSearch(null, title:"NotForSale") as ViewResult;

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

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.AdvancedSearch(null, title:"NotForSale") as ViewResult;

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

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.AdvancedSearch(null, title:TITLE_FRAGMENT_COMMON_TO_ALL_SEARCH_GAMES) as ViewResult;

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

            GamesController controller = new GamesController(dbStub.Object)
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

            GamesController controller = new GamesController(dbStub.Object)
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

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object,
                GamesPerPage = gamesPerPage
            };

            var result = await controller.AdvancedSearch(null, title:TITLE_FRAGMENT_COMMON_TO_ALL_SEARCH_GAMES) as ViewResult;

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

            GamesController controller = new GamesController(dbStub.Object)
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

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.AdvancedSearch(new List<string> { "not a match to any tag" }) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.Games, Is.Empty);
        }

        [Test]
        public void Details_NullId_Throws404Exception()
        {
            GamesController controller = new GamesController(null);

            Assert.That(async () => await controller.Details(null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }

        [Test]
        public void Details_IdNotInDb_Throws404Exception()
        {

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game>().AsQueryable());
            gameDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            GamesController controller = new GamesController(dbStub.Object);

            Assert.That(async () => await controller.Details(Id), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }

        [Test]
        public async void Details_IdInDb_ReturnsViewWithModel()
        {
            Game matchingGame = new Game
            {
                Id = Id,
                GameAvailabilityStatus = AvailabilityStatus.Available,
                GameSKUs = new List<GameProduct>()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { matchingGame }.AsQueryable());
            gameDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            GamesController controller = new GamesController(dbStub.Object)
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
                Id = Id,
                GameAvailabilityStatus = AvailabilityStatus.Available
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { matchingGame }.AsQueryable());

            gameDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InEmployeeRole();

            GamesController controller = new GamesController(dbStub.Object)
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
                Id = Id,
                GameAvailabilityStatus = status,
                GameSKUs = new List<GameProduct>()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game>{ matchingGame }.AsQueryable());

            gameDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InMemberRole();

            GamesController controller = new GamesController(dbStub.Object)
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
                Id = Id,
                GameAvailabilityStatus = AvailabilityStatus.NotForSale,
                GameSKUs = new List<GameProduct>()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { matchingGame }.AsQueryable());

            gameDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InMemberRole();

            GamesController controller = new GamesController(dbStub.Object)
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
                Id = Id,
                GameAvailabilityStatus = AvailabilityStatus.NotForSale
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { matchingGame }.AsQueryable());

            gameDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InNoRoles();

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            Assert.That(async () => await controller.Details(matchingGame.Id), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }

        [TestCase(VeilRoles.MEMBER_ROLE)]
        [TestCase(null /* Stand-in for No Role */)]
        public async void Details_UserIsUnprivilegedRole_ReturnsViewWithModelWithNoNotForSaleSKUs(string role)
        {
            Game matchingGame = new Game
            {
                Id = Id,
                GameAvailabilityStatus = AvailabilityStatus.PreOrder,
                GameSKUs = GetGameSKUsListWithAllAvailabilityStatuses()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { matchingGame }.AsQueryable());
            gameDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().IsInRole(role);

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Details(matchingGame.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.Not.Null);
            Assert.That(result.Model, Is.InstanceOf<Game>());

            var model = (Game) result.Model;

            Assert.That(model.GameSKUs, Has.All.Matches<Product>(p => p.ProductAvailabilityStatus != AvailabilityStatus.NotForSale));
        }

        [TestCase(VeilRoles.EMPLOYEE_ROLE)]
        [TestCase(VeilRoles.ADMIN_ROLE)]
        public async void Details_UserIsPrivilegedRole_ReturnsViewWithModelContainingNotForSaleSKUs(string role)
        {
            Game matchingGame = new Game
            {
                Id = Id,
                GameAvailabilityStatus = AvailabilityStatus.PreOrder,
                GameSKUs = GetGameSKUsListWithAllAvailabilityStatuses()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { matchingGame }.AsQueryable());
            gameDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().IsInRole(role);

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Details(matchingGame.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.Not.Null);
            Assert.That(result.Model, Is.InstanceOf<Game>());

            var model = (Game)result.Model;

            Assert.That(model.GameSKUs, Has.Some.Matches<Product>(p => p.ProductAvailabilityStatus == AvailabilityStatus.NotForSale));
        }

        [TestCase(VeilRoles.MEMBER_ROLE)]
        [TestCase(VeilRoles.ADMIN_ROLE)]
        public async void DeletePhysicalGameProduct_ValidDelete(string role)
        {
            GameProduct gameSku = new PhysicalGameProduct();
            gameSku.GameId = Id;
            gameSku.Id = Id;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<GameProduct>> gameProductDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> {gameSku}.AsQueryable());
            gameProductDbSetStub.SetupForInclude();

            gameProductDbSetStub.Setup(gp => gp.FindAsync(Id)).ReturnsAsync(gameSku);
            dbStub.Setup(db => db.GameProducts).Returns(gameProductDbSetStub.Object);


            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().IsInRole(role);

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.DeleteGameProduct(gameSku.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.Not.Null);
            Assert.That(result.Model, Is.InstanceOf<GameProduct>());
        }
    }
}
