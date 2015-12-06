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
    public class GameListTests : ReportsControllerTestsBase
    {
        [Test]
        public async void GameListWithOrders_ContainsOrders_CorrectQuantity()
        {
            Guid productId = new Guid("9AEE5E2E-378D-4828-B142-F69B81C53D8C");

            GameProduct gameProduct = new PhysicalGameProduct { Id = productId };

            Game game = new Game  { GameSKUs = new List<GameProduct> { gameProduct } };

            WebOrder webOrder = new WebOrder
            {
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        Product = gameProduct,
                        ProductId = gameProduct.Id,
                        Quantity = 2
                    }
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Game>> dbGameStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> {game}.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(dbGameStub.Object);

            Mock<DbSet<WebOrder>> dbWebOrdersStub = TestHelpers.GetFakeAsyncDbSet(new List<WebOrder> {webOrder}.AsQueryable());
            dbStub.Setup(db => db.WebOrders).Returns(dbWebOrdersStub.Object);

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.GameList() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<DateFilteredListViewModel<GameListRowViewModel>>());

            var model = (DateFilteredListViewModel<GameListRowViewModel>)result.Model;
            var items = model.Items;

            Assert.That(items.Count, Is.EqualTo(1));

            var item = items[0];

            Assert.That(item.QuantitySold, Is.EqualTo(2));
        }

        [Test]
        public async void GameListWithOrders_ContainsNoOrders()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Game>> dbGameStub = TestHelpers.GetFakeAsyncDbSet(new List<Game>().AsQueryable());
            dbStub.Setup(db => db.Games).Returns(dbGameStub.Object);

            Mock<DbSet<WebOrder>> dbWebOrdersStub = TestHelpers.GetFakeAsyncDbSet(new List<WebOrder>().AsQueryable());
            dbStub.Setup(db => db.WebOrders).Returns(dbWebOrdersStub.Object);

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.GameList() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<DateFilteredListViewModel<GameListRowViewModel>>());

            var model = (DateFilteredListViewModel<GameListRowViewModel>)result.Model;
            var items = model.Items;

            Assert.That(items.Count, Is.EqualTo(0));
        }

        [Test]
        public async void GameListWithOrders_ContainsOrders_CorrectQuantityInDateRange()
        {
            Guid productId = new Guid("9AEE5E2E-378D-4828-B142-F69B81C53D8C");

            GameProduct gameProduct = new PhysicalGameProduct { Id = productId };

            Game game = new Game { GameSKUs = new List<GameProduct> { gameProduct } };

            WebOrder webOrder1 = new WebOrder
            {
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        Product = gameProduct,
                        ProductId = gameProduct.Id,
                        Quantity = 2
                    }
                },
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
                        Quantity = 2
                    }
                },
                OrderDate = DateTime.MinValue
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Game>> dbGameStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { game }.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(dbGameStub.Object);

            Mock<DbSet<WebOrder>> dbWebOrdersStub = TestHelpers.GetFakeAsyncDbSet(new List<WebOrder> { webOrder1, webOrder2 }.AsQueryable());
            dbStub.Setup(db => db.WebOrders).Returns(dbWebOrdersStub.Object);

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.GameList(DateTime.Today, null) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<DateFilteredListViewModel<GameListRowViewModel>>());

            var model = (DateFilteredListViewModel<GameListRowViewModel>)result.Model;
            var items = model.Items;

            Assert.That(items.Count, Is.EqualTo(1));

            Assert.That(items[0].QuantitySold > 0);
        }

        [Test]
        public async void GameListWithOrders_ContainsNoOrdersInDateRange()
        {
            Guid productId = new Guid("9AEE5E2E-378D-4828-B142-F69B81C53D8C");

            GameProduct gameProduct = new PhysicalGameProduct { Id = productId };

            Game game = new Game { GameSKUs = new List<GameProduct> { gameProduct } };

            WebOrder webOrder = new WebOrder
            {
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        Product = gameProduct,
                        ProductId = gameProduct.Id,
                        Quantity = 2
                    }
                },
                OrderDate = DateTime.MinValue
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Game>> dbGameStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { game }.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(dbGameStub.Object);

            Mock<DbSet<WebOrder>> dbWebOrdersStub = TestHelpers.GetFakeAsyncDbSet(new List<WebOrder> { webOrder }.AsQueryable());
            dbStub.Setup(db => db.WebOrders).Returns(dbWebOrdersStub.Object);

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.GameList(DateTime.Today, DateTime.Now) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<DateFilteredListViewModel<GameListRowViewModel>>());

            var model = (DateFilteredListViewModel<GameListRowViewModel>)result.Model;
            var items = model.Items;

            Assert.That(items.Count, Is.EqualTo(1));

            Assert.That(items[0].QuantitySold, Is.EqualTo(0));
        }
    }
}
