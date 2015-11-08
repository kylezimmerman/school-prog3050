using System;
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

        [Test]
        public async void Details_NullId_ReturnsErrorView()
        {
            GamesController controller = new GamesController(null);

            var result = await controller.Details(null) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.EqualTo("Error"));
        }

        [Test]
        public async void Details_IdNotInDb_ReturnsErrorView()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game>().AsQueryable());
            gameDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            GamesController controller = new GamesController(dbStub.Object);

            var result = await controller.Details(Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.EqualTo("Error"));
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
        public async void Details_NotForSaleStatusShouldNotBeVisibleToMember_ReturnsErrorView()
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

            var result = await controller.Details(matchingGame.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.EqualTo("Error"));
        }

        [Test]
        public async void Details_NotForSaleStatusAndUserInNoRoles_ReturnsErrorView()
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

            var result = await controller.Details(matchingGame.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.EqualTo("Error"));
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
