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
            Game aGame = new Game
            {
                Id = gameId,
                Name = "gameName"
            };

            GameProduct aGameProduct = new PhysicalGameProduct
            {
                GameId = aGame.Id,
                Id = GameSKUId,
                PlatformCode = ps4Platform.PlatformCode,
                Game = aGame,
                Platform = ps4Platform
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            MockPlatforms(dbStub);
            StubEmptyProductLocationInventories(dbStub);

            Mock<DbSet<GameProduct>> gameProductDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> { aGameProduct }.AsQueryable());
            gameProductDbSetStub.SetupForInclude();
            gameProductDbSetStub.Setup(gp => gp.FindAsync(aGameProduct.Id)).ReturnsAsync(aGameProduct);

            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { aGame }.AsQueryable());
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
            GameProduct gameSku = new PhysicalGameProduct
            {
                GameId = gameId,
                Id = GameSKUId
            };

            Guid nonMatch = Guid.Empty;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<GameProduct>> gameProductDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> { gameSku }.AsQueryable());
            gameProductDbSetStub.SetupForInclude();

            gameProductDbSetStub.Setup(gp => gp.FindAsync(gameId)).ReturnsAsync(gameSku);
            dbStub.Setup(db => db.GameProducts).Returns(gameProductDbSetStub.Object);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.DeleteConfirmed(nonMatch), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }

        [Test]
        public async void DeleteConfirmed_NonZeroInventories_OnlyRemovesEmptyProductLocationInventories()
        {
            GameProduct gameProduct = new PhysicalGameProduct
            {
                Id = GameSKUId,
                Game = game,
                Platform = ps4Platform
            };

            var matchingEmptyInventory = new ProductLocationInventory
            {
                ProductId = gameProduct.Id,
                LocationId = new Guid("B5055382-1AC6-4306-8830-CF3225D55E36"),
                NewOnHand = 0,
                UsedOnHand = 0,
                NewOnOrder = 0
            };

            var inventories = new List<ProductLocationInventory>
            {
                matchingEmptyInventory,
                new ProductLocationInventory
                {
                    ProductId = gameProduct.Id,
                    LocationId = new Guid("26226470-8CB5-4408-B9FF-20CD35DAF7B9"),
                    NewOnHand = 1,
                    UsedOnHand = 0,
                    NewOnOrder = 0
                },
                new ProductLocationInventory
                {
                    ProductId = gameProduct.Id,
                    LocationId = new Guid("3817326F-77F1-46E9-93DB-6C8F13FAB135"),
                    NewOnHand = 0,
                    UsedOnHand = 1,
                    NewOnOrder = 0
                },
                new ProductLocationInventory
                {
                    ProductId = gameProduct.Id,
                    LocationId = new Guid("7625893A-CA94-42CB-9DF0-3D8CA1380405"),
                    NewOnHand = 0,
                    UsedOnHand = 0,
                    NewOnOrder = 1
                },
                new ProductLocationInventory
                {
                    ProductId = gameProduct.Id,
                    LocationId = new Guid("F341600E-EA96-4CDB-9EEE-62E42B8007D4"),
                    NewOnHand = -1,
                    UsedOnHand = 0,
                    NewOnOrder = 0
                },
                new ProductLocationInventory
                {
                    ProductId = gameProduct.Id,
                    LocationId = new Guid("964A866D-E6D1-43C6-A473-2C3AF6693473"),
                    NewOnHand = 0,
                    UsedOnHand = -1,
                    NewOnOrder = 0
                },
                new ProductLocationInventory
                {
                    ProductId = gameProduct.Id,
                    LocationId = new Guid("548DD025-F261-4BE3-AC5B-F59C08E5B033"),
                    NewOnHand = 0,
                    UsedOnHand = 0,
                    NewOnOrder = -1
                }
            };

            var deletedInventories = new List<ProductLocationInventory>();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<GameProduct>> gameProductsDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> { gameProduct }.AsQueryable());
            gameProductsDbSetStub.SetupForInclude();

            Mock<DbSet<ProductLocationInventory>> inventoriesDbSetStub = TestHelpers.GetFakeAsyncDbSet(inventories.AsQueryable());
            inventoriesDbSetStub.
                Setup(idb => idb.RemoveRange(It.IsAny<IEnumerable<ProductLocationInventory>>())).
                Returns<IEnumerable<ProductLocationInventory>>(val => val).
                Callback<IEnumerable<ProductLocationInventory>>(val => deletedInventories = val.ToList());

            dbStub.
                Setup(db => db.ProductLocationInventories).
                Returns(inventoriesDbSetStub.Object);


            dbStub.
                Setup(db => db.GameProducts).
                Returns(gameProductsDbSetStub.Object);

            dbStub.
                Setup(db => db.SaveChangesAsync()).
                ReturnsAsync(1);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            await controller.DeleteConfirmed(GameSKUId);

            Assert.That(deletedInventories, Has.Count.EqualTo(1));
            Assert.That(deletedInventories, Has.Member(matchingEmptyInventory));
        }

        [Test]
        public async void DeleteConfirmed_CatchesOnSaveDelete()
        {
            GameProduct aGameProduct = new PhysicalGameProduct
            {
                GameId = game.Id,
                Id = GameSKUId,
                PlatformCode = ps4Platform.PlatformCode,
                Game = game,
                Platform = ps4Platform
            };
            
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<GameProduct>> gameProductsDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> { aGameProduct }.AsQueryable());
            gameProductsDbSetStub.SetupForInclude();
            gameProductsDbSetStub.Setup(gp => gp.FindAsync(aGameProduct.Id)).ReturnsAsync(aGameProduct);

            StubEmptyProductLocationInventories(dbStub);

            dbStub.Setup(db => db.GameProducts).Returns(gameProductsDbSetStub.Object);
            dbStub.Setup(db => db.SaveChangesAsync()).Throws<DbUpdateException>();

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            var result = await controller.DeleteConfirmed(aGameProduct.Id) as RedirectToRouteResult;

            Assert.That(result != null);
        }

        [Test]
        public void DeleteConfirmed_SaveAsyncThrowingForeignKeyViolationException_HandlesException()
        {
            GameProduct gameProduct = new PhysicalGameProduct
            {
                Id = GameSKUId,
                Game = game,
                Platform = ps4Platform
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<GameProduct>> gameProductsDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> { gameProduct }.AsQueryable());
            gameProductsDbSetStub.SetupForInclude();

            StubEmptyProductLocationInventories(dbStub);

            dbStub.
                Setup(db => db.GameProducts).
                Returns(gameProductsDbSetStub.Object);


            DbUpdateException orderConstraintException = new DbUpdateException("See inner",
                SqlExceptionCreator.Create( // This message was copied verbatim from the actual exception being thrown
                    "The DELETE statement conflicted with the REFERENCE constraint " +
                    "\"FK_dbo.ProductLocationInventory_dbo.Product_ProductId\"." +
                    "The conflict occurred in database \"prog3050\", table " +
                    "\"dbo.ProductLocationInventory\", column 'ProductId'." +
                    "The statement has been terminated.",
                    (int)SqlErrorNumbers.ConstraintViolation));

            dbStub.
                Setup(db => db.SaveChangesAsync()).
                ThrowsAsync(orderConstraintException);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.DeleteConfirmed(GameSKUId), Throws.Nothing);
        }
    }
}
