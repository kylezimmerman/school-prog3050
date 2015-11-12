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
using Veil.Helpers;

namespace Veil.Tests.Controllers
{
    [TestFixture]
    class GameProductsControllerTests
    {
        private Guid Id;

        private Game game;

        private Platform ps4Platform;

        private Company veilCompany;

        [SetUp]
        public void Setup()
        {
            Id = new Guid("44B0752E-998B-466A-AAAD-3ED535BA3559");

            game = new Game {Id = Id};

            ps4Platform = new Platform
            {
                PlatformCode = "PS4",
                PlatformName = "Playstation 4"
            };

            veilCompany = new Company()
            {
                Id = new Guid("B4FDA176-1EA6-469A-BB02-75125D811ED4"),
                Name = "Veil"
            };
        }

        private Mock<DbSet<Game>> MockGames(Mock<IVeilDataAccess> dbStub, IQueryable<Game> games = null)
        {
            games = games ?? new List<Game> {game}.AsQueryable();

            var stub = TestHelpers.GetFakeAsyncDbSet(games);
            dbStub.Setup(db => db.Games).Returns(stub.Object);

            return stub;
        }

        private Mock<DbSet<Platform>> MockPlatforms(Mock<IVeilDataAccess> dbStub, IQueryable<Platform> platforms = null)
        {
            platforms = platforms ?? new List<Platform> {ps4Platform}.AsQueryable();

            var stub = TestHelpers.GetFakeAsyncDbSet(platforms);
            dbStub.Setup(db => db.Platforms).Returns(stub.Object);

            return stub;
        }

        private Mock<DbSet<Company>> MockCompanies(Mock<IVeilDataAccess> dbStub, IQueryable<Company> companies = null)
        {
            companies = companies ?? new List<Company> {veilCompany}.AsQueryable();

            var stub = TestHelpers.GetFakeAsyncDbSet(companies);
            dbStub.Setup(db => db.Companies).Returns(stub.Object);

            return stub;
        }

        private Mock<DbSet<GameProduct>> MockEmptyGameProducts(Mock<IVeilDataAccess> dbStub)
        {
            var stub = TestHelpers.GetFakeAsyncDbSet(new List<GameProduct>().AsQueryable());
            dbStub.Setup(db => db.GameProducts).Returns(stub.Object);

            return stub;
        }

        [Test]
        public void CreatePhysicalSKU_GET_InvalidGameId_Throws404()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockGames(dbStub);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.CreatePhysicalSKU(Guid.Empty), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }

