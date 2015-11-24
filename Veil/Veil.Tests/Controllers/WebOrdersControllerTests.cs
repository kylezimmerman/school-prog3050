using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Moq;
using NUnit.Framework;
using Stripe;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels;
using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;
using Veil.Helpers;
using Veil.Services;
using Veil.Services.Interfaces;

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

            WebOrdersController controller = new WebOrdersController(dbStub.Object, idGetterStub.Object, stripeService: null, userManager: null)
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

            WebOrdersController controller = new WebOrdersController(dbStub.Object, idGetterStub.Object, stripeService: null, userManager: null)
            {
                ControllerContext = context.Object
            };

            var result = await controller.Index() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.EqualTo("Index_Employee"));
            Assert.That(result.Model, Is.InstanceOf<IEnumerable<WebOrder>>());

            var model = (List<WebOrder>)result.Model;

            Assert.That(model.Count, Is.EqualTo(2));
        }

        [Test]
        public void Details_IdIsNull_Throws404Exception()
        {
            WebOrdersController controller = new WebOrdersController(veilDataAccess: null, idGetter: null, stripeService: null, userManager: null);

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

            WebOrdersController controller = new WebOrdersController(dbStub.Object, idGetterStub.Object, stripeService: null, userManager: null)
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

            WebOrdersController controller = new WebOrdersController(dbStub.Object, idGetterStub.Object, stripeService: null, userManager: null)
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

            WebOrdersController controller = new WebOrdersController(dbStub.Object, idGetterStub.Object, stripeService: null, userManager: null)
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

            WebOrdersController controller = new WebOrdersController(dbStub.Object, idGetterStub.Object, stripeService: null, userManager: null)
            {
                ControllerContext = context.Object
            };

            Assert.That(async () => await controller.Details(2), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public void Cancel_IdIsNull_Throws404Exception()
        {
            WebOrdersController controller = new WebOrdersController(veilDataAccess: null, idGetter: null, stripeService: null, userManager: null);

            Assert.That(async () => await controller.Cancel(null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void Cancel_UserIsMember_IsOwnOrder_OrderUnprocessed_RedirectsToDetails()
        {
            List<WebOrder> orders = new List<WebOrder>
            {
                new WebOrder
                {
                    Id = 1,
                    MemberId = UserId,
                    OrderStatus = OrderStatus.PendingProcessing,
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            IsNew = true,
                            ProductId = Id,
                            Quantity = 2
                        },
                        new OrderItem
                        {
                            IsNew = false,
                            ProductId = Id,
                            Quantity = 1
                        },
                    }
                }
            };

            ProductLocationInventory inventory = new ProductLocationInventory
            {
                Location = new Location
                {
                    SiteName = Location.ONLINE_WAREHOUSE_NAME
                },
                ProductId = Id,
                NewOnHand = 0,
                UsedOnHand = 0
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();
            dbStub.Setup(db => db.UserStore).Returns(userStoreStub.Object);

            Mock<DbSet<WebOrder>> webOrdersDbSetStub = TestHelpers.GetFakeAsyncDbSet(orders.AsQueryable());
            webOrdersDbSetStub.Setup(wo => wo.FindAsync(orders[0].Id)).ReturnsAsync(orders[0]);
            dbStub.Setup(db => db.WebOrders).Returns(webOrdersDbSetStub.Object);

            Mock<DbSet<ProductLocationInventory>> inventoryDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(new List<ProductLocationInventory> { inventory }.AsQueryable());
            dbStub.Setup(db => db.ProductLocationInventories).Returns(inventoryDbSetStub.Object);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(UserId);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.Setup(s => s.RefundCharge(It.IsAny<string>())).Returns(true);

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.SendEmailAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>())).
                Returns(Task.FromResult(0)).
                Verifiable();

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IView> partialViewStub = new Mock<IView>();
            Mock<IViewEngine> viewEngineStub = new Mock<IViewEngine>();
            var viewEngineResult = new ViewEngineResult(partialViewStub.Object, viewEngineStub.Object);
            viewEngineStub.Setup(ve => ve.FindPartialView(It.IsAny<ControllerContext>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(viewEngineResult);
            ViewEngines.Engines.Clear();
            ViewEngines.Engines.Add(viewEngineStub.Object);

            WebOrdersController controller = new WebOrdersController(dbStub.Object, idGetterStub.Object, stripeServiceStub.Object, userManagerMock.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.Cancel(1) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(orders[0].OrderStatus, Is.EqualTo(OrderStatus.UserCancelled));
            Assert.That(orders[0].ReasonForCancellationMessage, Is.EqualTo("Order cancelled by customer."));
            Assert.That(inventory.NewOnHand, Is.EqualTo(2));
            Assert.That(inventory.UsedOnHand, Is.EqualTo(1));

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.SendEmailAsync(It.Is<Guid>(val => val == orders[0].MemberId), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void Cancel_UserIsMember_ValidCancellation_RefundFails_RedirectsToDetails()
        {
            List<WebOrder> orders = new List<WebOrder>
            {
                new WebOrder
                {
                    Id = 1,
                    MemberId = UserId,
                    OrderStatus = OrderStatus.PendingProcessing,
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            IsNew = true,
                            ProductId = Id,
                            Quantity = 2
                        },
                        new OrderItem
                        {
                            IsNew = false,
                            ProductId = Id,
                            Quantity = 1
                        },
                    }
                }
            };

            ProductLocationInventory inventory = new ProductLocationInventory
            {
                Location = new Location
                {
                    SiteName = Location.ONLINE_WAREHOUSE_NAME
                },
                ProductId = Id,
                NewOnHand = 0,
                UsedOnHand = 0
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<WebOrder>> webOrdersDbSetStub = TestHelpers.GetFakeAsyncDbSet(orders.AsQueryable());
            webOrdersDbSetStub.Setup(wo => wo.FindAsync(orders[0].Id)).ReturnsAsync(orders[0]);
            dbStub.Setup(db => db.WebOrders).Returns(webOrdersDbSetStub.Object);

            Mock<DbSet<ProductLocationInventory>> inventoryDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(new List<ProductLocationInventory> { inventory }.AsQueryable());
            dbStub.Setup(db => db.ProductLocationInventories).Returns(inventoryDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(UserId);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.Setup(s => s.RefundCharge(It.IsAny<string>())).Throws(new StripeException());

            WebOrdersController controller = new WebOrdersController(dbStub.Object, idGetterStub.Object, stripeServiceStub.Object, userManager: null)
            {
                ControllerContext = context.Object
            };

            var result = await controller.Cancel(1) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));
        }

        [Test]
        public async void Cancel_UserIsMember_ValidCancellation_SaveChangesFails_RedirectsToDetails()
        {
            List<WebOrder> orders = new List<WebOrder>
            {
                new WebOrder
                {
                    Id = 1,
                    MemberId = UserId,
                    OrderStatus = OrderStatus.PendingProcessing,
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            IsNew = true,
                            ProductId = Id,
                            Quantity = 2
                        },
                        new OrderItem
                        {
                            IsNew = false,
                            ProductId = Id,
                            Quantity = 1
                        },
                    }
                }
            };

            ProductLocationInventory inventory = new ProductLocationInventory
            {
                Location = new Location
                {
                    SiteName = Location.ONLINE_WAREHOUSE_NAME
                },
                ProductId = Id,
                NewOnHand = 0,
                UsedOnHand = 0
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            dbStub.Setup(db => db.SaveChangesAsync()).Throws(new DataException());

            Mock<DbSet<WebOrder>> webOrdersDbSetStub = TestHelpers.GetFakeAsyncDbSet(orders.AsQueryable());
            webOrdersDbSetStub.Setup(wo => wo.FindAsync(orders[0].Id)).ReturnsAsync(orders[0]);
            dbStub.Setup(db => db.WebOrders).Returns(webOrdersDbSetStub.Object);

            Mock<DbSet<ProductLocationInventory>> inventoryDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(new List<ProductLocationInventory> { inventory }.AsQueryable());
            dbStub.Setup(db => db.ProductLocationInventories).Returns(inventoryDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(UserId);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.Setup(s => s.RefundCharge(It.IsAny<string>())).Returns(true);

            WebOrdersController controller = new WebOrdersController(dbStub.Object, idGetterStub.Object, stripeServiceStub.Object, userManager: null)
            {
                ControllerContext = context.Object
            };

            var result = await controller.Cancel(1) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));
        }

        [Test]
        public async void Cancel_UserIsMember_IsOwnOrder_OrderProcessed_RedirectsToDetails()
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
            webOrdersDbSetStub.Setup(wo => wo.FindAsync(orders[0].Id)).ReturnsAsync(orders[0]);
            dbStub.Setup(db => db.WebOrders).Returns(webOrdersDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(UserId);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.Setup(s => s.RefundCharge(It.IsAny<string>())).Returns(true);

            WebOrdersController controller = new WebOrdersController(dbStub.Object, idGetterStub.Object, stripeServiceStub.Object, userManager: null)
            {
                ControllerContext = context.Object
            };

            var result = await controller.Cancel(1) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(orders[0].OrderStatus, Is.EqualTo(OrderStatus.Processed));
            Assert.That(orders[0].ReasonForCancellationMessage, Is.Null);
        }

        [Test]
        public void Cancel_UserIsMember_IsNotOwnOrder_Throws404Exception()
        {
            List<WebOrder> orders = new List<WebOrder>
            {
                new WebOrder
                {
                    Id = 3,
                    MemberId = Id,
                    OrderStatus = OrderStatus.PendingProcessing
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<WebOrder>> webOrdersDbSetStub = TestHelpers.GetFakeAsyncDbSet(orders.AsQueryable());
            webOrdersDbSetStub.Setup(wo => wo.FindAsync(orders[0].Id)).ReturnsAsync(orders[0]);
            dbStub.Setup(db => db.WebOrders).Returns(webOrdersDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(UserId);

            WebOrdersController controller = new WebOrdersController(dbStub.Object, idGetterStub.Object, stripeService: null, userManager: null)
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
            dbStub.Setup(db => db.WebOrders).Returns(webOrdersDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(UserId);

            WebOrdersController controller = new WebOrdersController(dbStub.Object, idGetterStub.Object, stripeService: null, userManager: null)
            {
                ControllerContext = context.Object
            };

            Assert.That(async () => await controller.Cancel(2), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void SetStatusCancelled_ReasonIsNull_PresentsError()
        {
            WebOrdersController controller = new WebOrdersController(veilDataAccess: null, idGetter: null, stripeService: null, userManager: null);

            var result = await controller.SetStatusCancelled(null, null, true) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));

            var alerts = controller.TempData["AlertMessages"];
            Assert.That(alerts is List<AlertMessage>);

            AlertMessage message = ((List<AlertMessage>) alerts)[0];
            Assert.That(message.Message, Is.EqualTo("You must provide a reason for cancellation."));
        }

        [Test]
        public async void SetStatusCancelled_Unconfirmed_PresentsError()
        {
            WebOrdersController controller = new WebOrdersController(veilDataAccess: null, idGetter: null, stripeService: null, userManager: null);

            var result = await controller.SetStatusCancelled(null, "TestReason", false) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));

            var alerts = controller.TempData["AlertMessages"];
            Assert.That(alerts is List<AlertMessage>);

            AlertMessage message = ((List<AlertMessage>)alerts)[0];
            Assert.That(message.Message, Is.EqualTo("You must confirm your action by checking \"Confirm Cancellation.\""));
        }

        [TestCase(OrderStatus.EmployeeCancelled)]
        [TestCase(OrderStatus.UserCancelled)]
        [TestCase(OrderStatus.Processed)]
        public async void SetStatusCancelled_CurrentStatusInvalid_PresentsError(OrderStatus status)
        {
            List<WebOrder> orders = new List<WebOrder>
            {
                new WebOrder
                {
                    Id = 1,
                    MemberId = UserId,
                    OrderStatus = status
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<WebOrder>> webOrdersDbSetStub = TestHelpers.GetFakeAsyncDbSet(orders.AsQueryable());
            webOrdersDbSetStub.Setup(wo => wo.FindAsync(orders[0].Id)).ReturnsAsync(orders[0]);
            dbStub.Setup(db => db.WebOrders).Returns(webOrdersDbSetStub.Object);

            WebOrdersController controller = new WebOrdersController(dbStub.Object, idGetter: null, stripeService: null, userManager: null);

            var result = await controller.SetStatusCancelled(1, "TestReason", true) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));

            var alerts = controller.TempData["AlertMessages"];
            Assert.That(alerts is List<AlertMessage>);

            AlertMessage message = ((List<AlertMessage>)alerts)[0];
            Assert.That(message.Message, Is.EqualTo("You can only cancel an order if it is pending processing or being processed."));
        }

        [TestCase(OrderStatus.PendingProcessing)]
        [TestCase(OrderStatus.BeingProcessed)]
        public async void SetStatusCancelled_CurrentStatusValid_RedirectsToDetails(OrderStatus status)
        {
            List<WebOrder> orders = new List<WebOrder>
            {
                new WebOrder
                {
                    Id = 1,
                    MemberId = UserId,
                    OrderStatus = status,
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            IsNew = true,
                            ProductId = Id,
                            Quantity = 2
                        },
                        new OrderItem
                        {
                            IsNew = false,
                            ProductId = Id,
                            Quantity = 1
                        },
                    }
                }
            };

            ProductLocationInventory inventory = new ProductLocationInventory
            {
                Location = new Location
                {
                    SiteName = Location.ONLINE_WAREHOUSE_NAME
                },
                ProductId = Id,
                NewOnHand = 0,
                UsedOnHand = 0
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();
            dbStub.Setup(db => db.UserStore).Returns(userStoreStub.Object);

            Mock<DbSet<WebOrder>> webOrdersDbSetStub = TestHelpers.GetFakeAsyncDbSet(orders.AsQueryable());
            webOrdersDbSetStub.Setup(wo => wo.FindAsync(orders[0].Id)).ReturnsAsync(orders[0]);
            dbStub.Setup(db => db.WebOrders).Returns(webOrdersDbSetStub.Object);

            Mock<DbSet<ProductLocationInventory>> inventoryDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(new List<ProductLocationInventory> { inventory }.AsQueryable());
            dbStub.Setup(db => db.ProductLocationInventories).Returns(inventoryDbSetStub.Object);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.Setup(s => s.RefundCharge(It.IsAny<string>())).Returns(true);

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.SendEmailAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>())).
                Returns(Task.FromResult(0)).
                Verifiable();

            Mock<ControllerContext> context = new Mock<ControllerContext>();

            Mock<IView> partialViewStub = new Mock<IView>();
            Mock<IViewEngine> viewEngineStub = new Mock<IViewEngine>();
            var viewEngineResult = new ViewEngineResult(partialViewStub.Object, viewEngineStub.Object);
            viewEngineStub.Setup(ve => ve.FindPartialView(It.IsAny<ControllerContext>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(viewEngineResult);
            ViewEngines.Engines.Clear();
            ViewEngines.Engines.Add(viewEngineStub.Object);

            WebOrdersController controller = new WebOrdersController(dbStub.Object, null, stripeServiceStub.Object, userManagerMock.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.SetStatusCancelled(1, "TestReason", true) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(orders[0].OrderStatus, Is.EqualTo(OrderStatus.EmployeeCancelled));
            Assert.That(orders[0].ReasonForCancellationMessage, Is.EqualTo("TestReason"));
            Assert.That(inventory.NewOnHand, Is.EqualTo(2));
            Assert.That(inventory.UsedOnHand, Is.EqualTo(1));

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.SendEmailAsync(It.Is<Guid>(val => val == orders[0].MemberId), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void SetStatusProcessing_Unconfirmed_PresentsError()
        {
            WebOrdersController controller = new WebOrdersController(veilDataAccess: null, idGetter: null, stripeService: null, userManager: null);

            var result = await controller.SetStatusProcessing(null, false) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));

            var alerts = controller.TempData["AlertMessages"];
            Assert.That(alerts is List<AlertMessage>);

            AlertMessage message = ((List<AlertMessage>)alerts)[0];
            Assert.That(message.Message, Is.EqualTo("You must confirm your action by checking \"Confirm Processing.\""));
        }

        [TestCase(OrderStatus.UserCancelled)]
        [TestCase(OrderStatus.EmployeeCancelled)]
        [TestCase(OrderStatus.Processed)]
        [TestCase(OrderStatus.BeingProcessed)]
        public async void SetStatusProcessing_CurrentStatusInvalid_PresentsError(OrderStatus status)
        {
            List<WebOrder> orders = new List<WebOrder>
            {
                new WebOrder
                {
                    Id = 1,
                    MemberId = UserId,
                    OrderStatus = status
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<WebOrder>> webOrdersDbSetStub = TestHelpers.GetFakeAsyncDbSet(orders.AsQueryable());
            webOrdersDbSetStub.Setup(wo => wo.FindAsync(orders[0].Id)).ReturnsAsync(orders[0]);
            dbStub.Setup(db => db.WebOrders).Returns(webOrdersDbSetStub.Object);

            WebOrdersController controller = new WebOrdersController(dbStub.Object, idGetter: null, stripeService: null, userManager: null);

            var result = await controller.SetStatusProcessing(1, true) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));

            var alerts = controller.TempData["AlertMessages"];
            Assert.That(alerts is List<AlertMessage>);

            AlertMessage message = ((List<AlertMessage>)alerts)[0];
            Assert.That(message.Message, Is.EqualTo("An order can only begin processing if its status is Pending Processing."));
        }

        [Test]
        public async void SetStatusProcessing_CurrentStatusInvalid_RedirectsToDetails()
        {
            List<WebOrder> orders = new List<WebOrder>
            {
                new WebOrder
                {
                    Id = 1,
                    MemberId = UserId,
                    OrderStatus = OrderStatus.PendingProcessing
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<WebOrder>> webOrdersDbSetStub = TestHelpers.GetFakeAsyncDbSet(orders.AsQueryable());
            webOrdersDbSetStub.Setup(wo => wo.FindAsync(orders[0].Id)).ReturnsAsync(orders[0]);
            dbStub.Setup(db => db.WebOrders).Returns(webOrdersDbSetStub.Object);

            WebOrdersController controller = new WebOrdersController(dbStub.Object, idGetter: null, stripeService: null, userManager: null);

            var result = await controller.SetStatusProcessing(1, true) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(orders[0].OrderStatus, Is.EqualTo(OrderStatus.BeingProcessed));
        }

        [TestCase(OrderStatus.UserCancelled)]
        [TestCase(OrderStatus.EmployeeCancelled)]
        [TestCase(OrderStatus.Processed)]
        [TestCase(OrderStatus.PendingProcessing)]
        public async void SetStatusProcessed_CurrentStatusInvalid_PresentsError(OrderStatus status)
        {
            List<WebOrder> orders = new List<WebOrder>
            {
                new WebOrder
                {
                    Id = 1,
                    MemberId = UserId,
                    OrderStatus = status
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<WebOrder>> webOrdersDbSetStub = TestHelpers.GetFakeAsyncDbSet(orders.AsQueryable());
            webOrdersDbSetStub.Setup(wo => wo.FindAsync(orders[0].Id)).ReturnsAsync(orders[0]);
            dbStub.Setup(db => db.WebOrders).Returns(webOrdersDbSetStub.Object);

            WebOrdersController controller = new WebOrdersController(dbStub.Object, idGetter: null, stripeService: null, userManager: null);

            var result = await controller.SetStatusProcessed(1, true) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));

            var alerts = controller.TempData["AlertMessages"];
            Assert.That(alerts is List<AlertMessage>);

            AlertMessage message = ((List<AlertMessage>)alerts)[0];
            Assert.That(message.Message, Is.EqualTo("An order can only be processed if its status is Being Processed."));
        }

        [Test]
        public async void SetStatusProcessed_Unconfirmed_PresentsError()
        {
            WebOrdersController controller = new WebOrdersController(veilDataAccess: null, idGetter: null, stripeService: null, userManager: null);

            var result = await controller.SetStatusProcessed(null, false) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));

            var alerts = controller.TempData["AlertMessages"];
            Assert.That(alerts is List<AlertMessage>);

            AlertMessage message = ((List<AlertMessage>)alerts)[0];
            Assert.That(message.Message, Is.EqualTo("You must confirm your action by checking \"Confirm Processed.\""));
        }

        [Test]
        public async void SetStatusProcessed_CurrentStatusValid_RedirectsToDetails()
        {
            List<WebOrder> orders = new List<WebOrder>
            {
                new WebOrder
                {
                    Id = 1,
                    MemberId = UserId,
                    OrderStatus = OrderStatus.BeingProcessed,
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();
            dbStub.Setup(db => db.UserStore).Returns(userStoreStub.Object);

            Mock<DbSet<WebOrder>> webOrdersDbSetStub = TestHelpers.GetFakeAsyncDbSet(orders.AsQueryable());
            webOrdersDbSetStub.Setup(wo => wo.FindAsync(orders[0].Id)).ReturnsAsync(orders[0]);
            dbStub.Setup(db => db.WebOrders).Returns(webOrdersDbSetStub.Object);

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.SendEmailAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>())).
                Returns(Task.FromResult(0)).
                Verifiable();

            Mock<ControllerContext> context = new Mock<ControllerContext>();

            Mock<IView> partialViewStub = new Mock<IView>();
            Mock<IViewEngine> viewEngineStub = new Mock<IViewEngine>();
            var viewEngineResult = new ViewEngineResult(partialViewStub.Object, viewEngineStub.Object);
            viewEngineStub.Setup(ve => ve.FindPartialView(It.IsAny<ControllerContext>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(viewEngineResult);
            ViewEngines.Engines.Clear();
            ViewEngines.Engines.Add(viewEngineStub.Object);

            WebOrdersController controller = new WebOrdersController(dbStub.Object, null, null, userManagerMock.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.SetStatusProcessed(1, true) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(orders[0].OrderStatus, Is.EqualTo(OrderStatus.Processed));

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.SendEmailAsync(It.Is<Guid>(val => val == orders[0].MemberId), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(1)),
                Throws.Nothing);
        }
    }
}