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

namespace Veil.Tests.Controllers
{
    [Ignore("No implemented yet")]
    [TestFixture]
    public class CheckoutControllerTests
    {
        // TODO: Might want tests for both existing AddressId/BillingId and new Address/Billing info in session

        private Guid memberId;
        private Guid addressId;

        [SetUp]
        public void Setup()
        {
            memberId = new Guid("59EF92BE-D71F-49ED-992D-DF15773DAF98");
            addressId = new Guid("53BE47E4-0C74-4D49-97BB-7246A7880B39");
        }

        private CheckoutController CreateCheckoutController(
            IVeilDataAccess veilDataAccess = null, IGuidUserIdGetter idGetter = null,
            IStripeService stripeService = null, IShippingCostService shippingCostService = null,
            VeilUserManager userManager = null)
        {
            return new CheckoutController(veilDataAccess, idGetter, stripeService, shippingCostService, userManager);
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

        [Test]
        public async void ShippingInfo_EmptyCart_RedirectsToCartIndex()
        {
            
        }

        [Test]
        public async void ShippingInfo_NonEmptyCart_SetsUpViewModel()
        {
            
        }

        [Test]
        public async void ShippingInfo_NonEmptyCartWithNonSavedAddressAlreadyInSession_AddsAddressInfoToViewModel()
        {
            
        }

        [Test]
        public async void NewShippingInfo_EmptyCart_RedirectsToCardIndex()
        {
            
        }

        [Test]
        public async void NewShippingInfo_InvalidModelState_RedisplaysViewWithSameModel()
        {
            
        }

        [Test]
        public async void NewShippingInfo_InvalidModelState_SetsUpAddressesAndCountriesOnViewModel()
        {

        }

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

        [Test]
        public async void NewShippingInfo_InvalidCountry_RedisplaysViewWithSameViewModel()
        {
            
        }

        [Test]
        public async void NewShippingInfo_InvalidProvince_RedisplaysViewWithSameViewModel()
        {
            
        }

        [Test]
        public async void NewShippingInfo_InvalidCountryOrProvince_SetsUpAddressesAndCountriesOnViewModel()
        {
            
        }

        [Test]
        public async void NewShippingInfo_NewSession_AddsNewWebOrderCheckoutDetailsToSession()
        {
            
        }

        [Test]
        public async void NewShippingInfo_ExistingSession_UpdatesAndReassignsOrderDetails()
        {

        }

        [Test]
        public async void NewShippingInfo_ValidModel_FormatsPostalCode()
        {
            
        }

        [Test]
        public async void NewShippingInfo_SaveAddress_AddsNewAddressToDbSet()
        {
            
        }

        [Test]
        public async void NewShippingInfo_SaveAddress_CallsSaveChangesAsync()
        {
            
        }

        [Test]
        public async void NewShippingInfo_SaveAddress_AddsAddressIdToSessionOrderDetails()
        {
            
        }

        [Test]
        public async void NewShippingInfo_DoNotSaveAddress_AddsAddressCountryProvinceToSessionOrderDetails()
        {
            
        }

        [Test]
        public async void NewShippingInfo_ReturnToConfirm_RedirectsToConfirmOrder()
        {
            
        }

        [Test]
        public async void NewShippingInfo_DoNotReturnToConfirm_RedirectsToBilingInfo()
        {
            
        }

        [Test]
        public async void ExistingShippingInfo_EmptyCart_RedirectsToCartIndex()
        {
            
        }

        [Test]
        public async void ExistingShippingInfo_IdNotInDb_RedirectsToShippingInfo()
        {
            
        }

        [Test]
        public async void ExistingShippingInfo_NewSession_AddsNewWebOrderCheckoutDetailsToSession()
        {

        }

        [Test]
        public async void ExistingShippingInfo_ExistingSession_UpdatesAndReassignsOrderDetails()
        {
            
        }

        [Test]
        public async void ExistingShippingInfo_ReturnToConfirm_RedirectsToConfirmOrder()
        {
            
        }

        [Test]
        public async void ExistingShippingInfo_DoNotReturnToConfirm_RedirectsToBillingInfo()
        {
            
        }

        [Test]
        public async void BillingInfo_EmptyCart_RedirectsToCartIndex()
        {
            
        }

        [Test]
        public async void BillingInfo_AddressNotSetInSession_RedirectsToShippingInfo()
        {
            
        }

        [Test]
        public async void BillingInfo_NullSessionOrderDetails_RedirectsToShippingInfo()
        {
            
        }

        [Test]
        public async void BillingInfo_NewAddressInSession_DisplaysBillingInfo()
        {
            
        }

        [Test]
        public async void BillingInfo_ExistingAddressInSession_DisplaysBillingInfo()
        {
            
        }

        [Test]
        public async void BillingInfo_ValidState_SetsUpCreditCardsAndCountriesOnViewModel()
        {
            
        }

        [Test]
        public async void NewBillingInfo_EmptyCart_RedirectsToCartIndex()
        {
            
        }

        [Test]
        public async void NewBillingInfo_AddressNotSetInSession_RedirectsToShippingInfo()
        {

        }

        [Test]
        public async void NewBillingInfo_NullSessionOrderDetails_RedirectsToShippingInfo()
        {
            
        }

        [Test]
        public async void NewBillingInfo_NullOrWhiteSpaceStripeToken_RedirectsToBillingInfo()
        {
            
        }

        [Test]
        public async void NewBillingInfo_SaveCardButMemberIdNotInDb_ReturnsInternalServerErrorStatusCode()
        {
            
        }

        [Test]
        public async void NewBillingInfo_SaveCard_CallsStripeServiceCreateCardWithMemberCustomerIdAndPassedCardToken()
        {
            
        }

        [Test]
        public async void NewBillingInfo_StripeExceptionCardError_AddsCardErrorMessageToModelState()
        {
            
        }

        [Test]
        public async void NewBillingInfo_StripeException_RedisplaysBillingInfo()
        {
            
        }

        [Test]
        public async void NewBillingInfo_SaveCard_AddsCardToDbSet()
        {
            
        }

        [Test]
        public async void NewBillingInfo_SaveCard_CallsSaveChangesAsync()
        {
            
        }

        [Test]
        public async void NewBillingInfo_SaveCard_AddsCardIdToSessionOrderDetails()
        {
            
        }

        [Test]
        public async void NewBillingInfo_DoNotSaveCard_AddsPassedTokenToSessionOrderDetails()
        {
            
        }

        [Test]
        public async void NewBillingInfo_ValidState_ReassignsUpdatedOrderDetails()
        {
            
        }

        [Test]
        public async void NewBillingInfo_ValidState_RedirectsToConfirmOrder()
        {
            
        }

        [Test]
        public async void ExistingBillingInfo_EmptyCart_RedirectsToCartIndex()
        {
            
        }

        [Test]
        public async void ExistingBillingInfo_AddressNotSetInSession_RedirectsToShippingInfo()
        {
            
        }

        [Test]
        public async void ExistingBillingInfo_NullSessionOrderDetails_RedirectsToShippingInfo()
        {
            
        }

        [Test]
        public async void ExistingBillingInfo_IdNotInDb_RedirectsToBillingInfo()
        {
            
        }

        [Test]
        public async void ExistingBillingInfo_IdInDb_AddsCardIdToOrderDetailsAndReassignsToSession()
        {
            
        }

        [Test]
        public async void ConfirmOrder_EmptyCart_RedirectsToCartIndex()
        {
            
        }

        [Test]
        public async void ConfirmOrder_AddressNotSetInSession_RedirectsToShippingInfo()
        {
            
        }

        [Test]
        public async void ConfirmOrder_BillingInfoNotSetInsession_RedirectsToBillingInfo()
        {
            
        }

        [Test]
        public async void ConfirmOrder_NullSessionOrderDetails_RedirectsToShippingInfo()
        {
            
        }

        [Test]
        public async void ConfirmOrder_AddressIsIdButIdNotInDb_RedirectsToShippingInfo()
        {
            
        }

        [Test]
        public async void ConfirmOrder_AddressIsId_GetsAddressFromDb()
        {
            
        }

        [Test]
        public async void ConfirmOrder_AddressIsUnsaved_DoesNotTouchMemberAddressDbSet()
        {
            
        }

        [Test]
        public async void ConfirmOrder_CardIsIdButIdNotInDb_RedirectsToBillingInfo()
        {
            
        }

        [Test]
        public async void ConfirmOrder_CardIsId_GetsCardFromDb()
        {
            
        }

        [Test]
        public async void ConfirmOrder_CardIsToken_CallsStripeServiceGetLast4ForToken()
        {
            
        }

        [Test]
        public async void ConfirmOrder_CardIsTokenAndStripeExceptionThrown_RedirectsToBillingInfo()
        {
            
        }

        [Test]
        public async void ConfirmOrder_ValidState_LoadsCartWithProductsIncluded()
        {
            // Need 1 new 1 used to test logic
        }

        [Test]
        public async void ConfirmOrder_ValidState_OrdersCartItemsByProductId()
        {
            
        }

        [Test]
        public async void ConfirmOrder_ValidState_CallsShippingCostServiceCalculateShippingCost()
        {
            
        }

        [Test]
        public async void ConfirmOrder_ValidState_SetsViewModelPropertiesWithCorrectData()
        {
            
        }

        [Test]
        public async void ConfirmOrder_ValidState_CalculatesTaxAmountProperly()
        {
            
        }

        [Test]
        public async void ConfirmOrder_ValidState_OrdersViewModelCartItemsByProductId()
        {
            
        }

        [Test]
        public async void PlaceOrder_AddressNotSetInSession_RedirectsToShippingInfo()
        {

        }

        [Test]
        public async void PlaceOrder_BillingInfoNotSetInsession_RedirectsToBillingInfo()
        {

        }

        [Test]
        public async void PlaceOrder_NullSessionOrderDetails_RedirectsToShippingInfo()
        {

        }

        [Test]
        public async void PlaceOrder_EmptyCart_RedirectsToCardIndex()
        {
            
        }

        [Test]
        public async void PlaceOrder_CartDoesNotMatchPostedBackItems_RedirectsToConfirmOrder()
        {
            
        }

        [Test]
        public async void PlaceOrder_AddressIsIdButIdNotInDb_RedirectsToShippingInfo()
        {

        }

        [Test]
        public async void PlaceOrder_AddressIsId_GetsAddressFromDb()
        {

        }

        [Test]
        public async void PlaceOrder_AddressIsUnsaved_DoesNotTouchDb()
        {

        }

        [Test]
        public async void PlaceOrder_CardIsIdButIdNotInDb_RedirectsToBillingInfo()
        {

        }

        [Test]
        public async void PlaceOrder_CardIsId_GetsCardFromDb()
        {

        }

        [Test]
        public async void PlaceOrder_CardIsToken_CallsStripeServiceGetLast4ForToken()
        {

        }

        [Test]
        public async void PlaceOrder_CardIsTokenAndStripeExceptionThrown_RedirectsToBillingInfo()
        {

        }

        [Test]
        public async void PlaceOrder_CardIsId_LoadsStripeCustomerIdFromDb()
        {
            
        }

        [Test]
        public async void PlaceOrder_CardIsId_LoadsCardTokenFromDb()
        {
            
        }

        [Test]
        public async void PlaceOrder_CardIsToken_DoesNotTouchMemberCreditCardDbSet()
        {
            
        }

        [Test]
        public async void PlaceOrder_ValidState_GetsOnlineWarehouseProductLocationInventoryForEachCartItem()
        {
            
        }

        [Test]
        public async void PlaceOrder_NewItemUnrestrictedAvailability_DecreasesInventoryLevelByQuantity()
        {
            
        }

        [Test]
        public async void PlaceOrder_NewItemDiscontinuedAvailabilityWithEnoughNewOnHand_DecreasesInventoryLevelByQuantity()
        {
            
        }

        [Test]
        public async void PlaceOrder_NewItemDiscontinuedAvailabilityWithoutEnoughNewOnHand_RedirectsToConfirmOrder()
        {
            
        }

        [Test]
        public async void PlaceOrder_UsedWithHigherQuantityThanUsedOnHand_RedirectsToConfirmOrder()
        {
            
        }

        [Test]
        public async void PlaceOrder_UsedWithQuantityLessThanOrEqualToUsedOnHand_DecreasesInventoryLevelByQuantity()
        {
            
        }

        [Test]
        public async void PlaceOrder_ValidInventoryLevels_CallsStripeServiceChargeCard()
        {
            
        }

        [Test]
        public async void PlaceOrder_ValidInventoryLevelsAndStripeExceptionThrow_RedirectsToConfirmOrder()
        {
            
        }

        [Test]
        public async void PlaceOrder_SuccessfulCharge_AddsChargeIdToWebOrder()
        {
            
        }

        [Test]
        public async void PlaceOrder_SuccessfulCharge_AddsNewOrderToWebOrdersDbSet()
        {
            
        }

        [Test]
        public async void PlaceOrder_SucessfulCharge_ClearsCart()
        {
            
        }

        [Test]
        public async void PlaceOrder_SuccessfulCharge_CallsSaveChangesAsync()
        {
            
        }

        [Test]
        public async void PlaceOrder_SaveChangesThrows_CallsStripeServiceRefundCharge()
        {
            
        }

        [Test]
        public async void PlaceOrder_SaveChangesThrows_RedirectsToConfirmOrder()
        {
            
        }

        [Test]
        public async void PlaceOrder_SaveSuccessful_RemovesOrderDetailsFromSession()
        {
            
        }

        [Test]
        public async void PlaceOrder_SaveSuccessful_NullsCartQuantityInSession()
        {
            
        }

        [Test]
        public async void PlaceOrder_SaveSuccessful_CallsUserManageSendEmailAsync()
        {
            
        }

        [Test]
        public async void PlaceOrder_SaveSuccessful_RedirectsToHomeIndex()
        {
            
        }
    }
}
