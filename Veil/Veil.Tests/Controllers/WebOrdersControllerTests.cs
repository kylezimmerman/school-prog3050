using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Principal;
using System.Web;
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

        [SetUp]
        public void Setup()
        {
            Id = new Guid("45B0752E-998B-466A-AAAD-3ED535BA3559");
            UserId = new Guid("09EABF21-D5AC-4A5D-ADF8-27180E6D889B");
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

        [Test]
        public void Details_IdIsNull_Throws404Exception()
        {
            WebOrdersController controller = new WebOrdersController(veilDataAccess: null, idGetter: null);

            Assert.That(async () => await controller.Details(null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void Details_UserIsEmployee_ReturnsMatchingModel()
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
            webOrdersDbSetStub.SetupForInclude();
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

            var result = await controller.Details(3) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<WebOrder>());

            var model = (WebOrder)result.Model;

            Assert.That(model.Id, Is.EqualTo(3));
            Assert.That(model.MemberId, Is.EqualTo(Id));
            Assert.That(model.OrderStatus, Is.EqualTo(OrderStatus.PendingProcessing));
        }

        [Test]
        public async void Details_UserIsMember_IsOwnOrder_ReturnsMatchingModel()
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
            webOrdersDbSetStub.SetupForInclude();
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

            var result = await controller.Details(1) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<WebOrder>());

            var model = (WebOrder)result.Model;

            Assert.That(model.Id, Is.EqualTo(1));
            Assert.That(model.MemberId, Is.EqualTo(UserId));
            Assert.That(model.OrderStatus, Is.EqualTo(OrderStatus.Processed));
        }

        [Test]
        public void Details_UserIsMember_IsNotOwnOrder_Throws404Exception()
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
            webOrdersDbSetStub.SetupForInclude();
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

            Assert.That(async () => await controller.Details(2), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public void Details_WebOrderNotFound_Throws404Exception()
        {
            List<WebOrder> orders = new List<WebOrder>
            {
                new WebOrder
                {
                    Id = 1,
                    MemberId = UserId,
                    OrderStatus = OrderStatus.Processed
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<WebOrder>> webOrdersDbSetStub = TestHelpers.GetFakeAsyncDbSet(orders.AsQueryable());
            webOrdersDbSetStub.SetupForInclude();
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

            Assert.That(async () => await controller.Details(2), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public void Cancel_IdIsNull_Throws404Exception()
        {
            WebOrdersController controller = new WebOrdersController(veilDataAccess: null, idGetter: null);

            Assert.That(async () => await controller.Cancel(null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void Cancel_UserIsMember_IsOwnOrder_OrderUnprocessed_ReturnsMatchingModel()
        {
            List<WebOrder> orders = new List<WebOrder>
            {
                new WebOrder
                {
                    Id = 1,
                    MemberId = UserId,
                    OrderStatus = OrderStatus.PendingProcessing
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
            webOrdersDbSetStub.SetupForInclude();
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

            var result = await controller.Cancel(1) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(orders[0].OrderStatus, Is.EqualTo(OrderStatus.UserCancelled));
        }

        [Test]
        public async void Cancel_UserIsMember_IsOwnOrder_OrderProcessed_ReturnsMatchingModel()
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
            webOrdersDbSetStub.SetupForInclude();
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

            var result = await controller.Cancel(1) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(orders[0].OrderStatus, Is.EqualTo(OrderStatus.Processed));
        }

        [Test]
        public void Cancel_UserIsMember_IsNotOwnOrder_Throws404Exception()
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
            webOrdersDbSetStub.SetupForInclude();
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

            Assert.That(async () => await controller.Cancel(3), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public void Cancel_WebOrderNotFound_Throws404Exception()
        {
            List<WebOrder> orders = new List<WebOrder>
            {
                new WebOrder
                {
                    Id = 1,
                    MemberId = UserId,
                    OrderStatus = OrderStatus.Processed
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<WebOrder>> webOrdersDbSetStub = TestHelpers.GetFakeAsyncDbSet(orders.AsQueryable());
            webOrdersDbSetStub.SetupForInclude();
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

            Assert.That(async () => await controller.Cancel(2), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }
    }
}