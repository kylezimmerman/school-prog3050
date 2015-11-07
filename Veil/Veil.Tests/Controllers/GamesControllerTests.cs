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

        [SetUp]
        public void Setup()
        {
            notForSaleSKU = new PhysicalGameProduct
            {
                ProductAvailabilityStatus = AvailabilityStatus.NotForSale
            };

            availableSKU = new PhysicalGameProduct
            {
                ProductAvailabilityStatus = AvailabilityStatus.Available
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
                    Name = "No Match NotForSale"
                },
                new Game
                {
                    GameAvailabilityStatus = AvailabilityStatus.Available,
                    Name = "Batch Available"
                },
                new Game
                {
                    GameAvailabilityStatus = AvailabilityStatus.PreOrder,
                    Name = "Game Match PreOrder"
                },
                new Game
                {
                    GameAvailabilityStatus = AvailabilityStatus.DiscontinuedByManufacturer,
                    Name = "Title Patch DiscontinuedByManufacturer"
                }
            };
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
            Assert.That(result.Model, Is.InstanceOf<IEnumerable<Game>>());

            var model = (IEnumerable<Game>) result.Model;

            Assert.That(model, Has.Count.EqualTo(3));
            Assert.That(model, Has.None.Matches<Game>(g => g.GameAvailabilityStatus == AvailabilityStatus.NotForSale));
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
            Assert.That(result.Model, Is.InstanceOf<IEnumerable<Game>>());

            var model = (IEnumerable<Game>)result.Model;

            Assert.That(model, Has.Count.EqualTo(4));
            Assert.That(model, Has.Some.Matches<Game>(g => g.GameAvailabilityStatus == AvailabilityStatus.NotForSale));
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
            Assert.That(result.Model, Is.InstanceOf<IEnumerable<Game>>());

            var model = (IEnumerable<Game>)result.Model;

            Assert.That(model, Is.Empty);
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
            Assert.That(result.Model, Is.InstanceOf<IEnumerable<Game>>());

            var model = (IEnumerable<Game>)result.Model;

            Assert.That(model, Has.Count.EqualTo(1));
            Assert.That(model, Contains.Item(games[0]));
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

            var result = await controller.Search("atch") as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<IEnumerable<Game>>());

            var model = (IEnumerable<Game>)result.Model;

            Assert.That(model, Has.Count.EqualTo(games.Count));
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
            Assert.That(result.Model, Is.InstanceOf<IEnumerable<Game>>());

            var model = (IEnumerable<Game>)result.Model;

            Assert.That(model, Has.Count.EqualTo(0));
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
            Assert.That(result.Model, Is.InstanceOf<IEnumerable<Game>>());

            var model = (IEnumerable<Game>)result.Model;

            Assert.That(model, Has.Count.EqualTo(1));
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
        public void Details_NotForSaleStatusShouldNotBeVisibleToMember_Throws404Exception()
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
        public void Details_NotForSaleStatusAndUserInNoRoles_Throws404Exception()
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
    }
}
