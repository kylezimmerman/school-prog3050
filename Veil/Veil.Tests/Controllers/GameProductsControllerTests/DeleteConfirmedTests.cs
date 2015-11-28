using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
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
    public class DeleteConfirmedTests : GameProductsControllerTestsBase
    {
        [Test]
        public async void DeleteConfirmed_ValidDelete()
        {
            Game aGame = new Game();
            aGame.Id = Id;
            aGame.Name = "gameName";

            GameProduct aGameProduct = new PhysicalGameProduct();
            aGameProduct.GameId = aGame.Id;
            aGameProduct.Id = GameSKUId;
            aGameProduct.PlatformCode = ps4Platform.PlatformCode;
            aGameProduct.Game = aGame;
            aGameProduct.Platform = ps4Platform;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<GameProduct>> gameProductDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> { aGameProduct }.AsQueryable());
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { aGame }.AsQueryable());

            MockPlatforms(dbStub);
            StubLocationWithOnlineWarehouse(dbStub);
            StubEmptyProductLocationInventories(dbStub);
            gameProductDbSetStub.SetupForInclude();

            gameProductDbSetStub.Setup(gp => gp.FindAsync(aGameProduct.Id)).ReturnsAsync(aGameProduct);
            gameDbSetStub.Setup(g => g.FindAsync(aGame.Id)).ReturnsAsync(aGame);

            dbStub.Setup(db => db.GameProducts).Returns(gameProductDbSetStub.Object);
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            var result = await controller.DeleteConfirmed(aGameProduct.Id) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Index"));
        }

        [Test]
        public void DeleteConfirmed_NoMatchingIdInDB()
        {
            GameProduct gameSku = new PhysicalGameProduct();
            gameSku.GameId = Id;
            gameSku.Id = GameSKUId;

            Guid nonMatch = new Guid("44B0752E-998B-477A-AAAD-3ED535BA3559");

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<GameProduct>> gameProductDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> { gameSku }.AsQueryable());
            gameProductDbSetStub.SetupForInclude();

            gameProductDbSetStub.Setup(gp => gp.FindAsync(Id)).ReturnsAsync(gameSku);
            dbStub.Setup(db => db.GameProducts).Returns(gameProductDbSetStub.Object);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.DeleteConfirmed(nonMatch), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }

        [Test]
        public async void DeleteConfirmed_CatchesOnSaveDelete()
        {
            GameProduct aGameProduct = new PhysicalGameProduct();
            aGameProduct.GameId = game.Id;
            aGameProduct.Id = GameSKUId;
            aGameProduct.PlatformCode = ps4Platform.PlatformCode;
            aGameProduct.Game = game;
            aGameProduct.Platform = ps4Platform;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<GameProduct>> gameProductsDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> { aGameProduct }.AsQueryable());
            gameProductsDbSetStub.SetupForInclude();
            gameProductsDbSetStub.Setup(gp => gp.FindAsync(aGameProduct.Id)).ReturnsAsync(aGameProduct);

            StubLocationWithOnlineWarehouse(dbStub);
            StubEmptyProductLocationInventories(dbStub);

            dbStub.Setup(db => db.GameProducts).Returns(gameProductsDbSetStub.Object);
            dbStub.Setup(db => db.SaveChangesAsync()).Throws<DbUpdateException>();


            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            var result = await controller.DeleteConfirmed(aGameProduct.Id) as RedirectToRouteResult;

            Assert.That(result != null);
        }

        // TODO: Implement this
        [Ignore]
        [Test]
        public async void DeleteConfirmed_SaveAsyncThrows_HasValidModelForRedisplayedView()
        {

        }
    }
}
