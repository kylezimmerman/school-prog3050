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
using Veil.DataModels.Models.Identity;
using Veil.Models.Reports;

namespace Veil.Tests.Controllers.ReportsControllerTests
{
    public class SalesTests : ReportsControllerTestsBase
    {
        [Test]
        public async void Sales_NoOrders_TotalsAreZero()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<WebOrder>> orderDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<WebOrder>().AsQueryable());
            dbStub.Setup(db => db.WebOrders).Returns(orderDbSetStub.Object);

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.Sales() as ViewResult;

            Assert.That(result != null);

            var model = (SalesViewModel)result.Model;

            Assert.That(model.OrderCount, Is.EqualTo(0));
            Assert.That(model.TotalQuantity, Is.EqualTo(0));
            Assert.That(model.ItemsSum, Is.EqualTo(0));
            Assert.That(model.ShippingSum, Is.EqualTo(0));
            Assert.That(model.TaxSum, Is.EqualTo(0));
            Assert.That(model.Total, Is.EqualTo(0));
        }

        [Test]
        public async void Sales_WithOrders_ReturnsMatchingModel()
        {
            List<WebOrder> orders = new List<WebOrder>
            {
                new WebOrder
                {
                    Id = 1,
                    OrderSubtotal = 10m,
                    ShippingCost = 12m,
                    TaxAmount = 1.13m,
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            Quantity = 1
                        }
                    },
                    Member = new Member
                    {
                        UserAccount = new User
                        {
                            UserName = "TestUser"
                        }
                    }
                },
                new WebOrder
                {
                    Id = 2,
                    OrderSubtotal = 20m,
                    ShippingCost = 12m,
                    TaxAmount = 2.26m,
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            Quantity = 2
                        }
                    },
                    Member = new Member
                    {
                        UserAccount = new User
                        {
                            UserName = "OtherTestUser"
                        }
                    }
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<WebOrder>> orderDbSetStub = TestHelpers.GetFakeAsyncDbSet(orders.AsQueryable());
            dbStub.Setup(db => db.WebOrders).Returns(orderDbSetStub.Object);

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.Sales() as ViewResult;

            Assert.That(result != null);

            var model = (SalesViewModel)result.Model;

            Assert.That(model.OrderCount, Is.EqualTo(2));
            Assert.That(model.TotalQuantity, Is.EqualTo(3));
            Assert.That(model.ItemsSum, Is.EqualTo(30m));
            Assert.That(model.ShippingSum, Is.EqualTo(24m));
            Assert.That(model.TaxSum, Is.EqualTo(3.39m));
            Assert.That(model.Total, Is.EqualTo(30m + 24m + 3.39m));
        }

        [Test]
        public async void DateFilter_Sales_ReturnsMatchingModel()
        {
            List<WebOrder> orders = new List<WebOrder>
            {
                new WebOrder
                {
                    Id = 1,
                    OrderDate = new DateTime(2015, 10, 10),
                    OrderSubtotal = 10m,
                    ShippingCost = 12m,
                    TaxAmount = 1.13m,
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            Quantity = 1
                        }
                    },
                    Member = new Member
                    {
                        UserAccount = new User
                        {
                            UserName = "TestUser"
                        }
                    }
                },
                new WebOrder
                {
                    Id = 2,
                    OrderDate = new DateTime(2014, 10, 10),
                    OrderSubtotal = 20m,
                    ShippingCost = 12m,
                    TaxAmount = 2.26m,
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            Quantity = 2
                        }
                    },
                    Member = new Member
                    {
                        UserAccount = new User
                        {
                            UserName = "OtherTestUser"
                        }
                    }
                },
                new WebOrder
                {
                    Id = 3,
                    OrderDate = DateTime.Today.AddDays(1),
                    OrderSubtotal = 20m,
                    ShippingCost = 12m,
                    TaxAmount = 2.26m,
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            Quantity = 2
                        }
                    },
                    Member = new Member
                    {
                        UserAccount = new User
                        {
                            UserName = "OtherTestUser"
                        }
                    }
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<WebOrder>> orderDbSetStub = TestHelpers.GetFakeAsyncDbSet(orders.AsQueryable());
            dbStub.Setup(db => db.WebOrders).Returns(orderDbSetStub.Object);

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.Sales(new DateTime(2015, 10, 10), null) as ViewResult;

            Assert.That(result != null);

            var model = (SalesViewModel)result.Model;

            Assert.That(model.OrderCount, Is.EqualTo(1));
            Assert.That(model.Items[0].OrderNumber, Is.EqualTo(orders[0].Id));
            Assert.That(model.StartDate.Value, Is.EqualTo(new DateTime(2015, 10, 10)));
            Assert.That(model.EndDate.Value, Is.EqualTo(DateTime.Now).Within(1).Minutes);
        }
    }
}