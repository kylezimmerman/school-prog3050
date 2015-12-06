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
using Veil.DataModels.Models.Identity;
using Veil.Models.Reports;

namespace Veil.Tests.Controllers.ReportsControllerTests
{
    public class MemberDetailTests : ReportsControllerTestsBase
    {
        [Test]
        public void MemberDetail_UsernameIsNullOrWhitespace_Throws404Exception([Values(null, "", " ")]string userName)
        {
            var controller = new ReportsController(null);

            Assert.That(async () => await controller.MemberDetail(userName),
                Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public void MemberDetail_UserNotFound_Throws404Exception()
        {
            var member = new Member
            {
                UserAccount = new User
                {
                    UserName = "TestUser",
                    FirstName = "Test",
                    LastName = "User"
                },
                FavoriteTags = new List<Tag>(),
                FavoritePlatforms = new List<Platform>(),
                Wishlist = new List<Product>(),
                ReceivedFriendships = new List<Friendship>(),
                RequestedFriendships = new List<Friendship>(),
                WebOrders = new List<WebOrder>()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            ReportsController controller = new ReportsController(dbStub.Object);

            Assert.That(async () => await controller.MemberDetail("NotTestUser"), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void MemberDetail_UserFound_ReturnsMatchingModel()
        {
            var member = new Member
            {
                UserAccount = new User
                {
                    UserName = "TestUser",
                    FirstName = "Test",
                    LastName = "User"
                },
                FavoriteTags = new List<Tag>(),
                FavoritePlatforms = new List<Platform>(),
                Wishlist = new List<Product>(),
                ReceivedFriendships = new List<Friendship>(),
                RequestedFriendships = new List<Friendship>(),
                WebOrders = new List<WebOrder>
                {
                    new WebOrder
                    {
                        OrderItems = new List<OrderItem>
                        {
                            new OrderItem
                            {
                                Quantity = 1,
                                ListPrice = 10m
                            }
                        },
                        OrderSubtotal = 10m,
                        ShippingCost = 0m,
                        TaxAmount = 0m,
                        OrderStatus = OrderStatus.UserCancelled
                    },
                    new WebOrder
                    {
                        OrderItems = new List<OrderItem>
                        {
                            new OrderItem
                            {
                                Quantity = 2,
                                ListPrice = 10m
                            }
                        },
                        OrderSubtotal = 20m,
                        ShippingCost = 0m,
                        TaxAmount = 0m,
                        OrderStatus = OrderStatus.Processed
                    }
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberDetail(member.UserAccount.UserName) as ViewResult;

            Assert.That(result != null);

            var model = (MemberDetailViewModel)result.Model;

            Assert.That(model.FullName, Is.EqualTo("Test User"));
            Assert.That(model.OrderCount, Is.EqualTo(1));
            Assert.That(model.TotalQuantity, Is.EqualTo(2));
            Assert.That(model.Total, Is.EqualTo(20m));
        }

        [Test]
        public void DateFiltered_NullOrWhitespaceUserName_Throws404Exception([Values(null, "", " ")] string userName)
        {
            DateTime startTime = new DateTime(635847641516896833L, DateTimeKind.Local);

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member>().AsQueryable());
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            ReportsController controller = new ReportsController(dbStub.Object);

            Assert.That(async () => await controller.MemberDetail(userName, start: startTime, end: null),
                Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public void DateFiltered_UserNotFound_Throws404Exception()
        {
            DateTime startDate = new DateTime(635847641516896833L, DateTimeKind.Local);

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member>().AsQueryable());
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            ReportsController controller = new ReportsController(dbStub.Object);

            Assert.That(async () => await controller.MemberDetail("notFound", startDate, end: null),
                Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void DateFiltered_UserFound_ReturnsMatchingModel()
        {
            DateTime startDate = new DateTime(2015,12, 2);
            DateTime endDate = new DateTime(2015, 12, 3);

            var member = new Member
            {
                UserAccount = new User
                {
                    UserName = "TestUser",
                    FirstName = "Test",
                    LastName = "User"
                },
                FavoriteTags = new List<Tag>(),
                FavoritePlatforms = new List<Platform>(),
                Wishlist = new List<Product>(),
                ReceivedFriendships = new List<Friendship>(),
                RequestedFriendships = new List<Friendship>(),
                WebOrders = new List<WebOrder>
                {
                    new WebOrder
                    {
                        // Shouldn't be matched due to OrderDate
                        OrderItems = new List<OrderItem>
                        {
                            new OrderItem
                            {
                                Quantity = 1,
                                ListPrice = 10m
                            }
                        },
                        OrderSubtotal = 10m,
                        ShippingCost = 0m,
                        TaxAmount = 0m,
                        OrderStatus = OrderStatus.Processed,
                        OrderDate = new DateTime(2000, 1, 1)
                    },
                    // Should be matched
                    new WebOrder
                    {
                        OrderItems = new List<OrderItem>
                        {
                            new OrderItem
                            {
                                Quantity = 5,
                                ListPrice = 10m
                            }
                        },
                        OrderSubtotal = 50m,
                        ShippingCost = 0m,
                        TaxAmount = 0m,
                        OrderStatus = OrderStatus.Processed,
                        OrderDate = new DateTime(2015, 12, 2)
                    }
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.MemberDetail(member.UserAccount.UserName, startDate, endDate) as ViewResult;

            Assert.That(result != null);

            var model = (MemberDetailViewModel)result.Model;

            Assert.That(model.FullName, Is.EqualTo("Test User"));
            Assert.That(model.OrderCount, Is.EqualTo(1));
            Assert.That(model.TotalQuantity, Is.EqualTo(5));
            Assert.That(model.Total, Is.EqualTo(50m));
        }
    }
}
