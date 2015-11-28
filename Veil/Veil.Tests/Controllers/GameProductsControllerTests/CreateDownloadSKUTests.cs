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
    public class CreateDownloadSKUTests : GameProductsControllerTestsBase
    {
        [Test]
        public void CreateDownloadSKU_GET_InvalidGameId_Throws404()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockGames(dbStub);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.CreateDownloadSKU(Guid.Empty), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
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
    }
}
