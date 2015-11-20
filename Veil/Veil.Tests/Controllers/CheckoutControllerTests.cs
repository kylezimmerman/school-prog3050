using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using JetBrains.Annotations;
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
        private AddressViewModel validAddressViewModel;
        private GameProduct cartProduct;
        private CartItem cartItem;
        private WebOrderCheckoutDetails validShippingDetails;
        private WebOrderCheckoutDetails validBillingDetails;
        private WebOrderCheckoutDetails validConfirmDetails;

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

            validShippingDetails = new WebOrderCheckoutDetails
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

            validAddressViewModel = new AddressViewModel
            {
                City = "Waterloo",
                CountryCode = "CA",
                ProvinceCode = "ON",
                POBoxNumber = "1234",
                PostalCode = "N2L 6R2",
                StreetAddress = "445 Wes Graham Way"
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

        private void SetupVeilDataAccessWithAddresses(Mock<IVeilDataAccess> dbFake, [NotNull] IEnumerable<MemberAddress> addresses)
        {
            Mock<DbSet<MemberAddress>> addressDbSetFake =
                TestHelpers.GetFakeAsyncDbSet(addresses.AsQueryable());

            dbFake.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetFake.Object);
        }

        private void SetupVeilDataAccessWithCountriesSetupForInclude(
            Mock<IVeilDataAccess> dbFake, [NotNull]IEnumerable<Country> addresses)
        {
            Mock<DbSet<Country>> countriesDbSetFake =
                TestHelpers.GetFakeAsyncDbSet(addresses.AsQueryable());
            countriesDbSetFake.SetupForInclude();

            dbFake.
                Setup(db => db.Countries).
                Returns(countriesDbSetFake.Object);
        }

        private void SetupVeilDataAccessWithCarts(Mock<IVeilDataAccess> dbFake, [NotNull]IEnumerable<Cart> carts)
        {
            Mock<DbSet<Cart>> cartDbSetFake = TestHelpers.GetFakeAsyncDbSet(carts.AsQueryable());

            dbFake.
                Setup(db => db.Carts).
                Returns(cartDbSetFake.Object);
        }

        private void SetupVeilDataAccessWithProvinces(Mock<IVeilDataAccess> dbFake, IEnumerable<Province> provinces)
        {
            Mock<DbSet<Province>> provinceDbSetFake = TestHelpers.GetFakeAsyncDbSet(
                provinces.AsQueryable());

            dbFake.
                Setup(db => db.Provinces).
                Returns(provinceDbSetFake.Object);
        } 

        private Mock<ControllerContext> GetControllerContextWithSessionSetupToReturn(WebOrderCheckoutDetails returnValue)
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

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListWithValidMemberCart());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, GetCountries());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
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
            var orderDetails = validShippingDetails;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListWithValidMemberCart());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, GetCountries());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
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

            Assert.That(result, Is.Not.Null);
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
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListWithValidMemberCart());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, countries);
            SetupVeilDataAccessWithAddresses(dbStub, addresses);
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validShippingDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);
            controller.ModelState.AddModelError(nameof(AddressViewModel.ProvinceCode), "Invalid");

            var result = await controller.NewShippingInfo(viewModel, false) as ViewResult;

            Assert.That(result, Is.Not.Null);
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

        [TestCase("CA")]
        [TestCase("US")]
        public async void NewShippingInfo_InvalidPostalCodeModelStateWithCountryCodeSupplied_ReplacesErrorMessage(string countryCode)
        {
            AddressViewModel viewModel = new AddressViewModel
            {
                CountryCode = countryCode
            };

            string postalCodeErrorMessage = "Required";

            List<Country> countries = GetCountries();
            List<MemberAddress> addresses = GetMemberAddresses();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListWithValidMemberCart());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, countries);
            SetupVeilDataAccessWithAddresses(dbStub, addresses);
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validShippingDetails);

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
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListWithValidMemberCart());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, countries);
            SetupVeilDataAccessWithAddresses(dbStub, addresses);
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validShippingDetails);

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
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListWithValidMemberCart());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, new List<Country>());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validShippingDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.NewShippingInfo(viewModel, false) as ViewResult;

            Assert.That(result, Is.Not.Null);
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
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListWithValidMemberCart());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, countries);
            SetupVeilDataAccessWithProvinces(dbStub, provinces);
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validShippingDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.NewShippingInfo(viewModel, false) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Model, Is.InstanceOf<AddressViewModel>());

            var model = (AddressViewModel)result.Model;

            Assert.That(model, Is.EqualTo(viewModel));
        }

        [TestCase("NO", "ON")]
        [TestCase("CA", "NO")]
        public async void NewShippingInfo_InvalidCountryOrProvince_SetsUpAddressesAndCountriesOnViewModel(string countryCode, string provinceCode)
        {
            var viewModel = validAddressViewModel;
            viewModel.CountryCode = countryCode;
            viewModel.ProvinceCode = provinceCode;

            List<Country> countries = GetCountries();
            List<Province> provinces = new List<Province>();
            List<MemberAddress> addresses = GetMemberAddresses();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListWithValidMemberCart());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, countries);
            SetupVeilDataAccessWithAddresses(dbStub, addresses);
            SetupVeilDataAccessWithProvinces(dbStub, provinces);
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validShippingDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.NewShippingInfo(viewModel, false) as ViewResult;

            Assert.That(result, Is.Not.Null);
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
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListWithValidMemberCart());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, GetCountries());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            SetupVeilDataAccessWithProvinces(dbStub, new List<Province> { new Province { ProvinceCode = viewModel.ProvinceCode, CountryCode = viewModel.CountryCode} });
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
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListWithValidMemberCart());
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

            SetupVeilDataAccessWithProvinces(dbStub, new List<Province> { new Province { ProvinceCode = viewModel.ProvinceCode, CountryCode = viewModel.CountryCode } });
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

            Assert.That(newAddress, Is.Not.Null);
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
            SetupVeilDataAccessWithCarts(dbMock, GetCartsListWithValidMemberCart());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbMock, GetCountries());
            SetupVeilDataAccessWithAddresses(dbMock, new List<MemberAddress>());

            dbMock.
                Setup(db => db.SaveChangesAsync()).
                ReturnsAsync(1).
                Verifiable();

            SetupVeilDataAccessWithProvinces(dbMock, new List<Province> { new Province { ProvinceCode = viewModel.ProvinceCode, CountryCode = viewModel.CountryCode } });
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
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListWithValidMemberCart());
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

            SetupVeilDataAccessWithProvinces(dbStub, new List<Province> { new Province { ProvinceCode = viewModel.ProvinceCode, CountryCode = viewModel.CountryCode } });
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();
            contextStub.
                SetupGet(c => c.HttpContext.Session[CheckoutController.OrderCheckoutDetailsKey]).
                Returns(null);

            contextStub.
                SetupSet(c => c.HttpContext.Session[CheckoutController.OrderCheckoutDetailsKey] = It.IsAny<WebOrderCheckoutDetails>()).
                Callback((string name, object val) => checkoutDetails = (WebOrderCheckoutDetails)val);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            await controller.NewShippingInfo(viewModel, true);

            Assert.That(checkoutDetails, Is.Not.Null);
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
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListWithValidMemberCart());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, GetCountries());
            SetupVeilDataAccessWithProvinces(dbStub, new List<Province> { new Province { ProvinceCode = viewModel.ProvinceCode, CountryCode = viewModel.CountryCode } });
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();
            contextStub.
                SetupGet(c => c.HttpContext.Session[CheckoutController.OrderCheckoutDetailsKey]).
                Returns(null);

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

            Assert.That(checkoutDetails, Is.Not.Null);
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
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListWithValidMemberCart());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, GetCountries());
            SetupVeilDataAccessWithProvinces(dbStub, new List<Province> { new Province { ProvinceCode = viewModel.ProvinceCode, CountryCode = viewModel.CountryCode } });
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();
            contextStub.
                SetupGet(c => c.HttpContext.Session[CheckoutController.OrderCheckoutDetailsKey]).
                Returns(checkoutDetails);

            contextStub.
                SetupSet(c => c.HttpContext.Session[CheckoutController.OrderCheckoutDetailsKey] = It.IsAny<WebOrderCheckoutDetails>()).
                Callback((string name, object val) => setCheckoutDetails = (WebOrderCheckoutDetails)val);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            await controller.NewShippingInfo(viewModel, false);

            Assert.That(setCheckoutDetails, Is.Not.Null);
            Assert.That(setCheckoutDetails, Is.SameAs(checkoutDetails));
            Assert.That(setCheckoutDetails.StripeCardToken, Is.SameAs(checkoutDetails.StripeCardToken));
            Assert.That(setCheckoutDetails.ProvinceCode, Is.EqualTo(viewModel.ProvinceCode));
        }

        [Test]
        public async void NewShippingInfo_ReturnToConfirm_RedirectsToConfirmOrder()
        {
            var viewModel = validAddressViewModel;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListWithValidMemberCart());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, GetCountries());
            SetupVeilDataAccessWithProvinces(dbStub, new List<Province> { new Province { ProvinceCode = viewModel.ProvinceCode, CountryCode = viewModel.CountryCode } });
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();
            contextStub.
                SetupGet(c => c.HttpContext.Session[CheckoutController.OrderCheckoutDetailsKey]).
                Returns(null);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.NewShippingInfo(viewModel, false, returnToConfirm: true) as RedirectToRouteResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.ConfirmOrder)));
            Assert.That(result.RouteValues["Controller"], Is.Null);
        }

        [Test]
        public async void NewShippingInfo_DoNotReturnToConfirm_RedirectsToBilingInfo()
        {
            var viewModel = validAddressViewModel;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListWithValidMemberCart());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, GetCountries());
            SetupVeilDataAccessWithProvinces(dbStub, new List<Province> { new Province { ProvinceCode = viewModel.ProvinceCode, CountryCode = viewModel.CountryCode } });
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();
            contextStub.
                SetupGet(c => c.HttpContext.Session[CheckoutController.OrderCheckoutDetailsKey]).
                Returns(null);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.NewShippingInfo(viewModel, false, returnToConfirm: false) as RedirectToRouteResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.BillingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.Null);
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
