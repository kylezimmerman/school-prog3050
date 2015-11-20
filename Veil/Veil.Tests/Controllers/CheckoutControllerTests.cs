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
using Veil.Helpers;
using Veil.Models;
using Veil.Services;
using Veil.Services.Interfaces;

// TODO: Remove this after tests are implemented
#pragma warning disable 1998

namespace Veil.Tests.Controllers
{
    [TestFixture]
    public class CheckoutControllerTests
    {
        // TODO: Might want tests for both existing AddressId/BillingId and new Address/Billing info in session

        private Guid memberId;
        private Guid addressId;
        private Guid cartProductId;
        private GameProduct cartProduct;
        private CartItem cartItem;

        [SetUp]
        public void Setup()
        {
            memberId = new Guid("59EF92BE-D71F-49ED-992D-DF15773DAF98");
            addressId = new Guid("53BE47E4-0C74-4D49-97BB-7246A7880B39");
            cartProductId = new Guid("3882D242-A62A-4E99-BA11-D6EF340C2EE8");

            cartProduct = new PhysicalGameProduct
            {
                Id = cartProductId,
                NewWebPrice = 60.00m,
                ProductAvailabilityStatus = AvailabilityStatus.Available,
                ReleaseDate = new DateTime(635835582902643008L, DateTimeKind.Local),
                UsedWebPrice = 50.00m
            };

            cartItem = new CartItem
            {
                IsNew = true,
                MemberId = memberId,
                Product = cartProduct,
                ProductId = cartProduct.Id,
                Quantity = 1
            };
        }

        private CheckoutController CreateCheckoutController(
            IVeilDataAccess veilDataAccess = null, IGuidUserIdGetter idGetter = null,
            IStripeService stripeService = null, IShippingCostService shippingCostService = null,
            VeilUserManager userManager = null, ControllerContext context = null)
        {

            idGetter = idGetter ?? TestHelpers.GetSetupIUserIdGetterFake(memberId).Object;
            context = context ?? TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup().Object;

            var controller = new CheckoutController(
                veilDataAccess, idGetter, stripeService, shippingCostService, userManager)
            {
                ControllerContext = context
            };

            return controller;
        }

        private Mock<IVeilDataAccess> SetupVeilDataAccessFakeWithCountriesAndAddresses(List<Country> countries = null, List<MemberAddress> addresses = null)
        {
            addresses = addresses ?? new List<MemberAddress>();

            countries = countries ?? new List<Country>();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Country>> countryDbSetStub = TestHelpers.GetFakeAsyncDbSet(countries.AsQueryable());
            countryDbSetStub.SetupForInclude();

            dbStub.
                Setup(db => db.Countries).
                Returns(countryDbSetStub.Object);

            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(addresses.AsQueryable());

            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);

            return dbStub;
        }

        private Mock<IVeilDataAccess> SetupVeilDataAccessWithCartsCountriesAndAddresses(
            List<Cart> carts = null, List<Country> countries = null, List<MemberAddress> addresses = null)
        {
            carts = carts ?? new List<Cart>();

            Mock<IVeilDataAccess> dbStub = SetupVeilDataAccessFakeWithCountriesAndAddresses(countries, addresses);
            Mock<DbSet<Cart>> cartDbSetStub = TestHelpers.GetFakeAsyncDbSet(carts.AsQueryable());

            dbStub.
                Setup(db => db.Carts).
                Returns(cartDbSetStub.Object);

            return dbStub;
        }

        private Mock<IVeilDataAccess> SetupVeilDataAccessWithCartsCountriesAndAddresses(
            List<Cart> carts = null)
        {
            carts = carts ?? new List<Cart>();

            Mock<IVeilDataAccess> dbStub = SetupVeilDataAccessFakeWithCountriesAndAddresses(GetCountries(), GetMemberAddresses());
            Mock<DbSet<Cart>> cartDbSetStub = TestHelpers.GetFakeAsyncDbSet(carts.AsQueryable());

            dbStub.
                Setup(db => db.Carts).
                Returns(cartDbSetStub.Object);

            return dbStub;
        }

        private Mock<ControllerContext> GetControllerContextWithSessionSetupToReturn(
            WebOrderCheckoutDetails returnValue = null)
        {
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();
            contextStub.
                SetupGet(c => c.HttpContext.Session[CheckoutController.OrderCheckoutDetailsKey]).
                Returns(returnValue);

            return contextStub;
        } 

