using System;
using System.Web;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;

namespace Veil.Tests.Controllers.GameProductsControllerTests
{
    public class CreatePhysicalGameSKUTests : GameProductsControllerTestsBase
    {
        [Test]
        public void CreatePhysicalSKU_GET_InvalidGameId_Throws404()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockGames(dbStub);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.CreatePhysicalSKU(Guid.Empty), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
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
        public async void CreatePhysicalSKU_POST_SaveChangesCalledTwice()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            dbStub.Setup(db => db.SaveChangesAsync()).ReturnsAsync(1).Verifiable();
            MockGames(dbStub);
            MockEmptyGameProducts(dbStub);
            StubLocationWithOnlineWarehouse(dbStub);
            StubEmptyProductLocationInventories(dbStub);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            var gameProduct = new PhysicalGameProduct();

            await controller.CreatePhysicalSKU(game.Id, gameProduct);

            Assert.That(() => dbStub.Verify(db => db.SaveChangesAsync(), Times.Exactly(2)), Throws.Nothing);
        }

        [Test]
        public async void CreatePhysicalSKU_POST_Valid_RedirectsToGameDetails()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockGames(dbStub);
            MockEmptyGameProducts(dbStub);
            StubLocationWithOnlineWarehouse(dbStub);
            StubEmptyProductLocationInventories(dbStub);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            var result = await controller.CreatePhysicalSKU(game.Id, new PhysicalGameProduct()) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Games"));
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Details"));
            Assert.That(result.RouteValues["Id"], Is.EqualTo(game.Id));
        }
    }
}
