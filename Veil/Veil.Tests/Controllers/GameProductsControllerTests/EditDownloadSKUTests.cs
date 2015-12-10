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
using Veil.DataModels.Models;

namespace Veil.Tests.Controllers.GameProductsControllerTests
{
    class EditDownloadSKUTests : GameProductsControllerTestsBase
    {
        [Test]
        public void EditDownload_GET_InvalidId_Throws404()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockDownloadGameProducts(dbStub, new List<DownloadGameProduct>().AsQueryable());

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.EditDownloadSKU(Guid.Empty), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public void EditDownload_GET_NullId_Throws404()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockDownloadGameProducts(dbStub, new List<DownloadGameProduct>().AsQueryable());

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.EditDownloadSKU(id: null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void EditDownload_GET_ValidId_ReturnsView()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockPlatforms(dbStub);
            MockCompanies(dbStub);
            var downloadGameProductsDbStub = MockDownloadGameProducts(dbStub);
            downloadGameProductsDbStub.Setup(db => db.FindAsync(downloadGameProduct.Id)).ReturnsAsync(downloadGameProduct);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            var result = await controller.EditDownloadSKU(downloadGameProduct.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<DownloadGameProduct>());
            Assert.That(result.Model, Is.EqualTo(downloadGameProduct));
        }

        [Test]
        public void EditDownload_POST_InvalidId_Throws404()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockDownloadGameProducts(dbStub);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.EditDownloadSKU(id: null, gameProduct: null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void EditPhysical_POST_HasModelErrors_ShowEditPage()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockDownloadGameProducts(dbStub);
            MockPlatforms(dbStub);
            MockCompanies(dbStub);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);
            controller.ModelState.AddModelError("Name", "Name is required");

            var result = await controller.EditDownloadSKU(downloadGameProduct.Id, downloadGameProduct) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<DownloadGameProduct>());
            Assert.That(result.Model, Is.EqualTo(downloadGameProduct));
        }

        [Test]
        public async void EditDownload_POST_Valid_RedirectsToGameDetails()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockDownloadGameProducts(dbStub)
                .Setup(db => db.FindAsync(downloadGameProduct.Id))
                .ReturnsAsync(downloadGameProduct);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            var result = await controller.EditDownloadSKU(downloadGameProduct.Id, downloadGameProduct) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Games"));
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Details"));
            Assert.That(result.RouteValues["Id"], Is.EqualTo(downloadGameProduct.GameId));
        }

        [Test]
        public async void EditDownload_POST_Valid_SaveChangesCalledOnce()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockDownloadGameProducts(dbStub)
                .Setup(db => db.FindAsync(downloadGameProduct.Id))
                .ReturnsAsync(downloadGameProduct);
            dbStub.Setup(db => db.SaveChangesAsync()).ReturnsAsync(1).Verifiable();

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            await controller.EditDownloadSKU(downloadGameProduct.Id, downloadGameProduct);

            Assert.That(() => dbStub.Verify(db => db.SaveChangesAsync(), Times.Once), Throws.Nothing);
        }

        private Mock<DbSet<DownloadGameProduct>> MockDownloadGameProducts(Mock<IVeilDataAccess> dbStub, IQueryable<DownloadGameProduct> downloadGameProducts = null)
        {
            downloadGameProducts = downloadGameProducts ?? new List<DownloadGameProduct> { downloadGameProduct }.AsQueryable();

            var stub = TestHelpers.GetFakeAsyncDbSet(downloadGameProducts);
            dbStub.Setup(db => db.DownloadGameProducts).Returns(stub.Object);

            return stub;
        }
    }
}