        private List<MemberAddress> GetMemberAddresses()
        {
            return new List<MemberAddress>
            {
                new MemberAddress
                {
                    Address = new Address
                    {
                        City = "A city"
                    },
                    CountryCode = "CA",
                    MemberId = memberId
                },
                new MemberAddress
                {
                    Address = new Address
                    {
                        City = "Waterloo",
                        PostalCode = "N2L 6R2",
                        StreetAddress = "445 Wes Graham Way"
                    },
                    CountryCode = "CA",
                    ProvinceCode = "ON",
                    MemberId = memberId
                }
            };
        }

        private List<Country> GetCountries()
        {
            return new List<Country>
            {
                new Country { CountryCode = "CA", CountryName = "Canada"},
                new Country { CountryCode = "US", CountryName = "United States"}
            };
        }

        private List<Cart> GetCartsListWithValidMemberCart()
        {
            return new List<Cart>
            {
                new Cart
                {
                    Items = new List<CartItem>
                    {
                        cartItem
                    },
                    MemberId = memberId
                }
            };
        }

        

        [Test]
        public async void ShippingInfo_EmptyCart_RedirectsToCartIndex()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Cart>> cartDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Cart>().AsQueryable());
            dbStub.
                Setup(db => db.Carts).
                Returns(cartDbSetStub.Object);

            CheckoutController controller = CreateCheckoutController(dbStub.Object);

            var result = await controller.ShippingInfo() as RedirectToRouteResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Index"));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Cart"));
        }

        [Test]
        public async void ShippingInfo_NonEmptyCart_SetsUpViewModel()
        {
            List<Country> countries = GetCountries();
            List<MemberAddress> addresses = GetMemberAddresses();

            Mock<IVeilDataAccess> dbStub = SetupVeilDataAccessWithCartsCountriesAndAddresses(GetCartsListWithValidMemberCart(), countries, addresses);
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(null);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.ShippingInfo() as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Model, Is.InstanceOf<AddressViewModel>());

            var model = (AddressViewModel) result.Model;

            Assert.That(model.Addresses.Count(), Is.EqualTo(addresses.Count));
            Assert.That(model.Countries, Has.Count.EqualTo(countries.Count));
        }

        [Test]
        public async void ShippingInfo_NonEmptyCartWithNonSavedAddressAlreadyInSession_AddsAddressInfoToViewModel()
        {
            var orderDetails = new WebOrderCheckoutDetails
            {
                Address = new Address
                {
                    City = "Waterloo",
                    PostalCode = "N2L 6R2",
                    POBoxNumber = "123",
                    StreetAddress = "445 Wes Graham Way"
                },
                ProvinceCode = "ON",
                CountryCode = "CA"
            };

            Mock<IVeilDataAccess> dbStub = SetupVeilDataAccessWithCartsCountriesAndAddresses(GetCartsListWithValidMemberCart());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(orderDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.ShippingInfo() as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Model, Is.InstanceOf<AddressViewModel>());

            var model = (AddressViewModel)result.Model;

            Assert.That(model.StreetAddress, Is.EqualTo(orderDetails.Address.StreetAddress));
            Assert.That(model.City, Is.EqualTo(orderDetails.Address.City));
            Assert.That(model.PostalCode, Is.EqualTo(orderDetails.Address.PostalCode));
            Assert.That(model.POBoxNumber, Is.EqualTo(orderDetails.Address.POBoxNumber));
            Assert.That(model.CountryCode, Is.EqualTo(orderDetails.CountryCode));
            Assert.That(model.ProvinceCode, Is.EqualTo(orderDetails.ProvinceCode));
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewShippingInfo_EmptyCart_RedirectsToCardIndex()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewShippingInfo_InvalidModelState_RedisplaysViewWithSameModel()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewShippingInfo_InvalidModelState_SetsUpAddressesAndCountriesOnViewModel()
        {

        }

        [Ignore("No implemented yet")]
        [TestCase("CA")]
        [TestCase("US")]
        public async void NewShippingInfo_InvalidPostalCodeModelStateWithCountryCodeSupplied_ReplacesErrorMessage(string countryCode)
        {
            AddressViewModel viewModel = new AddressViewModel
            {
                CountryCode = countryCode
            };

            string postalCodeErrorMessage = "Required";

            Mock<IVeilDataAccess> dbStub = SetupVeilDataAccessFakeWithCountriesAndAddresses();

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            CheckoutController controller = CreateCheckoutController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object);
            controller.ControllerContext = contextStub.Object;

            controller.ModelState.AddModelError(nameof(AddressViewModel.PostalCode), postalCodeErrorMessage);

            await controller.NewShippingInfo(viewModel, false);

            Assert.That(controller.ModelState[nameof(AddressViewModel.PostalCode)].Errors, Has.None.Matches<ModelError>(modelError => modelError.ErrorMessage == postalCodeErrorMessage));
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewShippingInfo_InvalidPostalCodeModelStateWithoutCountryCodeSupplied_LeavesErrorMessage()
        {
            AddressViewModel viewModel = new AddressViewModel();

            string postalCodeErrorMessage = "Required";

            Mock<IVeilDataAccess> dbStub = SetupVeilDataAccessFakeWithCountriesAndAddresses();

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            CheckoutController controller = CreateCheckoutController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object);
            controller.ControllerContext = contextStub.Object;

            controller.ModelState.AddModelError(nameof(AddressViewModel.PostalCode), postalCodeErrorMessage);

            await controller.NewShippingInfo(viewModel, false);

            Assert.That(controller.ModelState[nameof(AddressViewModel.PostalCode)].Errors, Has.Some.Matches<ModelError>(modelError => modelError.ErrorMessage == postalCodeErrorMessage));
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewShippingInfo_InvalidCountry_RedisplaysViewWithSameViewModel()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewShippingInfo_InvalidProvince_RedisplaysViewWithSameViewModel()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewShippingInfo_InvalidCountryOrProvince_SetsUpAddressesAndCountriesOnViewModel()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewShippingInfo_NewSession_AddsNewWebOrderCheckoutDetailsToSession()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewShippingInfo_ExistingSession_UpdatesAndReassignsOrderDetails()
        {

        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewShippingInfo_ValidModel_FormatsPostalCode()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewShippingInfo_SaveAddress_AddsNewAddressToDbSet()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewShippingInfo_SaveAddress_CallsSaveChangesAsync()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewShippingInfo_SaveAddress_AddsAddressIdToSessionOrderDetails()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewShippingInfo_DoNotSaveAddress_AddsAddressCountryProvinceToSessionOrderDetails()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewShippingInfo_ReturnToConfirm_RedirectsToConfirmOrder()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewShippingInfo_DoNotReturnToConfirm_RedirectsToBilingInfo()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ExistingShippingInfo_EmptyCart_RedirectsToCartIndex()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ExistingShippingInfo_IdNotInDb_RedirectsToShippingInfo()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ExistingShippingInfo_NewSession_AddsNewWebOrderCheckoutDetailsToSession()
        {

        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ExistingShippingInfo_ExistingSession_UpdatesAndReassignsOrderDetails()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ExistingShippingInfo_ReturnToConfirm_RedirectsToConfirmOrder()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ExistingShippingInfo_DoNotReturnToConfirm_RedirectsToBillingInfo()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void BillingInfo_EmptyCart_RedirectsToCartIndex()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void BillingInfo_AddressNotSetInSession_RedirectsToShippingInfo()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void BillingInfo_NullSessionOrderDetails_RedirectsToShippingInfo()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void BillingInfo_NewAddressInSession_DisplaysBillingInfo()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void BillingInfo_ExistingAddressInSession_DisplaysBillingInfo()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void BillingInfo_ValidState_SetsUpCreditCardsAndCountriesOnViewModel()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewBillingInfo_EmptyCart_RedirectsToCartIndex()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewBillingInfo_AddressNotSetInSession_RedirectsToShippingInfo()
        {

        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewBillingInfo_NullSessionOrderDetails_RedirectsToShippingInfo()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewBillingInfo_NullOrWhiteSpaceStripeToken_RedirectsToBillingInfo()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewBillingInfo_SaveCardButMemberIdNotInDb_ReturnsInternalServerErrorStatusCode()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewBillingInfo_SaveCard_CallsStripeServiceCreateCardWithMemberCustomerIdAndPassedCardToken()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewBillingInfo_StripeExceptionCardError_AddsCardErrorMessageToModelState()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewBillingInfo_StripeException_RedisplaysBillingInfo()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewBillingInfo_SaveCard_AddsCardToDbSet()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewBillingInfo_SaveCard_CallsSaveChangesAsync()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewBillingInfo_SaveCard_AddsCardIdToSessionOrderDetails()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewBillingInfo_DoNotSaveCard_AddsPassedTokenToSessionOrderDetails()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewBillingInfo_ValidState_ReassignsUpdatedOrderDetails()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void NewBillingInfo_ValidState_RedirectsToConfirmOrder()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ExistingBillingInfo_EmptyCart_RedirectsToCartIndex()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ExistingBillingInfo_AddressNotSetInSession_RedirectsToShippingInfo()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ExistingBillingInfo_NullSessionOrderDetails_RedirectsToShippingInfo()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ExistingBillingInfo_IdNotInDb_RedirectsToBillingInfo()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ExistingBillingInfo_IdInDb_AddsCardIdToOrderDetailsAndReassignsToSession()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ConfirmOrder_EmptyCart_RedirectsToCartIndex()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ConfirmOrder_AddressNotSetInSession_RedirectsToShippingInfo()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ConfirmOrder_BillingInfoNotSetInsession_RedirectsToBillingInfo()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ConfirmOrder_NullSessionOrderDetails_RedirectsToShippingInfo()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ConfirmOrder_AddressIsIdButIdNotInDb_RedirectsToShippingInfo()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ConfirmOrder_AddressIsId_GetsAddressFromDb()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ConfirmOrder_AddressIsUnsaved_DoesNotTouchMemberAddressDbSet()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ConfirmOrder_CardIsIdButIdNotInDb_RedirectsToBillingInfo()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ConfirmOrder_CardIsId_GetsCardFromDb()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ConfirmOrder_CardIsToken_CallsStripeServiceGetLast4ForToken()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ConfirmOrder_CardIsTokenAndStripeExceptionThrown_RedirectsToBillingInfo()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ConfirmOrder_ValidState_LoadsCartWithProductsIncluded()
        {
            // Need 1 new 1 used to test logic
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ConfirmOrder_ValidState_OrdersCartItemsByProductId()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ConfirmOrder_ValidState_CallsShippingCostServiceCalculateShippingCost()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ConfirmOrder_ValidState_SetsViewModelPropertiesWithCorrectData()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ConfirmOrder_ValidState_CalculatesTaxAmountProperly()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void ConfirmOrder_ValidState_OrdersViewModelCartItemsByProductId()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_AddressNotSetInSession_RedirectsToShippingInfo()
        {

        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_BillingInfoNotSetInsession_RedirectsToBillingInfo()
        {

        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_NullSessionOrderDetails_RedirectsToShippingInfo()
        {

        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_EmptyCart_RedirectsToCardIndex()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_CartDoesNotMatchPostedBackItems_RedirectsToConfirmOrder()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_AddressIsIdButIdNotInDb_RedirectsToShippingInfo()
        {

        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_AddressIsId_GetsAddressFromDb()
        {

        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_AddressIsUnsaved_DoesNotTouchDb()
        {

        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_CardIsIdButIdNotInDb_RedirectsToBillingInfo()
        {

        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_CardIsId_GetsCardFromDb()
        {

        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_CardIsToken_CallsStripeServiceGetLast4ForToken()
        {

        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_CardIsTokenAndStripeExceptionThrown_RedirectsToBillingInfo()
        {

        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_CardIsId_LoadsStripeCustomerIdFromDb()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_CardIsId_LoadsCardTokenFromDb()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_CardIsToken_DoesNotTouchMemberCreditCardDbSet()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_ValidState_GetsOnlineWarehouseProductLocationInventoryForEachCartItem()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_NewItemUnrestrictedAvailability_DecreasesInventoryLevelByQuantity()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_NewItemDiscontinuedAvailabilityWithEnoughNewOnHand_DecreasesInventoryLevelByQuantity()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_NewItemDiscontinuedAvailabilityWithoutEnoughNewOnHand_RedirectsToConfirmOrder()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_UsedWithHigherQuantityThanUsedOnHand_RedirectsToConfirmOrder()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_UsedWithQuantityLessThanOrEqualToUsedOnHand_DecreasesInventoryLevelByQuantity()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_ValidInventoryLevels_CallsStripeServiceChargeCard()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_ValidInventoryLevelsAndStripeExceptionThrow_RedirectsToConfirmOrder()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_SuccessfulCharge_AddsChargeIdToWebOrder()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_SuccessfulCharge_AddsNewOrderToWebOrdersDbSet()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_SucessfulCharge_ClearsCart()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_SuccessfulCharge_CallsSaveChangesAsync()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_SaveChangesThrows_CallsStripeServiceRefundCharge()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_SaveChangesThrows_RedirectsToConfirmOrder()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_SaveSuccessful_RemovesOrderDetailsFromSession()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_SaveSuccessful_NullsCartQuantityInSession()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_SaveSuccessful_CallsUserManageSendEmailAsync()
        {
            
        }

        [Ignore("No implemented yet")]
        [Test]
        public async void PlaceOrder_SaveSuccessful_RedirectsToHomeIndex()
        {
            
        }
    }
}
