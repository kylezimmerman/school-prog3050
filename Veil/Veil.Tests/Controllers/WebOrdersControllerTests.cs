using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels;
using Veil.DataModels.Models;
using Veil.Helpers;

namespace Veil.Tests.Controllers
{
    [TestFixture]
    class WebOrdersControllerTests
    {
        private Guid Id;
        private Guid UserId;
        private Guid GameId;

        [SetUp]
        public void Setup()
        {
            Id = new Guid("45B0752E-998B-466A-AAAD-3ED535BA3559");
            UserId = new Guid("09EABF21-D5AC-4A5D-ADF8-27180E6D889B");
            GameId = new Guid("EFBCB640-388B-E511-80DF-001CD8B71DA6");
        }

        [Test]
        public async void Index_UserIsMember_OnlySeesOwnOrders_ReturnsMatchingModel()
        {
            List<WebOrder> orders = new List<WebOrder>
            {
                new WebOrder
                {
                    Id = 1,
                    MemberId = UserId,
                    OrderStatus = OrderStatus.Processed
                },
                new WebOrder
                {
                    Id = 2,
                    MemberId = Id,
                    OrderStatus = OrderStatus.BeingProcessed
                },
                new WebOrder
                {
                    Id = 3,
                    MemberId = Id,
                    OrderStatus = OrderStatus.PendingProcessing
                },
                new WebOrder
                {
                    Id = 4,
                    MemberId = UserId,
                    OrderStatus = OrderStatus.UserCancelled
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<WebOrder>> webOrdersDbSetStub = TestHelpers.GetFakeAsyncDbSet(orders.AsQueryable());
            dbStub.Setup(db => db.WebOrders).Returns(webOrdersDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(UserId);

            WebOrdersController controller = new WebOrdersController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.Index() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<IEnumerable<WebOrder>>());

            var model = (List<WebOrder>)result.Model;

            Assert.That(model.Count, Is.EqualTo(2));
        }

        [Test]
        public async void Index_UserIsEmployee_OnlySeesPending_ReturnsMatchingModel()
        {
            List<WebOrder> orders = new List<WebOrder>
            {
                new WebOrder
                {
                    Id = 1,
                    MemberId = UserId,
                    OrderStatus = OrderStatus.Processed
                },
                new WebOrder
                {
                    Id = 2,
                    MemberId = Id,
                    OrderStatus = OrderStatus.BeingProcessed
                },
                new WebOrder
                {
                    Id = 3,
                    MemberId = Id,
                    OrderStatus = OrderStatus.PendingProcessing
                },
                new WebOrder
                {
                    Id = 4,
                    MemberId = UserId,
                    OrderStatus = OrderStatus.UserCancelled
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<WebOrder>> webOrdersDbSetStub = TestHelpers.GetFakeAsyncDbSet(orders.AsQueryable());
            dbStub.Setup(db => db.WebOrders).Returns(webOrdersDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);
            context.Setup(c => c.HttpContext.User.IsInRole(VeilRoles.EMPLOYEE_ROLE)).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(UserId);

            WebOrdersController controller = new WebOrdersController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.Index() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.EqualTo("Index_Employee"));
            Assert.That(result.Model, Is.InstanceOf<IEnumerable<WebOrder>>());

            var model = (List<WebOrder>)result.Model;

            Assert.That(model.Count, Is.EqualTo(1));
        }
    }
}