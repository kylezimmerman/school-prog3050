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
    public class DeleteTests : GameProductsControllerTestsBase
    {
        [Test]
        public async void Delete_ValidDelete()
        {
            GameProduct aGameProduct = new PhysicalGameProduct()
            {
                GameId = game.Id,
                Id = GameSKUId,
                PlatformCode = ps4Platform.PlatformCode,
                Game = game,
                Platform = ps4Platform,
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<GameProduct>> gameProductDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> { aGameProduct }.AsQueryable());

            gameProductDbSetStub.Setup(gp => gp.FindAsync(aGameProduct.Id)).ReturnsAsync(aGameProduct);
            dbStub.Setup(db => db.GameProducts).Returns(gameProductDbSetStub.Object);


            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            var result = await controller.Delete(aGameProduct.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameProduct>());
        }

        [Test]
        public void Delete_NoMatchingIdInDB()
        {
            GameProduct gameSku = new PhysicalGameProduct();
            gameSku.GameId = gameId;
            gameSku.Id = GameSKUId;

            Guid nonMatch = new Guid("44B0752E-998B-477A-AAAD-3ED535BA3559");

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<GameProduct>> gameProductDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> { gameSku }.AsQueryable());

            gameProductDbSetStub.Setup(gp => gp.FindAsync(gameId)).ReturnsAsync(gameSku);
            dbStub.Setup(db => db.GameProducts).Returns(gameProductDbSetStub.Object);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.Delete(nonMatch), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }

        [Test]
        public void Delete_NullID()
        {
            GameProduct gameSku = new PhysicalGameProduct();
            gameSku.GameId = gameId;
            gameSku.Id = GameSKUId;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<GameProduct>> gameProductDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> { gameSku }.AsQueryable());
            gameProductDbSetStub.SetupForInclude();

            gameProductDbSetStub.Setup(gp => gp.FindAsync(gameId)).ReturnsAsync(gameSku);
            dbStub.Setup(db => db.GameProducts).Returns(gameProductDbSetStub.Object);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.Delete(null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }
    }
}
