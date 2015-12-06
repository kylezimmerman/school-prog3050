using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Moq;
using NUnit.Framework;
using Stripe;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;
using Veil.Exceptions;
using Veil.Helpers;
using Veil.Models;
using Veil.Services;
using Veil.Services.Interfaces;

namespace Veil.Tests.Controllers.CheckoutControllerTests
{
    public class PlaceOrderTests : CheckoutControllerTestsBase
    {
        private Location onlineWarehouse;

        [SetUp]
        public void Setup()
        {
            onlineWarehouse = new Location
            {
                SiteName = Location.ONLINE_WAREHOUSE_NAME
            };
        }

        private void SetupVeilDataAccessWithInventoriesForBothCartProducts(Mock<IVeilDataAccess> dbFake, int newOnHand = 10, int usedOnHand = 2)
        {
            Mock<DbSet<ProductLocationInventory>> inventoryDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(GetProductInventories(newOnHand, usedOnHand).AsQueryable());

            dbFake.
                Setup(db => db.ProductLocationInventories).
                Returns(inventoryDbSetStub.Object);
        }

        private void SetupVeilDataAccessUserStore(Mock<IVeilDataAccess> dbFake)
        {
            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();
            dbFake.Setup(db => db.UserStore).Returns(userStoreStub.Object);
        }

        private void SetupVeilDataAccessWithWebOrders(Mock<IVeilDataAccess> dbFake, List<WebOrder> webOrders)
        {
            Mock<DbSet<WebOrder>> webOrderDbSetFake = TestHelpers.GetFakeAsyncDbSet(webOrders.AsQueryable());
            webOrderDbSetFake.
                Setup(wdb => wdb.Add(It.IsAny<WebOrder>())).
                Returns<WebOrder>(val => val).
                Callback<WebOrder>(webOrders.Add);

            dbFake.
                Setup(db => db.WebOrders).
                Returns(webOrderDbSetFake.Object);
        }

