using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Models.Reports;

namespace Veil.Tests.Controllers.ReportsControllerTests
{
    class GameDetailTests : ReportsControllerTestsBase
    {
        [Test]
        public async void GameDetailWithOrders_NoOrders()
        {
            Guid gameId = new Guid("40D655DF-FB62-4FD5-8065-A81C9868B145");
            Guid productId = new Guid("9AEE5E2E-378D-4828-B142-F69B81C53D8C");

            GameProduct gameProduct = new PhysicalGameProduct
            {
                Id = productId,
                GameId = gameId
            };

            Game game = new Game { GameSKUs = new List<GameProduct>() };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Game>> dbGameStub = TestHelpers.GetFakeAsyncDbSet(new List<Game>().AsQueryable());
            dbStub.Setup(db => db.Games).Returns(dbGameStub.Object);

            Mock<DbSet<GameProduct>> dbGameProductStub = TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> { gameProduct }.AsQueryable());
            dbStub.Setup(db => db.GameProducts).Returns(dbGameProductStub.Object);

            Mock<DbSet<WebOrder>> dbWebOrdersStub = TestHelpers.GetFakeAsyncDbSet(new List<WebOrder>().AsQueryable());
            dbStub.Setup(db => db.WebOrders).Returns(dbWebOrdersStub.Object);

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.GameDetail(gameId) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameDetailViewModel>());

            var model = (GameDetailViewModel)result.Model;

            Assert.That(model.Items.Count, Is.EqualTo(1));

            Assert.That(model.TotalNewSales, Is.EqualTo(0));
            Assert.That(model.TotalUsedSales, Is.EqualTo(0));
            Assert.That(model.TotalSales, Is.EqualTo(0));

            Assert.That(model.TotalNewQuantity, Is.EqualTo(0));
            Assert.That(model.TotalUsedQuantity, Is.EqualTo(0));
            Assert.That(model.TotalQuantity, Is.EqualTo(0));
        }

        [Test]
        public async void GameDetailWithOrders_CorrectQuantitiesAndSales()
        {
            Guid gameId = new Guid("40D655DF-FB62-4FD5-8065-A81C9868B145");
            Guid productId = new Guid("9AEE5E2E-378D-4828-B142-F69B81C53D8C");

            int newQty = 1;
            decimal newPrice = 9.99m;
            int usedQty = 2;
            decimal usedPrice = 0.99m;

            GameProduct gameProduct = new PhysicalGameProduct
            {
                Id = productId,
                GameId = gameId
            };

            Game game = new Game { GameSKUs = new List<GameProduct> { gameProduct } };

            WebOrder webOrder = new WebOrder
            {
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        Product = gameProduct,
                        ProductId = gameProduct.Id,
                        IsNew = true,
                        Quantity = newQty,
                        ListPrice = newPrice
                    },
                    new OrderItem
                    {
                        Product = gameProduct,
                        ProductId = gameProduct.Id,
                        IsNew = false,
                        Quantity = usedQty,
                        ListPrice = usedPrice
                    }
                },
                OrderStatus = OrderStatus.Processed
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Game>> dbGameStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { game }.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(dbGameStub.Object);

            Mock<DbSet<GameProduct>> dbGameProductStub = TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> { gameProduct }.AsQueryable());
            dbStub.Setup(db => db.GameProducts).Returns(dbGameProductStub.Object);

            Mock<DbSet<WebOrder>> dbWebOrdersStub = TestHelpers.GetFakeAsyncDbSet(new List<WebOrder> { webOrder }.AsQueryable());
            dbStub.Setup(db => db.WebOrders).Returns(dbWebOrdersStub.Object);

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.GameDetail(gameId) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameDetailViewModel>());

            var model = (GameDetailViewModel)result.Model;

            decimal totalNewSales = newPrice*newQty;
            decimal totalUsedSales = usedPrice*usedQty;

            Assert.That(model.TotalNewSales, Is.EqualTo(totalNewSales));
            Assert.That(model.TotalUsedSales, Is.EqualTo(totalUsedSales));
            Assert.That(model.TotalSales, Is.EqualTo(totalNewSales + totalUsedSales));

            Assert.That(model.TotalNewQuantity, Is.EqualTo(newQty));
            Assert.That(model.TotalUsedQuantity, Is.EqualTo(usedQty));
            Assert.That(model.TotalQuantity, Is.EqualTo(newQty + usedQty));
        }

        [Test]
        public async void GameDetailWithOrders_CorrectQuantitiesAndSalesByDate()
        {
            Guid gameId = new Guid("40D655DF-FB62-4FD5-8065-A81C9868B145");
            Guid productId = new Guid("9AEE5E2E-378D-4828-B142-F69B81C53D8C");

            int newQty = 1;
            decimal newPrice = 9.99m;
            int usedQty = 2;
            decimal usedPrice = 0.99m;

            GameProduct gameProduct = new PhysicalGameProduct
            {
                Id = productId,
                GameId = gameId
            };

            Game game = new Game { GameSKUs = new List<GameProduct> { gameProduct } };

            WebOrder webOrder1 = new WebOrder
            {
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        Product = gameProduct,
                        ProductId = gameProduct.Id,
                        IsNew = true,
                        Quantity = newQty,
                        ListPrice = newPrice
                    },
                    new OrderItem
                    {
                        Product = gameProduct,
                        ProductId = gameProduct.Id,
                        IsNew = false,
                        Quantity = usedQty,
                        ListPrice = usedPrice
                    }
                },
                OrderStatus = OrderStatus.Processed,
                OrderDate = DateTime.Now
            };
            WebOrder webOrder2 = new WebOrder
            {
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        Product = gameProduct,
                        ProductId = gameProduct.Id,
                        IsNew = true,
                        Quantity = newQty,
                        ListPrice = newPrice
                    },
                    new OrderItem
                    {
                        Product = gameProduct,
                        ProductId = gameProduct.Id,
                        IsNew = false,
                        Quantity = usedQty,
                        ListPrice = usedPrice
                    }
                },
                OrderStatus = OrderStatus.Processed,
                OrderDate = DateTime.MinValue
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Game>> dbGameStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { game }.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(dbGameStub.Object);

            Mock<DbSet<GameProduct>> dbGameProductStub = TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> { gameProduct }.AsQueryable());
            dbStub.Setup(db => db.GameProducts).Returns(dbGameProductStub.Object);

            Mock<DbSet<WebOrder>> dbWebOrdersStub = TestHelpers.GetFakeAsyncDbSet(new List<WebOrder> { webOrder1, webOrder2 }.AsQueryable());
            dbStub.Setup(db => db.WebOrders).Returns(dbWebOrdersStub.Object);

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.GameDetail(gameId, DateTime.Today, DateTime.Today) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameDetailViewModel>());

            var model = (GameDetailViewModel)result.Model;

            Assert.That(model.Items.Count, Is.EqualTo(1));

            decimal totalNewSales = newPrice * newQty;
            decimal totalUsedSales = usedPrice * usedQty;

            Assert.That(model.TotalNewSales, Is.EqualTo(totalNewSales));
            Assert.That(model.TotalUsedSales, Is.EqualTo(totalUsedSales));
            Assert.That(model.TotalSales, Is.EqualTo(totalNewSales + totalUsedSales));

            Assert.That(model.TotalNewQuantity, Is.EqualTo(newQty));
            Assert.That(model.TotalUsedQuantity, Is.EqualTo(usedQty));
            Assert.That(model.TotalQuantity, Is.EqualTo(newQty + usedQty));
        }
    }
}
