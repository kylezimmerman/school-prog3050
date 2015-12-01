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
        public void MemberDetail_UsernameIsNull_Throws404Exception()
        {
            var controller = new ReportsController(null);

            Assert.That(async () => await controller.MemberDetail(null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
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
    }
}