        [Test]
        public void CreateDownloadSKU_GET_InvalidGameId_Throws404()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockGames(dbStub);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.CreateDownloadSKU(Guid.Empty), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }

        [Test]
        public void CreatePhysicalSKU_POST_InvalidGameId_Throws404()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockGames(dbStub);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.CreatePhysicalSKU(Guid.Empty, gameProduct: null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }

        [Test]
        public void CreateDownloadSKU_POST_InvalidGameId_Throws404()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockGames(dbStub);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.CreateDownloadSKU(Guid.Empty, gameProduct: null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }

        [Test]
        public async void CreatePhysicalSKU_GET_ValidGameId_ReturnsView()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockGames(dbStub);
            MockPlatforms(dbStub);
            MockCompanies(dbStub);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            var result = await controller.CreatePhysicalSKU(game.Id) as ViewResult;

            Assert.That(result != null);
        }

        [Test]
        public async void CreateDownloadSKU_GET_ValidGameId_ReturnsView()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockGames(dbStub);
            MockPlatforms(dbStub);
            MockCompanies(dbStub);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            var result = await controller.CreateDownloadSKU(game.Id) as ViewResult;

            Assert.That(result != null);
        }

        [Test]
        public async void CreatePhysicalSKU_POST_ModelStateIsNotValid()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockGames(dbStub);
            MockPlatforms(dbStub);
            MockCompanies(dbStub);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);
            controller.ModelState.AddModelError("Name", "Name is required");

            var gameProduct = new PhysicalGameProduct();

            var result = await controller.CreatePhysicalSKU(game.Id, gameProduct) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<PhysicalGameProduct>());
        }

        [Test]
        public async void CreateDownloadSKU_POST_ModelStateIsNotValid()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockGames(dbStub);
            MockPlatforms(dbStub);
            MockCompanies(dbStub);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);
            controller.ModelState.AddModelError("Name", "Name is required");

            var gameProduct = new DownloadGameProduct();

            var result = await controller.CreateDownloadSKU(game.Id, gameProduct) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<DownloadGameProduct>());
        }

        [Test]
        public async void CreatePhysicalSKU_POST_SaveChangesCalledOnce()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            dbStub.Setup(db => db.SaveChangesAsync()).ReturnsAsync(1).Verifiable();
            MockGames(dbStub);
            MockEmptyGameProducts(dbStub);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            var gameProduct = new PhysicalGameProduct();

            await controller.CreatePhysicalSKU(game.Id, gameProduct);

            Assert.That(() => dbStub.Verify(db => db.SaveChangesAsync(), Times.Once), Throws.Nothing);
        }

        [Test]
        public async void CreateDownloadSKU_POST_SaveChangesCalledOnce()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            dbStub.Setup(db => db.SaveChangesAsync()).ReturnsAsync(1).Verifiable();
            MockGames(dbStub);
            MockEmptyGameProducts(dbStub);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            var gameProduct = new DownloadGameProduct();

            await controller.CreateDownloadSKU(game.Id, gameProduct);

            Assert.That(() => dbStub.Verify(db => db.SaveChangesAsync(), Times.Once), Throws.Nothing);
        }

        [Test]
        public async void CreatePhysicalSKU_POST_Valid_RedirectsToGameDetails()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockGames(dbStub);
            MockEmptyGameProducts(dbStub);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            var result = await controller.CreatePhysicalSKU(game.Id, new PhysicalGameProduct()) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Games"));
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Details"));
            Assert.That(result.RouteValues["Id"], Is.EqualTo(game.Id));
        }

        [Test]
        public async void CreateDownloadSKU_POST_Valid_RedirectsToGameDetails()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockGames(dbStub);
            MockEmptyGameProducts(dbStub);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            var result = await controller.CreateDownloadSKU(game.Id, new DownloadGameProduct()) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Games"));
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Details"));
            Assert.That(result.RouteValues["Id"], Is.EqualTo(game.Id));
        }

        [TestCase(VeilRoles.MEMBER_ROLE)]
        [TestCase(VeilRoles.ADMIN_ROLE)]
        public async void DeletePhysicalGameProduct_ValidDelete(string role)
        {
            GameProduct gameSku = new PhysicalGameProduct();
            gameSku.GameId = Id;
            gameSku.Id = Id;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<GameProduct>> gameProductDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> { gameSku }.AsQueryable());
            gameProductDbSetStub.SetupForInclude();

            gameProductDbSetStub.Setup(gp => gp.FindAsync(Id)).ReturnsAsync(gameSku);
            dbStub.Setup(db => db.GameProducts).Returns(gameProductDbSetStub.Object);


            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().IsInRole(role);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Delete(gameSku.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.Not.Null);
            Assert.That(result.Model, Is.InstanceOf<GameProduct>());
        }

        [TestCase(VeilRoles.MEMBER_ROLE)]
        [TestCase(VeilRoles.ADMIN_ROLE)]
        public void DeletePhysicalGameProduct_InvalidID(string role)
        {
            GameProduct gameSku = new PhysicalGameProduct();
            gameSku.GameId = Id;
            gameSku.Id = Id;

            Guid nonMatch = new Guid("44B0752E-998B-477A-AAAD-3ED535BA3559");

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<GameProduct>> gameProductDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> { gameSku }.AsQueryable());
            gameProductDbSetStub.SetupForInclude();

            gameProductDbSetStub.Setup(gp => gp.FindAsync(Id)).ReturnsAsync(gameSku);
            dbStub.Setup(db => db.GameProducts).Returns(gameProductDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().IsInRole(role);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            Assert.That(async () => await controller.Delete(nonMatch), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }
    }
}
