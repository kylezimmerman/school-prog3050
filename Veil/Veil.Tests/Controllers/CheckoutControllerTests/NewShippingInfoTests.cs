using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Models;

namespace Veil.Tests.Controllers.CheckoutControllerTests
{
    public class NewShippingInfoTests : CheckoutControllerTestsBase
    {
        private List<Province> GetProvinceList(AddressViewModel model)
        {
            return new List<Province>
            {
                new Province
                {
                    ProvinceCode = model.ProvinceCode,
                    CountryCode = model.CountryCode
                }
            };
        }

        [Test]
        public async void NewShippingInfo_EmptyCart_RedirectsToCardIndex()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, new List<Cart>());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, GetCountries());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(null);
            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);
            controller.ControllerContext = contextStub.Object;

            var result = await controller.NewShippingInfo(null, false) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Index"));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Cart"));
        }

        [Test]
        public async void NewShippingInfo_InvalidModelState_RedisplaysViewWithSameModelWithAddressesAndCountriesSetup()
        {
            var viewModel = new AddressViewModel();

            List<Country> countries = GetCountries();
            List<MemberAddress> addresses = GetMemberAddresses();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, countries);
            SetupVeilDataAccessWithAddresses(dbStub, addresses);
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);
            controller.ModelState.AddModelError(nameof(AddressViewModel.ProvinceCode), "Invalid");

            var result = await controller.NewShippingInfo(viewModel, false) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<AddressViewModel>());

            var model = (AddressViewModel)result.Model;

            Assert.That(model.Addresses.Count(), Is.EqualTo(addresses.Count));
            Assert.That(model.Countries, Has.Count.EqualTo(countries.Count));
            Assert.That(model, Is.EqualTo(viewModel));
            Assert.That(model.City, Is.EqualTo(viewModel.City));
            Assert.That(model.CountryCode, Is.EqualTo(viewModel.CountryCode));
            Assert.That(model.ProvinceCode, Is.EqualTo(viewModel.ProvinceCode));
            Assert.That(model.POBoxNumber, Is.EqualTo(viewModel.POBoxNumber));
            Assert.That(model.PostalCode, Is.EqualTo(viewModel.PostalCode));
            Assert.That(model.StreetAddress, Is.EqualTo(viewModel.StreetAddress));
        }

        [Test]
        public async void NewShippingInfo_InvalidPostalCodeModelStateWithCountryCodeSupplied_ReplacesErrorMessage([Values("CA", "US")]string countryCode)
        {
            AddressViewModel viewModel = new AddressViewModel
            {
                CountryCode = countryCode
            };

            string postalCodeErrorMessage = "Required";

            List<Country> countries = GetCountries();
            List<MemberAddress> addresses = GetMemberAddresses();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, countries);
            SetupVeilDataAccessWithAddresses(dbStub, addresses);
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            controller.ModelState.AddModelError(nameof(AddressViewModel.PostalCode), postalCodeErrorMessage);

            await controller.NewShippingInfo(viewModel, false);

            Assert.That(controller.ModelState[nameof(AddressViewModel.PostalCode)].Errors, Has.None.Matches<ModelError>(modelError => modelError.ErrorMessage == postalCodeErrorMessage));
        }

        [Test]
        public async void NewShippingInfo_InvalidPostalCodeModelStateWithoutCountryCodeSupplied_LeavesErrorMessage()
        {
            AddressViewModel viewModel = new AddressViewModel();

            string postalCodeErrorMessage = "Required";

            List<Country> countries = GetCountries();
            List<MemberAddress> addresses = GetMemberAddresses();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, countries);
            SetupVeilDataAccessWithAddresses(dbStub, addresses);
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            controller.ModelState.AddModelError(nameof(AddressViewModel.PostalCode), postalCodeErrorMessage);

            await controller.NewShippingInfo(viewModel, false);

            Assert.That(controller.ModelState[nameof(AddressViewModel.PostalCode)].Errors, Has.Some.Matches<ModelError>(modelError => modelError.ErrorMessage == postalCodeErrorMessage));
        }

        [Test]
        public async void NewShippingInfo_InvalidCountry_RedisplaysViewWithSameViewModel()
        {
            var viewModel = validAddressViewModel;
            viewModel.CountryCode = "NO"; // Doesn't exist in the empty list of countries

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, new List<Country>());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.NewShippingInfo(viewModel, false) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<AddressViewModel>());

            var model = (AddressViewModel)result.Model;

            Assert.That(model, Is.EqualTo(viewModel));
        }

        [Test]
        public async void NewShippingInfo_InvalidProvince_RedisplaysViewWithSameViewModel()
        {
            var viewModel = validAddressViewModel;
            viewModel.ProvinceCode = "NO"; // Doesn't exist in the empty list of provinces

            List<Country> countries = GetCountries();
            List<Province> provinces = new List<Province>();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, countries);
            SetupVeilDataAccessWithProvincesSetupForInclude(dbStub, provinces);
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.NewShippingInfo(viewModel, false) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<AddressViewModel>());

            var model = (AddressViewModel)result.Model;

            Assert.That(model, Is.EqualTo(viewModel));
        }

        [Test, Sequential]
        public async void NewShippingInfo_InvalidCountryOrProvince_SetsUpAddressesAndCountriesOnViewModel([Values("NO", "CA")]string countryCode, [Values("ON", "NO")]string provinceCode)
        {
            var viewModel = validAddressViewModel;
            viewModel.CountryCode = countryCode;
            viewModel.ProvinceCode = provinceCode;

            List<Country> countries = GetCountries();
            List<Province> provinces = new List<Province>();
            List<MemberAddress> addresses = GetMemberAddresses();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, countries);
            SetupVeilDataAccessWithAddresses(dbStub, addresses);
            SetupVeilDataAccessWithProvincesSetupForInclude(dbStub, provinces);
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.NewShippingInfo(viewModel, false) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<AddressViewModel>());

            var model = (AddressViewModel)result.Model;

            Assert.That(model, Is.EqualTo(viewModel));
            Assert.That(model.Addresses.Count(), Is.EqualTo(addresses.Count));
            Assert.That(model.Countries, Has.Count.EqualTo(countries.Count));
        }
        
        [Test]
        public async void NewShippingInfo_ValidModel_FormatsPostalCode()
        {
            Mock<AddressViewModel> viewModelMock = new Mock<AddressViewModel>();
            viewModelMock.
                Setup(vm => vm.FormatPostalCode()).
                Verifiable();

            var viewModel = viewModelMock.Object;
            viewModel.City = "Waterloo";
            viewModel.CountryCode = "CA";
            viewModel.ProvinceCode = "ON";
            viewModel.POBoxNumber = "1234";
            viewModel.PostalCode = "N2L-6R2";
            viewModel.StreetAddress = "445 Wes Graham Way";

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, GetCountries());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithProvincesSetupForInclude(dbStub, GetProvinceList(viewModel));
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(null);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            await controller.NewShippingInfo(viewModel, false);

            Assert.That(
                () => 
                    viewModelMock.Verify(vm => vm.FormatPostalCode(),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void NewShippingInfo_SaveAddress_MapsViewModelAndAddsNewAddressToDbSet()
        {
            Address mappedAddress = new Address();

            Mock<AddressViewModel> viewModelMock = new Mock<AddressViewModel>();
            viewModelMock.Setup(vm => vm.MapToNewAddress()).
                Returns(mappedAddress).
                Verifiable();

            var viewModel = viewModelMock.Object;
            viewModel.City = "Waterloo";
            viewModel.CountryCode = "CA";
            viewModel.ProvinceCode = "ON";
            viewModel.POBoxNumber = "1234";
            viewModel.PostalCode = "N2L 6R2";
            viewModel.StreetAddress = "445 Wes Graham Way";

            MemberAddress newAddress = null;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, GetCountries());
            
            Mock<DbSet<MemberAddress>> addressDbSetMock = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetMock.
                Setup(adb => adb.Add(It.IsAny<MemberAddress>())).
                Returns<MemberAddress>(val => val).
                Callback<MemberAddress>(ma => newAddress = ma).
                Verifiable();

            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetMock.Object);

            SetupVeilDataAccessWithProvincesSetupForInclude(dbStub, new List<Province> { new Province { ProvinceCode = viewModel.ProvinceCode, CountryCode = viewModel.CountryCode } });
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(null);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            await controller.NewShippingInfo(viewModel, true);

            Assert.That(
                () => 
                    viewModelMock.Verify(vm => vm.MapToNewAddress(),
                    Times.Once),
                Throws.Nothing);

            Assert.That(
                () => 
                    addressDbSetMock.Verify(adb => adb.Add(It.IsAny<MemberAddress>()),
                    Times.Once),
                Throws.Nothing);

            Assert.That(newAddress != null);
            Assert.That(newAddress.CountryCode, Is.EqualTo(viewModel.CountryCode));
            Assert.That(newAddress.ProvinceCode, Is.EqualTo(viewModel.ProvinceCode));
            Assert.That(newAddress.MemberId, Is.EqualTo(memberId));
            Assert.That(newAddress.Address, Is.SameAs(mappedAddress));
        }

        [Test]
        public async void NewShippingInfo_SaveAddress_CallsSaveChangesAsync()
        {
            var viewModel = validAddressViewModel;

            Mock<IVeilDataAccess> dbMock = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbMock, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbMock, GetCountries());
            SetupVeilDataAccessWithAddresses(dbMock, new List<MemberAddress>());

            dbMock.
                Setup(db => db.SaveChangesAsync()).
                ReturnsAsync(1).
                Verifiable();

            SetupVeilDataAccessWithProvincesSetupForInclude(dbMock, GetProvinceList(viewModel));
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(null);

            CheckoutController controller = CreateCheckoutController(dbMock.Object, context: contextStub.Object);

            await controller.NewShippingInfo(viewModel, true);

            Assert.That(
                () =>
                    dbMock.Verify(db => db.SaveChangesAsync(),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void NewShippingInfo_SaveAddressNewSession_AddsAddressIdToNewSessionOrderDetails()
        {
            WebOrderCheckoutDetails checkoutDetails = null;

            var viewModel = validAddressViewModel;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, GetCountries());
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.Add(It.IsAny<MemberAddress>())).
                Returns<MemberAddress>(
                    val =>
                    {
                        val.Id = addressId;
                        return val;
                    });

            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);
            dbStub.
                Setup(db => db.SaveChangesAsync()).
                ReturnsAsync(1);

            SetupVeilDataAccessWithProvincesSetupForInclude(dbStub, GetProvinceList(viewModel));
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(null);

            contextStub.
                SetupSet(c => c.HttpContext.Session[CheckoutController.OrderCheckoutDetailsKey] = It.IsAny<WebOrderCheckoutDetails>()).
                Callback((string name, object val) => checkoutDetails = (WebOrderCheckoutDetails)val);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            await controller.NewShippingInfo(viewModel, true);

            Assert.That(checkoutDetails != null);
            Assert.That(checkoutDetails.MemberAddressId, Is.EqualTo(addressId));
        }

        [Test]
        public async void NewShippingInfo_DoNotSaveAddressNewSession_AddsViewModelInfoToNewSessionOrderDetails()
        {
            WebOrderCheckoutDetails checkoutDetails = null;

            Address mappedAddress = new Address();

            Mock<AddressViewModel> viewModelMock = new Mock<AddressViewModel>();
            viewModelMock.Setup(vm => vm.MapToNewAddress()).
                Returns(mappedAddress).
                Verifiable();

            var viewModel = viewModelMock.Object;
            viewModel.City = "Waterloo";
            viewModel.CountryCode = "CA";
            viewModel.ProvinceCode = "ON";
            viewModel.POBoxNumber = "1234";
            viewModel.PostalCode = "N2L 6R2";
            viewModel.StreetAddress = "445 Wes Graham Way";

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, GetCountries());
            SetupVeilDataAccessWithProvincesSetupForInclude(dbStub, GetProvinceList(viewModel));
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(null);

            contextStub.
                SetupSet(c => c.HttpContext.Session[CheckoutController.OrderCheckoutDetailsKey] = It.IsAny<WebOrderCheckoutDetails>()).
                Callback((string name, object val) => checkoutDetails = (WebOrderCheckoutDetails)val);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            await controller.NewShippingInfo(viewModel, false);

            Assert.That(
                () => 
                    viewModelMock.Verify(vm => vm.MapToNewAddress(),
                    Times.Once),
                Throws.Nothing);

            Assert.That(checkoutDetails != null);
            Assert.That(checkoutDetails.Address, Is.EqualTo(mappedAddress));
            Assert.That(checkoutDetails.ProvinceCode, Is.EqualTo(viewModel.ProvinceCode));
            Assert.That(checkoutDetails.CountryCode, Is.EqualTo(viewModel.CountryCode));
        }

        [Test]
        public async void NewShippingInfo_ExistingSession_UpdatesAndReassignsOrderDetails()
        {
            WebOrderCheckoutDetails checkoutDetails = new WebOrderCheckoutDetails
            {
                StripeCardToken = "cardToken"
            };

            WebOrderCheckoutDetails setCheckoutDetails = null;

            var viewModel = validAddressViewModel;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, GetCountries());
            SetupVeilDataAccessWithProvincesSetupForInclude(dbStub, GetProvinceList(viewModel));
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(checkoutDetails);

            contextStub.
                SetupSet(c => c.HttpContext.Session[CheckoutController.OrderCheckoutDetailsKey] = It.IsAny<WebOrderCheckoutDetails>()).
                Callback((string name, object val) => setCheckoutDetails = (WebOrderCheckoutDetails)val);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            await controller.NewShippingInfo(viewModel, false);

            Assert.That(setCheckoutDetails != null);
            Assert.That(setCheckoutDetails, Is.SameAs(checkoutDetails));
            Assert.That(setCheckoutDetails.StripeCardToken, Is.SameAs(checkoutDetails.StripeCardToken));
            Assert.That(setCheckoutDetails.ProvinceCode, Is.EqualTo(viewModel.ProvinceCode));
        }

        [Test]
        public async void NewShippingInfo_ReturnToConfirm_RedirectsToConfirmOrder()
        {
            var viewModel = validAddressViewModel;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, GetCountries());
            SetupVeilDataAccessWithProvincesSetupForInclude(dbStub, GetProvinceList(viewModel));
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(null);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.NewShippingInfo(viewModel, false, returnToConfirm: true) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.ConfirmOrder)));
            Assert.That(result.RouteValues["Controller"], Is.Null);
        }

        [Test]
        public async void NewShippingInfo_DoNotReturnToConfirm_RedirectsToBilingInfo()
        {
            var viewModel = validAddressViewModel;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, GetCountries());
            SetupVeilDataAccessWithProvincesSetupForInclude(dbStub, GetProvinceList(viewModel));
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(null);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.NewShippingInfo(viewModel, false, returnToConfirm: false) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.BillingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.Null);
        }
    }
}