        private void SetupViewEngineStub(string viewString = null)
        {
            viewString = viewString ?? string.Empty;

            Mock<IView> partialViewStub = new Mock<IView>();
            partialViewStub.
                Setup(pvs => pvs.Render(It.IsAny<ViewContext>(), It.IsAny<TextWriter>())).
                Callback((ViewContext vc, TextWriter tw) => tw.Write(viewString));

            Mock<IViewEngine> viewEngineStub = new Mock<IViewEngine>();
            var viewEngineResult = new ViewEngineResult(partialViewStub.Object, viewEngineStub.Object);
            viewEngineStub.Setup(ve => ve.FindPartialView(It.IsAny<ControllerContext>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(viewEngineResult);
            ViewEngines.Engines.Clear();
            ViewEngines.Engines.Add(viewEngineStub.Object);
        }

        private Mock<VeilUserManager> GetUserManageStubWithSendEmailSetup(IVeilDataAccess db)
        {
            Mock<VeilUserManager> userManageStub = new Mock<VeilUserManager>(db, null /*messageService*/, null /*dataProtectionProvider*/);
            userManageStub.
                Setup(um => um.SendEmailAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>())).
                Returns(Task.FromResult(0));

            return userManageStub;
        }

        private List<ProductLocationInventory> GetProductInventories(int newOnHand = 10, int usedOnHand = 2)
        {
            return new List<ProductLocationInventory>
            {
                new ProductLocationInventory
                {
                    Location = onlineWarehouse,
                    ProductId = cartProduct1Id,
                    Product = cartProduct1,
                    NewOnHand = newOnHand,
                    UsedOnHand = usedOnHand
                },
                new ProductLocationInventory
                {
                    Location = onlineWarehouse,
                    ProductId = cartProduct2Id,
                    Product = cartProduct2,
                    NewOnHand = newOnHand,
                    UsedOnHand = usedOnHand
                }
            };
        }

        [Test]
        public async void PlaceOrder_AddressNotSetInSession_RedirectsToShippingInfo()
        {
            WebOrderCheckoutDetails checkoutDetails = new WebOrderCheckoutDetails();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(checkoutDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.PlaceOrder(new List<CartItem>()) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.ShippingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo(null));
        }

        [Test]
        public async void PlaceOrder_BillingInfoNotSetInsession_RedirectsToBillingInfo()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, new List<MemberAddress> { new MemberAddress { Id = addressId } });
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.PlaceOrder(new List<CartItem>()) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.BillingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo(null));
        }

        [Test]
        public async void PlaceOrder_NullSessionOrderDetails_RedirectsToShippingInfo()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(null);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.PlaceOrder(new List<CartItem>()) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.ShippingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo(null));
        }

        [Test]
        public async void PlaceOrder_EmptyCart_RedirectsToCardIndex()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, new List<Cart>());

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingBillingDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.PlaceOrder(null) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Index"));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Cart"));
        }

        [Test]
        public async void PlaceOrder_CartDoesNotMatchPostedBackItems_RedirectsToConfirmOrder()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingBillingDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.PlaceOrder(new List<CartItem> { usedProduct1CartItem }) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo("ConfirmOrder"));
            Assert.That(result.RouteValues["Controller"], Is.Null);
        }

        [Test]
        public async void PlaceOrder_AddressIsIdButIdNotInDb_RedirectsToShippingInfo()
        {
            WebOrderCheckoutDetails details = new WebOrderCheckoutDetails
            {
                MemberAddressId = addressId,
                MemberCreditCardId = creditCardId
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, new List<MemberAddress>());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(details);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList()) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.ShippingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo(null));
        }

        [Test]
        public async void PlaceOrder_AddressIsId_GetsAddressFromDb()
        {
            WebOrderCheckoutDetails details = new WebOrderCheckoutDetails
            {
                MemberAddressId = addressId,
                StripeCardToken = "cardToken"
            };

            MemberAddress address = new MemberAddress
            {
                Address = new Address
                {
                    City = "Waterloo",
                    PostalCode = "N2L 6R2",
                    StreetAddress = "445 Wes Graham Way"
                },
                CountryCode = "CA",
                Country = new Country { CountryCode = "CA", CountryName = "Canada", FederalTaxRate = 0.05m },
                ProvinceCode = "ON",
                Province = new Province { CountryCode = "CA", ProvinceCode = "ON", ProvincialTaxRate = 0.08m },
                MemberId = memberId,
                Id = addressId
            };

            List<MemberAddress> addresses = new List<MemberAddress> { address };

            Mock<IVeilDataAccess> dbMock = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbMock, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithMember(dbMock, member);
            SetupVeilDataAccessWithUser(dbMock, memberUser);
            Mock<DbSet<MemberAddress>> addressDbMock = TestHelpers.GetFakeAsyncDbSet(addresses.AsQueryable());
            addressDbMock.SetupForInclude();

            dbMock.
                Setup(db => db.MemberAddresses).
                Returns(addressDbMock.Object).
                Verifiable();

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(details);
            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();
            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.GetLast4ForToken(It.IsAny<string>())).
                Returns<string>(null); // End the test early by returning null

            CheckoutController controller = CreateCheckoutController(dbMock.Object, context: contextStub.Object, shippingCostService: shippingServiceStub.Object, stripeService: stripeServiceStub.Object);

            await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList());

            Assert.That(
                () =>
                    dbMock.Verify(db => db.MemberAddresses,
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public void PlaceOrder_AddressIsUnsaved_DoesNotTouchDb()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, GetCountries());
            SetupVeilDataAccessWithProvincesSetupForInclude(dbStub, GetProvinceList(validNotSavedShippingBillingDetails));

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.GetLast4ForToken(It.IsAny<string>())).
                Returns<string>(null);

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingBillingDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object, stripeService: stripeServiceStub.Object);

            Assert.That(
                async () => await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList()),
                Throws.Nothing,
                "MemberAddresses isn't set up as it shouldn't be touched by this test");
        }

        [Test]
        public async void PlaceOrder_CardIsIdButIdNotInDb_RedirectsToBillingInfo()
        {
            WebOrderCheckoutDetails details = new WebOrderCheckoutDetails
            {
                MemberAddressId = addressId,
                MemberCreditCardId = creditCardId
            };

            member.CreditCards = new List<MemberCreditCard>();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(details);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList()) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.BillingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo(null));
        }

        [Test]
        public async void PlaceOrder_CardIsId_GetsCardFromDb()
        {
            WebOrderCheckoutDetails details = new WebOrderCheckoutDetails
            {
                MemberAddressId = addressId,
                MemberCreditCardId = creditCardId
            };

            member.CreditCards = new List<MemberCreditCard>();

            Mock<IVeilDataAccess> dbMock = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbMock, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithUser(dbMock, memberUser);
            SetupVeilDataAccessWithAddresses(dbMock, GetMemberAddresses());
            Mock<DbSet<Member>> memberDbSet = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());

            dbMock.
                Setup(db => db.Members).
                Returns(memberDbSet.Object).
                Verifiable();

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(details);
            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(dbMock.Object, context: contextStub.Object, shippingCostService: shippingServiceStub.Object);

            await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList());

            Assert.That(
                () =>
                    dbMock.Verify(db => db.Members,
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void PlaceOrder_CardIsToken_CallsStripeServiceGetLast4ForToken()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, GetCountries());
            SetupVeilDataAccessWithProvincesSetupForInclude(dbStub, GetProvinceList(validNotSavedShippingBillingDetails));

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingBillingDetails);

            Mock<IStripeService> stripeServiceMock = new Mock<IStripeService>();
            stripeServiceMock.
                Setup(s => s.GetLast4ForToken(It.IsAny<string>())).
                Returns<string>(null).
                Verifiable();

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object, stripeService: stripeServiceMock.Object);

            await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList());

            Assert.That(
                () =>
                    stripeServiceMock.Verify(s => s.GetLast4ForToken(validNotSavedShippingBillingDetails.StripeCardToken),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void PlaceOrder_CardIsTokenAndStripeExceptionThrown_RedirectsToBillingInfo()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, GetCountries());
            SetupVeilDataAccessWithProvincesSetupForInclude(dbStub, GetProvinceList(validNotSavedShippingBillingDetails));

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingBillingDetails);

            Mock<IStripeService> stripeServiceMock = new Mock<IStripeService>();
            stripeServiceMock.
                Setup(s => s.GetLast4ForToken(It.IsAny<string>())).
                Throws(new StripeServiceException("message", StripeExceptionType.UnknownError));

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object, stripeService: stripeServiceMock.Object);

            var result = await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList()) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.BillingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.Null);
        }

        [Test]
        public void PlaceOrder_CardIsTokenAndStripeApiKeyExceptionThrown_ThrowsInternalServerError()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, GetCountries());
            SetupVeilDataAccessWithProvincesSetupForInclude(dbStub, GetProvinceList(validNotSavedShippingBillingDetails));

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingBillingDetails);

            Mock<IStripeService> stripeServiceMock = new Mock<IStripeService>();
            stripeServiceMock.
                Setup(s => s.GetLast4ForToken(It.IsAny<string>())).
                Throws(new StripeServiceException("message", StripeExceptionType.ApiKeyError));

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object, stripeService: stripeServiceMock.Object);

            Assert.That(async () => await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList()),
                Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() >= (int)HttpStatusCode.InternalServerError));
        }

        [Test]
        public async void PlaceOrder_ValidState_GetsOnlineWarehouseProductLocationInventoryForEachCartItem()
        {
            cartWithNewAndUsed.Items.Add(newProduct2CartItem);

            Mock<IVeilDataAccess> dbMock = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbMock, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbMock, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbMock, member);
            Mock<DbSet<ProductLocationInventory>> inventoryDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(GetProductInventories(10, 10).AsQueryable());

            dbMock.
                Setup(db => db.ProductLocationInventories).
                Returns(inventoryDbSetStub.Object).
                Verifiable();
            
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);

            Mock<IStripeService> stripeServiceMock = new Mock<IStripeService>();
            stripeServiceMock.
                Setup(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).
                Throws(new StripeServiceException("message", StripeExceptionType.UnknownError)); // Throw to end execution early

            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(
                dbMock.Object,
                context: contextStub.Object,
                stripeService: stripeServiceMock.Object,
                shippingCostService: shippingServiceStub.Object);

            await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList());

            Assert.That(
                () => 
                    dbMock.Verify(db => db.ProductLocationInventories,
                    Times.Exactly(cartWithNewAndUsed.Items.GroupBy(i => i.ProductId).Count()), 
                    "Should be called once per ProductId in the cart"),
                Throws.Nothing);
        }

        [Test]
        public async void PlaceOrder_NewItemUnrestrictedAvailability_DecreasesInventoryLevelByQuantity(
            [Values(AvailabilityStatus.Available, AvailabilityStatus.PreOrder)]AvailabilityStatus status,
            [Values(1, 10, 50)]int quantity)
        {
            newProduct1CartItem.Quantity = quantity;
            cartProduct1.ProductAvailabilityStatus = status;

            ProductLocationInventory inventory = new ProductLocationInventory
            {
                ProductId = cartProduct1Id,
                Product = cartProduct1,
                Location = onlineWarehouse,
                NewOnHand = 10,
                UsedOnHand = 10
            };

            Cart cart = new Cart
            {
                Items = new List<CartItem>
                {
                    newProduct1CartItem
                },
                MemberId = memberId,
                Member = member
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, new List<Cart> { cart });
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);
            Mock<DbSet<ProductLocationInventory>> inventoryDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(new List<ProductLocationInventory> { inventory }.AsQueryable());

            dbStub.
                Setup(db => db.ProductLocationInventories).
                Returns(inventoryDbSetStub.Object);

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).
                Throws(new StripeServiceException("message", StripeExceptionType.UnknownError)); // Throw to end execution early

            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(
                dbStub.Object,
                context: contextStub.Object,
                stripeService: stripeServiceStub.Object,
                shippingCostService: shippingServiceStub.Object);

            await controller.PlaceOrder(cart.Items.ToList());

            Assert.That(inventory.NewOnHand, Is.EqualTo(10 - newProduct1CartItem.Quantity));
        }

        [Test]
        public async void PlaceOrder_NewItemDiscontinuedAvailabilityWithEnoughNewOnHand_DecreasesInventoryLevelByQuantity(
            [Values(AvailabilityStatus.DiscontinuedByManufacturer, AvailabilityStatus.NotForSale)]AvailabilityStatus status,
            [Values(1, 10)]int quantity)
        {
            newProduct1CartItem.Quantity = quantity;
            cartProduct1.ProductAvailabilityStatus = status;

            ProductLocationInventory inventory = new ProductLocationInventory
            {
                ProductId = cartProduct1Id,
                Product = cartProduct1,
                Location = onlineWarehouse,
                NewOnHand = 10,
                UsedOnHand = 10
            };

            Cart cart = new Cart
            {
                Items = new List<CartItem>
                {
                    newProduct1CartItem
                },
                MemberId = memberId,
                Member = member
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, new List<Cart> { cart });
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);
            Mock<DbSet<ProductLocationInventory>> inventoryDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(new List<ProductLocationInventory> { inventory }.AsQueryable());

            dbStub.
                Setup(db => db.ProductLocationInventories).
                Returns(inventoryDbSetStub.Object);

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).
                Throws(new StripeServiceException("message", StripeExceptionType.UnknownError)); // Throw to end execution early

            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(
                dbStub.Object,
                context: contextStub.Object,
                stripeService: stripeServiceStub.Object,
                shippingCostService: shippingServiceStub.Object);

            await controller.PlaceOrder(cart.Items.ToList());

            Assert.That(inventory.NewOnHand, Is.EqualTo(10 - newProduct1CartItem.Quantity));
        }

        [TestCase(AvailabilityStatus.DiscontinuedByManufacturer)]
        [TestCase(AvailabilityStatus.NotForSale)]
        public async void PlaceOrder_NewItemDiscontinuedAvailabilityWithoutEnoughNewOnHand_RedirectsToConfirmOrder(AvailabilityStatus status)
        {
            newProduct1CartItem.Quantity = 11;
            cartProduct1.ProductAvailabilityStatus = status;

            ProductLocationInventory inventory = new ProductLocationInventory
            {
                ProductId = cartProduct1Id,
                Product = cartProduct1,
                Location = onlineWarehouse,
                NewOnHand = 10,
                UsedOnHand = 10
            };

            Cart cart = new Cart
            {
                Items = new List<CartItem>
                {
                    newProduct1CartItem
                },
                MemberId = memberId,
                Member = member
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, new List<Cart> { cart });
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);
            Mock<DbSet<ProductLocationInventory>> inventoryDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(new List<ProductLocationInventory> { inventory }.AsQueryable());

            dbStub.
                Setup(db => db.ProductLocationInventories).
                Returns(inventoryDbSetStub.Object);

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).
                Throws(new StripeServiceException("message", StripeExceptionType.UnknownError)); // Throw to end execution early

            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(
                dbStub.Object,
                context: contextStub.Object,
                stripeService: stripeServiceStub.Object,
                shippingCostService: shippingServiceStub.Object);

            var result = await controller.PlaceOrder(cart.Items.ToList()) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.ConfirmOrder)));
            Assert.That(result.RouteValues["Controller"], Is.Null);

            Assert.That(inventory.NewOnHand, Is.EqualTo(10));
        }

        [TestCase(AvailabilityStatus.Available)]
        [TestCase(AvailabilityStatus.PreOrder)]
        [TestCase(AvailabilityStatus.DiscontinuedByManufacturer)]
        [TestCase(AvailabilityStatus.NotForSale)]
        public async void PlaceOrder_UsedWithHigherQuantityThanUsedOnHand_RedirectsToConfirmOrder(AvailabilityStatus status)
        {
            usedProduct1CartItem.Quantity = 11;
            cartProduct1.ProductAvailabilityStatus = status;

            ProductLocationInventory inventory = new ProductLocationInventory
            {
                ProductId = cartProduct1Id,
                Product = cartProduct1,
                Location = onlineWarehouse,
                NewOnHand = 10,
                UsedOnHand = 10
            };

            Cart cart = new Cart
            {
                Items = new List<CartItem>
                {
                    usedProduct1CartItem
                },
                MemberId = memberId,
                Member = member
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, new List<Cart> { cart });
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);
            Mock<DbSet<ProductLocationInventory>> inventoryDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(new List<ProductLocationInventory> { inventory }.AsQueryable());

            dbStub.
                Setup(db => db.ProductLocationInventories).
                Returns(inventoryDbSetStub.Object);

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).
                Throws(new StripeServiceException("message", StripeExceptionType.UnknownError)); // Throw to end execution early

            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(
                dbStub.Object,
                context: contextStub.Object,
                stripeService: stripeServiceStub.Object,
                shippingCostService: shippingServiceStub.Object);

            var result = await controller.PlaceOrder(cart.Items.ToList()) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.ConfirmOrder)));
            Assert.That(result.RouteValues["Controller"], Is.Null);

            Assert.That(inventory.UsedOnHand, Is.EqualTo(10));
        }

        [Test]
        public async void PlaceOrder_UsedWithQuantityLessThanOrEqualToUsedOnHand_DecreasesInventoryLevelByQuantity(
            [Values(AvailabilityStatus.Available,
                    AvailabilityStatus.PreOrder,
                    AvailabilityStatus.DiscontinuedByManufacturer,
                    AvailabilityStatus.NotForSale)] AvailabilityStatus status,
            [Values(1, 10)]int quantity)
        {
            usedProduct1CartItem.Quantity = quantity;
            cartProduct1.ProductAvailabilityStatus = status;

            ProductLocationInventory inventory = new ProductLocationInventory
            {
                ProductId = cartProduct1Id,
                Product = cartProduct1,
                Location = onlineWarehouse,
                NewOnHand = 10,
                UsedOnHand = 10
            };

            Cart cart = new Cart
            {
                Items = new List<CartItem>
                {
                    usedProduct1CartItem
                },
                MemberId = memberId,
                Member = member
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, new List<Cart> { cart });
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);
            Mock<DbSet<ProductLocationInventory>> inventoryDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(new List<ProductLocationInventory> { inventory }.AsQueryable());

            dbStub.
                Setup(db => db.ProductLocationInventories).
                Returns(inventoryDbSetStub.Object);

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).
                Throws(new StripeServiceException("message", StripeExceptionType.UnknownError)); // Throw to end execution early

            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(
                dbStub.Object,
                context: contextStub.Object,
                stripeService: stripeServiceStub.Object,
                shippingCostService: shippingServiceStub.Object);

            await controller.PlaceOrder(cart.Items.ToList());

            Assert.That(inventory.UsedOnHand, Is.EqualTo(10 - usedProduct1CartItem.Quantity));
        }

        [Test]
        public async void PlaceOrder_ValidInventoryLevels_CallsStripeServiceChargeCard()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);
            SetupVeilDataAccessWithInventoriesForBothCartProducts(dbStub);

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);

            Mock<IStripeService> stripeServiceMock = new Mock<IStripeService>();
            stripeServiceMock.
                Setup(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).
                Throws(new StripeServiceException("message", StripeExceptionType.UnknownError)).
                Verifiable(); // Throw to end execution early

            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(
                dbStub.Object,
                context: contextStub.Object,
                stripeService: stripeServiceMock.Object,
                shippingCostService: shippingServiceStub.Object);

            await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList());

            Assert.That(
                () => 
                    stripeServiceMock.Verify(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void PlaceOrder_ValidInventoryLevelsAndSavedCard_CallsStripeServiceChargeCardWithRetrievedCustomerIdAndCardToken()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);
            SetupVeilDataAccessWithInventoriesForBothCartProducts(dbStub);

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);

            Mock<IStripeService> stripeServiceMock = new Mock<IStripeService>();
            stripeServiceMock.
                Setup(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).
                Throws(new StripeServiceException("message", StripeExceptionType.UnknownError)).
                Verifiable(); // Throw to end execution early

            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(
                dbStub.Object,
                context: contextStub.Object,
                stripeService: stripeServiceMock.Object,
                shippingCostService: shippingServiceStub.Object);

            await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList());

            Assert.That(
                () =>
                    stripeServiceMock.Verify(s => s.ChargeCard(It.IsAny<decimal>(), memberCreditCard.StripeCardId, member.StripeCustomerId, It.IsAny<string>()),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void PlaceOrder_ValidInventoryLevelsAndNotSavedCard_CallsStripeServiceChargeCardWithNullCustomerId()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);
            SetupVeilDataAccessWithInventoriesForBothCartProducts(dbStub);

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);

            Mock<IStripeService> stripeServiceMock = new Mock<IStripeService>();
            stripeServiceMock.
                Setup(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).
                Throws(new StripeServiceException("message", StripeExceptionType.UnknownError)).
                Verifiable(); // Throw to end execution early

            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(
                dbStub.Object,
                context: contextStub.Object,
                stripeService: stripeServiceMock.Object,
                shippingCostService: shippingServiceStub.Object);

            await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList());

            Assert.That(
                () =>
                    stripeServiceMock.Verify(s => s.ChargeCard(It.IsAny<decimal>(), memberCreditCard.StripeCardId, null, It.IsAny<string>()),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void PlaceOrder_ValidInventoryLevels_CallsStripeServiceChargeCardWithCorrectOrderTotal()
        {
            decimal shippingCost = 8.0m;
            decimal correctOrderTotal = 87.1m; /* 60 + 10 * (1.13) + 8.0 */

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);
            SetupVeilDataAccessWithInventoriesForBothCartProducts(dbStub);

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);

            Mock<IStripeService> stripeServiceMock = new Mock<IStripeService>();
            stripeServiceMock.
                Setup(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).
                Throws(new StripeServiceException("message", StripeExceptionType.UnknownError)).
                Verifiable(); // Throw to end execution early

            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();
            shippingServiceStub.Setup(
                s => s.CalculateShippingCost(It.IsAny<decimal>(), It.IsAny<ICollection<CartItem>>())).
                Returns(shippingCost);

            CheckoutController controller = CreateCheckoutController(
                dbStub.Object,
                context: contextStub.Object,
                stripeService: stripeServiceMock.Object,
                shippingCostService: shippingServiceStub.Object);

            await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList());

            Assert.That(
                () =>
                    stripeServiceMock.Verify(s => s.ChargeCard(correctOrderTotal, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public void PlaceOrder_CardIsToken_DoesNotTouchMemberCreditCardDbSet()
        {
            member.CreditCards = null;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);
            SetupVeilDataAccessWithInventoriesForBothCartProducts(dbStub);
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, GetCountries());
            SetupVeilDataAccessWithProvincesSetupForInclude(dbStub, GetProvinceList(validNotSavedShippingBillingDetails));

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingBillingDetails);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.GetLast4ForToken(It.IsAny<string>())).
                Returns("4242");
            stripeServiceStub.
                Setup(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).
                Throws(new StripeServiceException("message", StripeExceptionType.UnknownError)); // Throw to end execution early

            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();
            shippingServiceStub.
                Setup(s => s.CalculateShippingCost(It.IsAny<decimal>(), It.IsAny<ICollection<CartItem>>())).
                Returns(0m);

            CheckoutController controller = CreateCheckoutController(
                dbStub.Object,
                context: contextStub.Object,
                stripeService: stripeServiceStub.Object,
                shippingCostService: shippingServiceStub.Object);

            Assert.That(
                async () => await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList()),
                Throws.Nothing,
                "This test should not touch the member's credit cards." +
                " It has been set up to throw when trying to select from member.CreditCards by setting that to null");
        }

        [Test]
        public async void PlaceOrder_StripeChargeCardThrowsCardError_AddsCardErrorMessageToAlertMessages()
        {
            string stripeErrorMessage = "A card error message";

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);
            SetupVeilDataAccessWithInventoriesForBothCartProducts(dbStub);

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);

            StripeServiceException exception = new StripeServiceException(stripeErrorMessage, StripeExceptionType.CardError);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).
                Throws(exception);

            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(
                dbStub.Object,
                context: contextStub.Object,
                stripeService: stripeServiceStub.Object,
                shippingCostService: shippingServiceStub.Object);

            await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList());

            Assert.That(controller.TempData[AlertHelper.ALERT_MESSAGE_KEY], Has.Some.Matches<AlertMessage>(am => am.Message == stripeErrorMessage));
        }

        [Test]
        public async void PlaceOrder_ValidInventoryLevelsAndStripeChargeCardExceptionThrow_RedirectsToConfirmOrder()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);
            SetupVeilDataAccessWithInventoriesForBothCartProducts(dbStub);

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).
                Throws(new StripeServiceException("message", StripeExceptionType.UnknownError));

            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(
                dbStub.Object,
                context: contextStub.Object,
                stripeService: stripeServiceStub.Object,
                shippingCostService: shippingServiceStub.Object);

            var result = await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList()) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.ConfirmOrder)));
            Assert.That(result.RouteValues["Controller"], Is.Null);
        }
        
        [Test]
        public async void PlaceOrder_SuccessfulCharge_AddsNewOrderToWebOrdersDbSet()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);
            SetupVeilDataAccessWithInventoriesForBothCartProducts(dbStub);
            Mock<DbSet<WebOrder>> webOrderDbSetMock = TestHelpers.GetFakeAsyncDbSet(new List<WebOrder>().AsQueryable());
            webOrderDbSetMock.
                Setup(wdb => wdb.Add(It.IsAny<WebOrder>())).
                Returns<WebOrder>(val => val).
                Verifiable();

            dbStub.
                Setup(db => db.WebOrders).
                Returns(webOrderDbSetMock.Object);
            dbStub.
                Setup(db => db.SaveChangesAsync()).
                Throws<DataException>(); // Throw to end early

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(
                dbStub.Object,
                context: contextStub.Object,
                stripeService: stripeServiceStub.Object,
                shippingCostService: shippingServiceStub.Object);

            await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList());

            Assert.That(
                () => 
                    webOrderDbSetMock.Verify(wdb => wdb.Add(It.IsAny<WebOrder>()),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void PlaceOrder_SuccessfulCharge_AddsAllDetailsToWebOrder()
        {
            string stripeChargeId = "chargeToken";
            decimal subTotal = 70.0m; /* 60 (new) + 10 (used) */
            decimal shippingCost = 8.0m;
            decimal taxAmount = 9.1m; /* 70 * 0.13 */
            int itemCount = 2;

            List<WebOrder> webOrders = new List<WebOrder>();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);
            SetupVeilDataAccessWithInventoriesForBothCartProducts(dbStub);
            SetupVeilDataAccessWithWebOrders(dbStub, webOrders);

            dbStub.
                Setup(db => db.SaveChangesAsync()).
                Throws<DataException>(); // Throw to end early

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).
                Returns(stripeChargeId);

            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();
            shippingServiceStub.
                Setup(s => s.CalculateShippingCost(It.IsAny<decimal>(), It.IsAny<ICollection<CartItem>>())).
                Returns(shippingCost);

            CheckoutController controller = CreateCheckoutController(
                dbStub.Object,
                context: contextStub.Object,
                stripeService: stripeServiceStub.Object,
                shippingCostService: shippingServiceStub.Object);

            await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList());

            WebOrder newOrder = webOrders.FirstOrDefault();

            Assert.That(newOrder != null);
            Assert.That(newOrder.StripeChargeId, Is.EqualTo(stripeChargeId));
            Assert.That(newOrder.Address, Is.EqualTo(memberAddress.Address));
            Assert.That(newOrder.CountryCode, Is.EqualTo(memberAddress.CountryCode));
            Assert.That(newOrder.ProvinceCode, Is.EqualTo(memberAddress.ProvinceCode));
            Assert.That(newOrder.CreditCardLast4Digits, Is.EqualTo(memberCreditCard.Last4Digits));
            Assert.That(newOrder.MemberId, Is.EqualTo(memberId));
            Assert.That(newOrder.OrderDate, Is.EqualTo(DateTime.Now).Within(1).Minutes);
            Assert.That(newOrder.OrderStatus, Is.EqualTo(OrderStatus.PendingProcessing));
            Assert.That(newOrder.OrderSubtotal, Is.EqualTo(subTotal));
            Assert.That(newOrder.ShippingCost, Is.EqualTo(shippingCost));
            Assert.That(newOrder.TaxAmount, Is.EqualTo(taxAmount));
            Assert.That(newOrder.OrderItems, Has.Count.EqualTo(itemCount));
            Assert.That(newOrder.OrderItems, 
                Has.Exactly(1).Matches<OrderItem>(vm =>
                    vm.IsNew == newProduct1CartItem.IsNew &&
                    vm.ListPrice == newProduct1CartItem.Product.NewWebPrice &&
                    vm.ProductId == newProduct1CartItem.ProductId &&
                    vm.Quantity == newProduct1CartItem.Quantity).
                And.Exactly(1).Matches<OrderItem>(vm =>
                    vm.IsNew == usedProduct1CartItem.IsNew &&
                    vm.ListPrice == usedProduct1CartItem.Product.UsedWebPrice &&
                    vm.ProductId == usedProduct1CartItem.ProductId &&
                    vm.Quantity == usedProduct1CartItem.Quantity));
        }

        [Test]
        public async void PlaceOrder_SuccessfulCharge_ClearsCart()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);
            SetupVeilDataAccessWithInventoriesForBothCartProducts(dbStub);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>());

            dbStub.
                Setup(db => db.SaveChangesAsync()).
                Throws<DataException>(); // Throw to end early

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(
                dbStub.Object,
                context: contextStub.Object,
                stripeService: stripeServiceStub.Object,
                shippingCostService: shippingServiceStub.Object);

            Assert.That(cartWithNewAndUsed.Items, Is.Not.Empty,
                "Cart can't be empty at this point for the test to be valid");

            await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList());

            Assert.That(cartWithNewAndUsed.Items, Is.Empty);
        }

        [Test]
        public async void PlaceOrder_SuccessfulCharge_CallsSaveChangesAsync()
        {
            Mock<IVeilDataAccess> dbMock = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbMock, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbMock, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbMock, member);
            SetupVeilDataAccessWithInventoriesForBothCartProducts(dbMock);
            SetupVeilDataAccessWithWebOrders(dbMock, new List<WebOrder>());

            dbMock.
                Setup(db => db.SaveChangesAsync()).
                Throws<DataException>(). // Throw to end early
                Verifiable();

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(
                dbMock.Object,
                context: contextStub.Object,
                stripeService: stripeServiceStub.Object,
                shippingCostService: shippingServiceStub.Object);

            await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList());

            Assert.That(
                () => 
                    dbMock.Verify(db => db.SaveChangesAsync(), 
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void PlaceOrder_SaveChangesThrows_CallsStripeServiceRefundChargeWithReturnedChargeId()
        {
            string stripeChargeId = "chargeToken";

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);
            SetupVeilDataAccessWithInventoriesForBothCartProducts(dbStub);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>());

            dbStub.
                Setup(db => db.SaveChangesAsync()).
                Throws<DataException>();

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);

            Mock<IStripeService> stripeServiceMock = new Mock<IStripeService>();
            stripeServiceMock.
                Setup(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).
                Returns(stripeChargeId);
            stripeServiceMock.
                Setup(s => s.RefundCharge(It.IsAny<string>())).
                Verifiable();

            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(
                dbStub.Object,
                context: contextStub.Object,
                stripeService: stripeServiceMock.Object,
                shippingCostService: shippingServiceStub.Object);

            await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList());

            Assert.That(
                () =>
                    stripeServiceMock.Verify(s => s.RefundCharge(stripeChargeId),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void PlaceOrder_SaveChangesThrowsThenRefundThrows_AddsErrorMessageToAlerts()
        {
            string stripeChargeId = "chargeToken";
            string stripeErrorMessage = "a stripe error message";

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);
            SetupVeilDataAccessWithInventoriesForBothCartProducts(dbStub);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>());

            dbStub.
                Setup(db => db.SaveChangesAsync()).
                Throws<DataException>();

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);

            Mock<IStripeService> stripeServiceMock = new Mock<IStripeService>();
            stripeServiceMock.
                Setup(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).
                Returns(stripeChargeId);
            stripeServiceMock.
                Setup(s => s.RefundCharge(It.IsAny<string>())).
                Throws(new StripeServiceException(stripeErrorMessage, StripeExceptionType.UnknownError));

            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(
                dbStub.Object,
                context: contextStub.Object,
                stripeService: stripeServiceMock.Object,
                shippingCostService: shippingServiceStub.Object);

            await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList());

            Assert.That(controller.TempData[AlertHelper.ALERT_MESSAGE_KEY], Has.Some.Matches<AlertMessage>(am => am.Message == stripeErrorMessage));
        }

        [Test]
        public async void PlaceOrder_SaveChangesThrows_RedirectsToConfirmOrder()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);
            SetupVeilDataAccessWithInventoriesForBothCartProducts(dbStub);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>());

            dbStub.
                Setup(db => db.SaveChangesAsync()).
                Throws<DataException>();

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(
                dbStub.Object,
                context: contextStub.Object,
                stripeService: stripeServiceStub.Object,
                shippingCostService: shippingServiceStub.Object);

            var result = await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList()) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.ConfirmOrder)));
            Assert.That(result.RouteValues["Controller"], Is.Null);
        }

        [Test]
        public async void PlaceOrder_SaveSuccessful_RemovesOrderDetailsFromSession()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessUserStore(dbStub);
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);
            SetupVeilDataAccessWithInventoriesForBothCartProducts(dbStub);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>());

            dbStub.
                Setup(db => db.SaveChangesAsync()).
                ReturnsAsync(1);

            SetupViewEngineStub();
            Mock<VeilUserManager> userManagerStub = GetUserManageStubWithSendEmailSetup(dbStub.Object);

            Mock<ControllerContext> contextMock = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();
            contextMock.
                SetupGet(c => c.HttpContext.Session[CheckoutController.OrderCheckoutDetailsKey]).
                Returns(validSavedShippingBillingDetails);
            contextMock.
                Setup(c => c.HttpContext.Session.Remove(It.IsAny<string>())).
                Verifiable();

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(
                dbStub.Object,
                context: contextMock.Object,
                stripeService: stripeServiceStub.Object,
                shippingCostService: shippingServiceStub.Object,
                userManager: userManagerStub.Object);

            await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList());

            Assert.That(
                () => 
                    contextMock.Verify(c => c.HttpContext.Session.Remove(CheckoutController.OrderCheckoutDetailsKey),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void PlaceOrder_SaveSuccessful_NullsCartQuantityInSession()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessUserStore(dbStub);
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);
            SetupVeilDataAccessWithInventoriesForBothCartProducts(dbStub);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>());

            dbStub.
                Setup(db => db.SaveChangesAsync()).
                ReturnsAsync(1);

            SetupViewEngineStub();
            Mock<VeilUserManager> userManagerStub = GetUserManageStubWithSendEmailSetup(dbStub.Object);

            Mock<ControllerContext> contextMock = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();
            contextMock.
                SetupGet(c => c.HttpContext.Session[CheckoutController.OrderCheckoutDetailsKey]).
                Returns(validSavedShippingBillingDetails);
            contextMock.
                SetupSet(c => c.HttpContext.Session[CartController.CART_QTY_SESSION_KEY] = It.IsAny<object>()).
                Verifiable();

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(
                dbStub.Object,
                context: contextMock.Object,
                stripeService: stripeServiceStub.Object,
                shippingCostService: shippingServiceStub.Object,
                userManager: userManagerStub.Object);

            await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList());

            Assert.That(
                () =>
                    contextMock.VerifySet(c => c.HttpContext.Session[CartController.CART_QTY_SESSION_KEY] = null,
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void PlaceOrder_SaveSuccessful_CallsUserManageSendEmailAsync()
        {
            string viewStringResult = "The body";

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessUserStore(dbStub);
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);
            SetupVeilDataAccessWithInventoriesForBothCartProducts(dbStub);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>());

            dbStub.
                Setup(db => db.SaveChangesAsync()).
                ReturnsAsync(1);

            SetupViewEngineStub(viewString: viewStringResult);
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerStub.
                Setup(um => um.SendEmailAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>())).
                Returns(Task.FromResult(0)).
                Verifiable();

            Mock<ControllerContext> contextMock = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(
                dbStub.Object,
                context: contextMock.Object,
                stripeService: stripeServiceStub.Object,
                shippingCostService: shippingServiceStub.Object,
                userManager: userManagerStub.Object);

            await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList());

            Assert.That(
                () =>
                    userManagerStub.Verify(um => um.SendEmailAsync(memberId, It.IsAny<string>(), viewStringResult),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void PlaceOrder_SaveSuccessful_RedirectsToHomeIndex()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessUserStore(dbStub);
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);
            SetupVeilDataAccessWithInventoriesForBothCartProducts(dbStub);
            SetupVeilDataAccessWithWebOrders(dbStub, new List<WebOrder>());

            dbStub.
                Setup(db => db.SaveChangesAsync()).
                ReturnsAsync(1);

            SetupViewEngineStub();
            Mock<VeilUserManager> userManagerStub = GetUserManageStubWithSendEmailSetup(dbStub.Object);
            Mock<ControllerContext> contextMock = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.ChargeCard(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(
                dbStub.Object,
                context: contextMock.Object,
                stripeService: stripeServiceStub.Object,
                shippingCostService: shippingServiceStub.Object,
                userManager: userManagerStub.Object);

            var result = await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList()) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(HomeController.Index)));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Home"));
        }
    }
}
