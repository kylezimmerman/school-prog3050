using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Exceptions;
using Veil.Extensions;
using Veil.Models;
using Veil.Services.Interfaces;

namespace Veil.Tests.Controllers.CheckoutControllerTests
{
    public class ConfirmOrderTests : CheckoutControllerTestsBase
    {
        [Test]
        public async void ConfirmOrder_EmptyCart_RedirectsToCartIndex()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, new List<Cart>());

            CheckoutController controller = CreateCheckoutController(dbStub.Object);

            var result = await controller.ConfirmOrder() as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Index"));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Cart"));
        }

        [Test]
        public async void ConfirmOrder_AddressNotSetInSession_RedirectsToShippingInfo()
        {
            WebOrderCheckoutDetails checkoutDetails = new WebOrderCheckoutDetails();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(checkoutDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.ConfirmOrder() as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.ShippingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo(null));
        }

        [Test]
        public async void ConfirmOrder_BillingInfoNotSetInsession_RedirectsToBillingInfo()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, new List<MemberAddress> { new MemberAddress { Id = addressId } });
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.ConfirmOrder() as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.BillingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo(null));
        }

        [Test]
        public async void ConfirmOrder_NullSessionOrderDetails_RedirectsToShippingInfo()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(null);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.ConfirmOrder() as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.ShippingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo(null));
        }

        [Test]
        public async void ConfirmOrder_AddressIsIdButIdNotInDb_RedirectsToShippingInfo()
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

            var result = await controller.ConfirmOrder() as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.ShippingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo(null));
        }

        [Test]
        public void ConfirmOrder_AddressIsUnsaved_DoesNotTouchMemberAddressDbSet()
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

            Assert.That(async () => await controller.ConfirmOrder(), Throws.Nothing);
        }

        [Test]
        public async void ConfirmOrder_CardIsIdButIdNotInDb_RedirectsToBillingInfo()
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

            var result = await controller.ConfirmOrder() as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.BillingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo(null));
        }

        [Test]
        public async void ConfirmOrder_CardIsToken_CallsStripeServiceGetLast4ForToken()
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

            await controller.ConfirmOrder();

            Assert.That(
                () => 
                    stripeServiceMock.Verify(s => s.GetLast4ForToken(validNotSavedShippingBillingDetails.StripeCardToken),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void ConfirmOrder_CardIsTokenAndStripeExceptionThrown_RedirectsToBillingInfo()
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

            var result = await controller.ConfirmOrder() as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.BillingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.Null);
        }

        [Test]
        public void ConfirmOrder_CardIsTokenAndStripeApiKeyExceptionThrown_ThrowsInternalServerError()
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
            
            Assert.That(async () => await controller.ConfirmOrder(), 
                Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() >= (int)HttpStatusCode.InternalServerError));
        }

        [Test]
        public async void ConfirmOrder_ValidState_LoadsCartWithProductsIncluded()
        {
            member.CreditCards = new List<MemberCreditCard> { memberCreditCard };

            Mock<IVeilDataAccess> dbMock = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbMock, memberUser);
            SetupVeilDataAccessWithMember(dbMock, member);
            SetupVeilDataAccessWithAddresses(dbMock, GetMemberAddresses());
            Mock<DbSet<Cart>> cartDbSetMock = TestHelpers.GetFakeAsyncDbSet(GetCartsListContainingCartWithNewAndUsed().AsQueryable());
            cartDbSetMock.
                Setup(cdb => cdb.Include(It.IsAny<string>())).
                Returns(cartDbSetMock.Object).
                Verifiable();
            dbMock.
                Setup(db => db.Carts).
                Returns(cartDbSetMock.Object).
                Verifiable();

            Mock<IShippingCostService> shippingCostStub = new Mock<IShippingCostService>();
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);

            CheckoutController controller = CreateCheckoutController(dbMock.Object, context: contextStub.Object, shippingCostService: shippingCostStub.Object);

            await controller.ConfirmOrder();

            Assert.That(
                () =>
                    dbMock.Verify(db => db.Carts,
                    Times.Exactly(2),
                    "Once for ensuring cart isn't empty, once for getting the cart"),
                Throws.Nothing);

           Assert.That(
                () => 
                    cartDbSetMock.Verify(cdb => cdb.Include("Items.Product"),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void ConfirmOrder_ValidState_CallsShippingCostServiceCalculateShippingCost()
        {
            Cart cart = GetCartsListContainingCartWithNewAndUsed().First();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithMember(dbStub, member);
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());

            Mock<IShippingCostService> shippingCostMock = new Mock<IShippingCostService>();
            shippingCostMock.
                Setup(sc => sc.CalculateShippingCost(It.IsAny<decimal>(), It.IsAny<ICollection<CartItem>>())).
                Returns(0m).
                Verifiable();

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object, shippingCostService: shippingCostMock.Object);

            await controller.ConfirmOrder();

            Assert.That(
                () =>
                    shippingCostMock.Verify(sc => sc.CalculateShippingCost(cart.TotalCartItemsPrice, cart.Items),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void ConfirmOrder_AddressIsId_GetsAddressFromDb()
        {
            WebOrderCheckoutDetails details = new WebOrderCheckoutDetails
            {
                MemberAddressId = addressId,
                MemberCreditCardId = creditCardId
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

            CheckoutController controller = CreateCheckoutController(dbMock.Object, context: contextStub.Object, shippingCostService: shippingServiceStub.Object);

            var result = await controller.ConfirmOrder() as ViewResult;

            Assert.That(
                () => 
                    dbMock.Verify(db => db.MemberAddresses,
                    Times.Once),
                Throws.Nothing);

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<ConfirmOrderViewModel>());

            var model = (ConfirmOrderViewModel) result.Model;

            Assert.That(model.Address, Is.SameAs(address.Address));
            Assert.That(model.CountryName, Is.SameAs(address.Country.CountryName));
            Assert.That(model.ProvinceName, Is.SameAs(address.Province.Name));
        }

        [Test]
        public async void ConfirmOrder_CardIsId_GetsCardFromDb()
        {
            WebOrderCheckoutDetails details = new WebOrderCheckoutDetails
            {
                MemberAddressId = addressId,
                MemberCreditCardId = creditCardId
            };

            Member currentMember = member;
            member.CreditCards = new List<MemberCreditCard> { memberCreditCard };

            Mock<IVeilDataAccess> dbMock = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbMock, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithUser(dbMock, memberUser);
            SetupVeilDataAccessWithAddresses(dbMock, GetMemberAddresses());
            Mock<DbSet<Member>> memberDbSet = TestHelpers.GetFakeAsyncDbSet(new List<Member> { currentMember }.AsQueryable());

            dbMock.
                Setup(db => db.Members).
                Returns(memberDbSet.Object).
                Verifiable();

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(details);
            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(dbMock.Object, context: contextStub.Object, shippingCostService: shippingServiceStub.Object);

            var result = await controller.ConfirmOrder() as ViewResult;

            Assert.That(
                () =>
                    dbMock.Verify(db => db.Members,
                    Times.Once),
                Throws.Nothing);

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<ConfirmOrderViewModel>());

            var model = (ConfirmOrderViewModel)result.Model;

            Assert.That(model.CreditCardLast4Digits, Is.EqualTo(memberCreditCard.Last4Digits.FormatLast4Digits()));
        }

        [Test]
        public async void ConfirmOrder_ValidState_SetsViewModelPropertiesWithCorrectData()
        {
            decimal shippingCost = 5.99m;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, new List<Cart> { cartWithNewAndUsed });
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);
            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();
            shippingServiceStub.
                Setup(sc => sc.CalculateShippingCost(It.IsAny<decimal>(), It.IsAny<ICollection<CartItem>>())).
                Returns(shippingCost);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object, shippingCostService: shippingServiceStub.Object);

            var result = await controller.ConfirmOrder() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<ConfirmOrderViewModel>());

            var model = (ConfirmOrderViewModel)result.Model;

            Assert.That(model.FullName, Is.EqualTo(memberUser.FirstName + " " + memberUser.LastName));
            Assert.That(model.PhoneNumber, Is.EqualTo(memberUser.PhoneNumber));
            Assert.That(model.ShippingCost, Is.EqualTo(shippingCost));
            Assert.That(model.ItemSubTotal, Is.EqualTo(cartWithNewAndUsed.TotalCartItemsPrice));
            Assert.That(model.Items, 
                Has.Exactly(1).Matches<ConfirmOrderCartItemViewModel>(vm => 
                    vm.IsNew == newProduct1CartItem.IsNew &&
                    vm.ItemPrice == newProduct1CartItem.Product.NewWebPrice &&
                    vm.ProductId == newProduct1CartItem.ProductId &&
                    vm.Quantity == newProduct1CartItem.Quantity &&
                    vm.Name == newProduct1CartItem.Product.Name &&
                    vm.PlatformName == cartProduct1.Platform.PlatformName).
                And.Exactly(1).Matches<ConfirmOrderCartItemViewModel>(vm =>
                    vm.IsNew == usedProduct1CartItem.IsNew &&
                    vm.ItemPrice == usedProduct1CartItem.Product.UsedWebPrice &&
                    vm.ProductId == usedProduct1CartItem.ProductId &&
                    vm.Quantity == usedProduct1CartItem.Quantity &&
                    vm.Name == usedProduct1CartItem.Product.Name &&
                    vm.PlatformName == cartProduct1.Platform.PlatformName));
        }

        [Test]
        public async void ConfirmOrder_ValidState_CalculatesTaxAmountProperly()
        {
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

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithAddresses(dbStub, addresses);
            SetupVeilDataAccessWithMember(dbStub, member);

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);
            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object, shippingCostService: shippingServiceStub.Object);

            var result = await controller.ConfirmOrder() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<ConfirmOrderViewModel>());

            var model = (ConfirmOrderViewModel)result.Model;

            Assert.That(model.TaxAmount, Is.EqualTo(model.ItemSubTotal * 0.13m));
        }

        [Test]
        public async void ConfirmOrder_ValidState_OrdersViewModelCartItemsByProductIdDescending()
        {
            cartWithNewAndUsed.Items.Add(new CartItem
            {
                IsNew = true,
                ProductId = new Guid("00000000-0000-0000-0000-000000000000"),
                Product = new PhysicalGameProduct
                {
                    Game= game,
                    Platform = platform,
                    PlatformCode = platform.PlatformCode,
                    Id = new Guid("00000000-0000-0000-0000-000000000000"),
                    NewWebPrice = 60.0m,
                },
                MemberId = memberId,
                Quantity = 10
            });

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, new List<Cart> { cartWithNewAndUsed });
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);
            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object, shippingCostService: shippingServiceStub.Object);

            var result = await controller.ConfirmOrder() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<ConfirmOrderViewModel>());

            var model = (ConfirmOrderViewModel)result.Model;

            Assert.That(model.Items, Is.Ordered.By(nameof(ConfirmOrderCartItemViewModel.ProductId)).Descending);
        }

        [Test]
        public async void ConfirmOrder_CartItemsWithSameProductId_OrdersViewModelCartItemsByIsNew()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, new List<Cart> { cartWithNewAndUsed });
            SetupVeilDataAccessWithUser(dbStub, memberUser);
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithMember(dbStub, member);

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validSavedShippingBillingDetails);
            Mock<IShippingCostService> shippingServiceStub = new Mock<IShippingCostService>();

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object, shippingCostService: shippingServiceStub.Object);

            var result = await controller.ConfirmOrder() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<ConfirmOrderViewModel>());

            var model = (ConfirmOrderViewModel)result.Model;

            Assert.That(model.Items, Is.Ordered.By(nameof(ConfirmOrderCartItemViewModel.IsNew)));
        }
    }
}
