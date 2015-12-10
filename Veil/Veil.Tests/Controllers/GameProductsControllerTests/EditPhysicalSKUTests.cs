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
    class EditPhysicalSKUTests : GameProductsControllerTestsBase
    {
        [Test]
        public void EditPhysical_GET_InvalidId_Throws404()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockPhysicalGameProducts(dbStub, new List<PhysicalGameProduct>().AsQueryable());

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.EditPhysicalSKU(Guid.Empty), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public void EditPhysical_GET_NullId_Throws404()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockPhysicalGameProducts(dbStub, new List<PhysicalGameProduct>().AsQueryable());

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.EditPhysicalSKU(id: null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void EditPhysical_GET_ValidId_ReturnsView()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockPlatforms(dbStub);
            MockCompanies(dbStub);
            var physicalGameProductsDbStub = MockPhysicalGameProducts(dbStub);
            physicalGameProductsDbStub.Setup(db => db.FindAsync(physicalGameProduct.Id)).ReturnsAsync(physicalGameProduct);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            var result = await controller.EditPhysicalSKU(physicalGameProduct.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<PhysicalGameProduct>());
            Assert.That(result.Model, Is.EqualTo(physicalGameProduct));
        }

        [Test]
        public void EditPhysical_POST_InvalidId_Throws404()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockPhysicalGameProducts(dbStub);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.EditPhysicalSKU(id: null, gameProduct: null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void EditPhysical_POST_HasModelErrors_ShowEditPage()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockPhysicalGameProducts(dbStub);
            MockPlatforms(dbStub);
            MockCompanies(dbStub);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);
            controller.ModelState.AddModelError("Name", "Name is required");

            var result = await controller.EditPhysicalSKU(physicalGameProduct.Id, physicalGameProduct) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<PhysicalGameProduct>());
            Assert.That(result.Model, Is.EqualTo(physicalGameProduct));
        }

        [Test]
        public async void EditPhysical_POST_Valid_RedirectsToGameDetails()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockPhysicalGameProducts(dbStub)
                .Setup(db => db.FindAsync(physicalGameProduct.Id))
                .ReturnsAsync(physicalGameProduct);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            var result = await controller.EditPhysicalSKU(physicalGameProduct.Id, physicalGameProduct) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Games"));
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Details"));
            Assert.That(result.RouteValues["Id"], Is.EqualTo(physicalGameProduct.GameId));
        }

        [Test]
        public async void EditPhysical_POST_Valid_SaveChangesCalledOnce()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockPhysicalGameProducts(dbStub)
                .Setup(db => db.FindAsync(physicalGameProduct.Id))
                .ReturnsAsync(physicalGameProduct);

            dbStub.Setup(db => db.SaveChangesAsync()).ReturnsAsync(1).Verifiable();

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            await controller.EditPhysicalSKU(physicalGameProduct.Id, physicalGameProduct);

            Assert.That(() => dbStub.Verify(db => db.SaveChangesAsync(), Times.Once), Throws.Nothing);
        }

        private Mock<DbSet<PhysicalGameProduct>> MockPhysicalGameProducts(Mock<IVeilDataAccess> dbStub, IQueryable<PhysicalGameProduct> physicalGameProducts = null)
        {
            physicalGameProducts = physicalGameProducts ?? new List<PhysicalGameProduct> { physicalGameProduct }.AsQueryable();
            var stub = TestHelpers.GetFakeAsyncDbSet(physicalGameProducts);
            dbStub.Setup(db => db.PhysicalGameProducts).Returns(stub.Object);

            return stub;
        }
    }
}
