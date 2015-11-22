using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using Microsoft.AspNet.Identity;
using Moq;
using NUnit.Framework;
using Stripe;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;
using Veil.Extensions;
using Veil.Helpers;
using Veil.Models;
using Veil.Services;
using Veil.Services.Interfaces;

namespace Veil.Tests.Controllers
{
    [TestFixture]
    public class CheckoutControllerTests
    {
        private Guid memberId;
        private Guid addressId;
        private Guid creditCardId;
        private Guid cartProduct1Id;
        private Guid cartProduct2Id;
        private AddressViewModel validAddressViewModel;
        private GameProduct cartProduct1;
        private GameProduct cartProduct2;
        private Game game;
        private Platform platform;
        private CartItem newProduct1CartItem;
        private CartItem usedProduct1CartItem;
        private CartItem newProduct2CartItem;
        private Cart cartWithNewAndUsed;
        private MemberCreditCard memberCreditCard;
        private MemberAddress memberAddress;
        private Member member;
        private User memberUser;
        private WebOrderCheckoutDetails validNotSavedShippingDetails;
        private WebOrderCheckoutDetails validNotSavedShippingBillingDetails;
        private WebOrderCheckoutDetails validSavedShippingBillingDetails;
        private Location onlineWarehouse;

        [SetUp]
        public void Setup()
        {
            memberId = new Guid("59EF92BE-D71F-49ED-992D-DF15773DAF98");
            addressId = new Guid("53BE47E4-0C74-4D49-97BB-7246A7880B39");
            creditCardId = new Guid("D9A69026-E3DA-4748-816B-293D9BE3E43F");
            cartProduct1Id = new Guid("3882D242-A62A-4E99-BA11-D6EF340C2EE8");
            cartProduct2Id = new Guid("7413D131-7337-42DC-A7E4-1155EB91E8C9");

            memberAddress = new MemberAddress
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

            game = new Game
            {
                Name = "A game"
            };

            platform = new Platform
            {
                PlatformCode = "XONE",
                PlatformName = "Xbox One"
            };

            cartProduct1 = new PhysicalGameProduct
            {
                Id = cartProduct1Id,
                NewWebPrice = 60.00m,
                ProductAvailabilityStatus = AvailabilityStatus.Available,
                ReleaseDate = new DateTime(635835582902643008L, DateTimeKind.Local),
                UsedWebPrice = 10.00m,
                Game = game,
                Platform = platform
            };

            cartProduct2 = new PhysicalGameProduct
            {
                Id = cartProduct2Id,
                NewWebPrice = 59.99m,
                ProductAvailabilityStatus = AvailabilityStatus.Available,
                ReleaseDate = new DateTime(635837213100050176L, DateTimeKind.Local),
                Game = game,
                Platform = platform
            };

            newProduct1CartItem = new CartItem
            {
                IsNew = true,
                MemberId = memberId,
                Product = cartProduct1,
                ProductId = cartProduct1.Id,
                Quantity = 1
            };

            usedProduct1CartItem = new CartItem
            {
                IsNew = false,
                MemberId = memberId,
                Product = cartProduct1,
                ProductId = cartProduct1.Id,
                Quantity = 1
            };

            newProduct2CartItem = new CartItem
            {
                IsNew = true,
                MemberId = memberId,
                Product = cartProduct2,
                ProductId = cartProduct2.Id,
                Quantity = 1
            };

            validNotSavedShippingDetails = new WebOrderCheckoutDetails
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

            memberCreditCard = new MemberCreditCard
            {
                Id = creditCardId,
                CardholderName = "John Doe",
                ExpiryMonth = 11,
                ExpiryYear = 2015,
                Last4Digits = "4242",
                Member = member,
                MemberId = memberId,
                StripeCardId = "cardToken"
            };

            member = new Member
            {
                UserId = memberId,
                CreditCards = new List<MemberCreditCard>
                {
                    memberCreditCard
                }
            };

            memberUser = new User
            {
                FirstName = "John",
                LastName = "Doe",
                Id = memberId,
                PhoneNumber = "800-555-0199",
            };
            
            validNotSavedShippingBillingDetails = new WebOrderCheckoutDetails
            {
                Address = new Address
                {
                    City = "Waterloo",
                    PostalCode = "N2L 6R2",
                    POBoxNumber = "123",
                    StreetAddress = "445 Wes Graham Way"
                },
                ProvinceCode = "ON",
                CountryCode = "CA",
                StripeCardToken = "card_token"
            };

            validSavedShippingBillingDetails = new WebOrderCheckoutDetails
            {
                MemberCreditCardId = creditCardId,
                MemberAddressId = addressId
            };

            cartWithNewAndUsed = new Cart
            {
                Items = new List<CartItem>
                {
                    newProduct1CartItem,
                    usedProduct1CartItem
                },
                Member = member,
                MemberId = memberId
            };

            onlineWarehouse = new Location
            {
                SiteName = Location.ONLINE_WAREHOUSE_NAME
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
            addressDbSetFake.SetupForInclude();

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
            cartDbSetFake.SetupForInclude();

            dbFake.
                Setup(db => db.Carts).
                Returns(cartDbSetFake.Object);
        }

        private void SetupVeilDataAccessWithProvincesSetupForInclude(Mock<IVeilDataAccess> dbFake, IEnumerable<Province> provinces)
        {
            Mock<DbSet<Province>> provinceDbSetFake = TestHelpers.GetFakeAsyncDbSet(
                provinces.AsQueryable());
            provinceDbSetFake.SetupForInclude();

            dbFake.
                Setup(db => db.Provinces).
                Returns(provinceDbSetFake.Object);
        }

        private void SetupVeilDataAccessWithMember(Mock<IVeilDataAccess> dbFake, Member theMember)
        {
            Mock<DbSet<Member>> memberDbSetFake =
                TestHelpers.GetFakeAsyncDbSet(new List<Member> { theMember }.AsQueryable());

            if (theMember != null)
            {
                memberDbSetFake.
                    Setup(mdb => mdb.FindAsync(theMember.UserId)).
                    ReturnsAsync(theMember);
            }
            
            dbFake.
                Setup(db => db.Members).
                Returns(memberDbSetFake.Object);
        }

        private void SetupVeilDataAccessWithUser(Mock<IVeilDataAccess> dbFake, User user)
        {
            Mock<DbSet<User>> userDbSetFake = TestHelpers.GetFakeAsyncDbSet(new List<User> { user }.AsQueryable());

            dbFake.
                Setup(db => db.Users).
                Returns(userDbSetFake.Object);
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
                Callback<WebOrder>(val => webOrders.Add(val));

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
                memberAddress
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

        private List<Cart> GetCartsListContainingCartWithNewAndUsed()
        {
            return new List<Cart>
            {
                cartWithNewAndUsed
            };
        }

        private List<Province> GetProvinceList(WebOrderCheckoutDetails details)
        {
            List<Province> provinces = new List<Province>
            {
                new Province
                {
                    ProvinceCode = details.ProvinceCode,
                    CountryCode = details.CountryCode,
                    ProvincialTaxRate = 0.08m,
                    Country = new Country { CountryCode = "CA", CountryName = "Canada", FederalTaxRate = 0.05m }
                }
            };

            return provinces;
        }

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

        private Mock<VeilUserManager> GetUserManageStubWithSendEmailSetup(IVeilDataAccess db)
        {
            Mock<VeilUserManager> userManageStub = new Mock<VeilUserManager>(db, null /*messageService*/, null /*dataProtectionProvider*/);
            userManageStub.
                Setup(um => um.SendEmailAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>())).
                Returns(Task.FromResult(0));

            return userManageStub;
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

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Index"));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Cart"));
        }

        [Test]
        public async void ShippingInfo_NonEmptyCart_SetsUpViewModel()
        {
            List<Country> countries = GetCountries();
            List<MemberAddress> addresses = GetMemberAddresses();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, GetCountries());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(null);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.ShippingInfo() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<AddressViewModel>());

            var model = (AddressViewModel) result.Model;

            Assert.That(model.Addresses.Count(), Is.EqualTo(addresses.Count));
            Assert.That(model.Countries, Has.Count.EqualTo(countries.Count));
        }

        [Test]
        public async void ShippingInfo_NonEmptyCartWithNonSavedAddressAlreadyInSession_AddsAddressInfoToViewModel()
        {
            var orderDetails = validNotSavedShippingDetails;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, GetCountries());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(orderDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.ShippingInfo() as ViewResult;

            Assert.That(result != null);
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

        [Test]
        public async void ExistingShippingInfo_EmptyCart_RedirectsToCartIndex()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Cart>> cartDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Cart>().AsQueryable());
            dbStub.
                Setup(db => db.Carts).
                Returns(cartDbSetStub.Object);

            CheckoutController controller = CreateCheckoutController(dbStub.Object);

            var result = await controller.ExistingShippingInfo(addressId) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Index"));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Cart"));
        }

        [Test]
        public async void ExistingShippingInfo_IdNotInDb_RedirectsToShippingInfo()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, new List<MemberAddress>());

            CheckoutController controller = CreateCheckoutController(dbStub.Object);

            var result = await controller.ExistingShippingInfo(addressId) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo("ShippingInfo"));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo(null));
        }

        [Test]
        public async void ExistingShippingInfo_NewSession_AddsNewWebOrderCheckoutDetailsToSession()
        {
            WebOrderCheckoutDetails checkoutDetails = null;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(null);

            contextStub.
                SetupSet(c => c.HttpContext.Session[CheckoutController.OrderCheckoutDetailsKey] = It.IsAny<WebOrderCheckoutDetails>()).
                Callback((string name, object val) => checkoutDetails = (WebOrderCheckoutDetails)val);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            await controller.ExistingShippingInfo(addressId);

            Assert.That(checkoutDetails != null);
            Assert.That(checkoutDetails.MemberAddressId, Is.EqualTo(addressId));
        }

        [Test]
        public async void ExistingShippingInfo_ExistingSession_UpdatesAndReassignsOrderDetails()
        {
            WebOrderCheckoutDetails checkoutDetails = new WebOrderCheckoutDetails
            {
                StripeCardToken = "cardToken"
            };
            WebOrderCheckoutDetails setCheckoutDetails = null;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(checkoutDetails);
            contextStub.
                SetupSet(c => c.HttpContext.Session[CheckoutController.OrderCheckoutDetailsKey] = It.IsAny<WebOrderCheckoutDetails>()).
                Callback((string name, object val) => setCheckoutDetails = (WebOrderCheckoutDetails)val);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            await controller.ExistingShippingInfo(addressId);

            Assert.That(setCheckoutDetails != null);
            Assert.That(setCheckoutDetails, Is.SameAs(checkoutDetails));
            Assert.That(setCheckoutDetails.MemberAddressId, Is.EqualTo(addressId));
        }

        [Test]
        public async void ExistingShippingInfo_ReturnToConfirm_RedirectsToConfirmOrder()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(null);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.ExistingShippingInfo(addressId, returnToConfirm: true) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.ConfirmOrder)));
            Assert.That(result.RouteValues["Controller"], Is.Null);
        }

        [Test]
        public async void ExistingShippingInfo_DoNotReturnToConfirm_RedirectsToBillingInfo()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithAddresses(dbStub, GetMemberAddresses());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(null);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.ExistingShippingInfo(addressId, returnToConfirm: false) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.BillingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.Null);
        }

        [Test]
        public async void BillingInfo_EmptyCart_RedirectsToCartIndex()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Cart>> cartDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Cart>().AsQueryable());
            dbStub.
                Setup(db => db.Carts).
                Returns(cartDbSetStub.Object);

            CheckoutController controller = CreateCheckoutController(dbStub.Object);

            var result = await controller.BillingInfo() as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Index"));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Cart"));
        }

        [Test]
        public async void BillingInfo_AddressNotSetInSession_RedirectsToShippingInfo()
        {
            WebOrderCheckoutDetails checkoutDetails = new WebOrderCheckoutDetails();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(checkoutDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.BillingInfo() as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.ShippingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo(null));
        }

        [Test]
        public async void BillingInfo_NullSessionOrderDetails_RedirectsToShippingInfo()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(null);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.BillingInfo() as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.ShippingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo(null));
        }

        [Test]
        public async void BillingInfo_NewAddressInSession_DisplaysBillingInfo()
        {
            WebOrderCheckoutDetails checkoutDetails = new WebOrderCheckoutDetails
            {
                Address = new Address(),
                CountryCode = "CA",
                ProvinceCode = "ON"
            };

            var viewModel = validAddressViewModel;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, GetCountries());
            SetupVeilDataAccessWithProvincesSetupForInclude(dbStub, new List<Province> { new Province { ProvinceCode = viewModel.ProvinceCode, CountryCode = viewModel.CountryCode } });
            SetupVeilDataAccessWithMember(dbStub, new Member());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(checkoutDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.BillingInfo() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.EqualTo(string.Empty).Or.EqualTo("BillingInfo"));
        }
        
        [Test]
        public async void BillingInfo_ExistingAddressInSession_DisplaysBillingInfo()
        {
            WebOrderCheckoutDetails checkoutDetails = new WebOrderCheckoutDetails
            {
                MemberAddressId = addressId
            };

            var viewModel = validAddressViewModel;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, GetCountries());
            SetupVeilDataAccessWithProvincesSetupForInclude(dbStub, new List<Province> { new Province { ProvinceCode = viewModel.ProvinceCode, CountryCode = viewModel.CountryCode } });
            SetupVeilDataAccessWithMember(dbStub, new Member());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(checkoutDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.BillingInfo() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.EqualTo(string.Empty).Or.EqualTo("BillingInfo"));
        }

        [Test]
        public async void BillingInfo_ValidState_SetsUpCreditCardsAndCountriesOnViewModel()
        {
            Member memberWithCreditCards = member;
            memberWithCreditCards.CreditCards = new List<MemberCreditCard> { memberCreditCard };

            List<Country> countries = GetCountries();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithCountriesSetupForInclude(dbStub, countries);
            SetupVeilDataAccessWithProvincesSetupForInclude(dbStub, new List<Province>());
            SetupVeilDataAccessWithMember(dbStub, memberWithCreditCards);
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.BillingInfo() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<BillingInfoViewModel>());

            var model = (BillingInfoViewModel) result.Model;

            Assert.That(model.CreditCards.Count(), Is.EqualTo(memberWithCreditCards.CreditCards.Count));
            Assert.That(model.Countries, Has.Count.EqualTo(countries.Count));
        }

        [Test]
        public async void NewBillingInfo_EmptyCart_RedirectsToCartIndex()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Cart>> cartDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Cart>().AsQueryable());
            dbStub.
                Setup(db => db.Carts).
                Returns(cartDbSetStub.Object);

            CheckoutController controller = CreateCheckoutController(dbStub.Object);

            var result = await controller.NewBillingInfo(null, false) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Index"));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Cart"));
        }

        [Test]
        public async void NewBillingInfo_AddressNotSetInSession_RedirectsToShippingInfo()
        {
            WebOrderCheckoutDetails checkoutDetails = new WebOrderCheckoutDetails();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(checkoutDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.NewBillingInfo(null, false) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.ShippingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo(null));
        }

        [Test]
        public async void NewBillingInfo_NullSessionOrderDetails_RedirectsToShippingInfo()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(null);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.NewBillingInfo(null, false) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.ShippingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo(null));
        }

        [Test]
        public async void NewBillingInfo_NullOrWhiteSpaceStripeToken_RedirectsToBillingInfo([Values(null, "", " ")]string cardToken)
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.NewBillingInfo(cardToken, false) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.BillingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo(null));
        }

        [Test]
        public async void NewBillingInfo_SaveCardButMemberIdNotInDb_ReturnsInternalServerErrorStatusCode()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithMember(dbStub, new Member());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.NewBillingInfo("cardToken", saveCard: true) as HttpStatusCodeResult;

            Assert.That(result != null);
            Assert.That(result.StatusCode, Is.GreaterThanOrEqualTo((int)HttpStatusCode.InternalServerError));
        }

        [Test]
        public async void NewBillingInfo_SaveCard_CallsStripeServiceCreateCardWithMemberCustomerIdAndPassedCardToken()
        {
            Member currentMember = member;
            string cardToken = "cardToken";

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithMember(dbStub, currentMember);
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingDetails);

            StripeException exception = new StripeException(
                HttpStatusCode.BadRequest,
                new StripeError
                {
                    Code = "Any"
                },
                "message");

            Mock<IStripeService> stripeServiceMock = new Mock<IStripeService>();
            stripeServiceMock.
                Setup(s => s.CreateCreditCard(It.IsAny<Member>(), It.IsAny<string>())).
                Throws(exception).
                Verifiable();

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object, stripeService: stripeServiceMock.Object);

            await controller.NewBillingInfo(cardToken, saveCard: true);

            Assert.That(
                () => 
                    stripeServiceMock.Verify(s => s.CreateCreditCard(currentMember, cardToken),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void NewBillingInfo_StripeExceptionCardError_AddsCardErrorMessageToModelState()
        {
            Member currentMember = member;
            string cardToken = "cardToken";
            string stripeErrorMessage = "A card error message";

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithMember(dbStub, currentMember);
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingDetails);

            StripeException exception = new StripeException(
                HttpStatusCode.BadRequest,
                new StripeError
                {
                    Code = "card_error"
                },
                stripeErrorMessage);

            Mock<IStripeService> stripeServiceMock = new Mock<IStripeService>();
            stripeServiceMock.
                Setup(s => s.CreateCreditCard(It.IsAny<Member>(), It.IsAny<string>())).
                Throws(exception);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object, stripeService: stripeServiceMock.Object);

            await controller.NewBillingInfo(cardToken, saveCard: true);

            Assert.That(controller.ModelState[ManageController.STRIPE_ISSUES_MODELSTATE_KEY].Errors, Has.Some.Matches<ModelError>(modelError => modelError.ErrorMessage == stripeErrorMessage));
        }

        [Test]
        public async void NewBillingInfo_StripeException_RedisplaysBillingInfo()
        {
            Member currentMember = member;
            string cardToken = "cardToken";
            string stripeErrorMessage = "A card error message";

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithMember(dbStub, currentMember);
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingDetails);

            StripeException exception = new StripeException(
                HttpStatusCode.BadRequest,
                new StripeError
                {
                    Code = "card_error"
                },
                stripeErrorMessage);

            Mock<IStripeService> stripeServiceMock = new Mock<IStripeService>();
            stripeServiceMock.
                Setup(s => s.CreateCreditCard(It.IsAny<Member>(), It.IsAny<string>())).
                Throws(exception);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object, stripeService: stripeServiceMock.Object);

            var result = await controller.NewBillingInfo(cardToken, saveCard: true) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.BillingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.Null);
        }

        [Test]
        public async void NewBillingInfo_SaveCard_AddsCardToMembersCreditCards()
        {
            Member currentMember = member;
            string cardToken = "cardToken";
            MemberCreditCard newCard = new MemberCreditCard();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithMember(dbStub, currentMember);

            Mock<ICollection<MemberCreditCard>> creditCardsListMock = new Mock<ICollection<MemberCreditCard>>();
            creditCardsListMock.
                Setup(cc => cc.Add(It.IsAny<MemberCreditCard>())).
                Verifiable();

            currentMember.CreditCards = creditCardsListMock.Object;

            Mock<IStripeService> stripeServiceMock = new Mock<IStripeService>();
            stripeServiceMock.
                Setup(s => s.CreateCreditCard(It.IsAny<Member>(), It.IsAny<string>())).
                Returns(newCard);

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object, stripeService: stripeServiceMock.Object);

            await controller.NewBillingInfo(cardToken, saveCard: true);

            Assert.That(
                () => 
                    creditCardsListMock.Verify(cc => cc.Add(newCard),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void NewBillingInfo_SaveCard_CallsSaveChangesAsync()
        {
            Member currentMember = member;
            currentMember.CreditCards = new List<MemberCreditCard>();
            string cardToken = "cardToken";
            MemberCreditCard newCard = new MemberCreditCard();

            Mock<IVeilDataAccess> dbMock = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbMock, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithMember(dbMock, currentMember);

            dbMock.
                Setup(db => db.SaveChangesAsync()).
                ReturnsAsync(1).
                Verifiable();

            Mock<IStripeService> stripeServiceMock = new Mock<IStripeService>();
            stripeServiceMock.
                Setup(s => s.CreateCreditCard(It.IsAny<Member>(), It.IsAny<string>())).
                Returns(newCard);

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingDetails);

            CheckoutController controller = CreateCheckoutController(dbMock.Object, context: contextStub.Object, stripeService: stripeServiceMock.Object);

            await controller.NewBillingInfo(cardToken, saveCard: true);

            Assert.That(
                () =>
                    dbMock.Verify(cc => cc.SaveChangesAsync(),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void NewBillingInfo_SaveCard_AddsCardIdToSessionOrderDetails()
        {
            WebOrderCheckoutDetails details = validNotSavedShippingDetails;

            Member currentMember = member;
            string cardToken = "cardToken";
            Guid cardId = creditCardId;
            MemberCreditCard newCard = new MemberCreditCard();

            Mock<IVeilDataAccess> dbMock = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbMock, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithMember(dbMock, currentMember);

            Mock<ICollection<MemberCreditCard>> creditCardsListMock = new Mock<ICollection<MemberCreditCard>>();
            creditCardsListMock.
                Setup(cc => cc.Add(It.IsAny<MemberCreditCard>())).
                Callback<MemberCreditCard>(
                    val =>
                    {
                        val.Id = cardId;
                    });

            currentMember.CreditCards = creditCardsListMock.Object;

            dbMock.
                Setup(db => db.SaveChangesAsync()).
                ReturnsAsync(1);

            Mock<IStripeService> stripeServiceMock = new Mock<IStripeService>();
            stripeServiceMock.
                Setup(s => s.CreateCreditCard(It.IsAny<Member>(), It.IsAny<string>())).
                Returns(newCard);

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(details);

            CheckoutController controller = CreateCheckoutController(dbMock.Object, context: contextStub.Object, stripeService: stripeServiceMock.Object);

            await controller.NewBillingInfo(cardToken, saveCard: true);

            Assert.That(details.MemberCreditCardId, Is.EqualTo(cardId));
        }

        [Test]
        public async void NewBillingInfo_DoNotSaveCard_AddsPassedTokenToSessionOrderDetails()
        {
            WebOrderCheckoutDetails details = validNotSavedShippingDetails;

            Member currentMember = member;
            string cardToken = "cardToken";

            Mock<IVeilDataAccess> dbMock = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbMock, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithMember(dbMock, currentMember);

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(details);

            CheckoutController controller = CreateCheckoutController(dbMock.Object, context: contextStub.Object);

            await controller.NewBillingInfo(cardToken, saveCard: false);

            Assert.That(details.StripeCardToken, Is.EqualTo(cardToken));
        }

        [Test]
        public async void NewBillingInfo_ValidState_ReassignsUpdatedOrderDetails()
        {
            WebOrderCheckoutDetails details = validNotSavedShippingDetails;
            WebOrderCheckoutDetails setDetails = null;

            Member currentMember = member;
            string cardToken = "cardToken";

            Mock<IVeilDataAccess> dbMock = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbMock, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithMember(dbMock, currentMember);

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(details);
            contextStub.
                SetupSet(c => c.HttpContext.Session[CheckoutController.OrderCheckoutDetailsKey] = It.IsAny<WebOrderCheckoutDetails>()).
                Callback((string name, object val) => setDetails = (WebOrderCheckoutDetails)val);

            CheckoutController controller = CreateCheckoutController(dbMock.Object, context: contextStub.Object);

            await controller.NewBillingInfo(cardToken, saveCard: false);

            Assert.That(setDetails, Is.SameAs(details));
        }

        [Test]
        public async void NewBillingInfo_ValidState_RedirectsToConfirmOrder()
        {
            WebOrderCheckoutDetails details = validNotSavedShippingDetails;

            Member currentMember = member;
            string cardToken = "cardToken";

            Mock<IVeilDataAccess> dbMock = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbMock, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithMember(dbMock, currentMember);

            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(details);

            CheckoutController controller = CreateCheckoutController(dbMock.Object, context: contextStub.Object);

            var result = await controller.NewBillingInfo(cardToken, saveCard: false) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.ConfirmOrder)));
            Assert.That(result.RouteValues["Controller"], Is.Null);
        }

        [Test]
        public async void ExistingBillingInfo_EmptyCart_RedirectsToCartIndex()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Cart>> cartDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Cart>().AsQueryable());
            dbStub.
                Setup(db => db.Carts).
                Returns(cartDbSetStub.Object);

            CheckoutController controller = CreateCheckoutController(dbStub.Object);

            var result = await controller.ExistingBillingInfo(creditCardId) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Index"));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Cart"));
        }

        [Test]
        public async void ExistingBillingInfo_AddressNotSetInSession_RedirectsToShippingInfo()
        {
            WebOrderCheckoutDetails checkoutDetails = new WebOrderCheckoutDetails();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(checkoutDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.ExistingBillingInfo(creditCardId) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.ShippingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo(null));
        }

        [Test]
        public async void ExistingBillingInfo_NullSessionOrderDetails_RedirectsToShippingInfo()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(null);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.ExistingBillingInfo(creditCardId) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.ShippingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo(null));
        }

        [Test]
        public async void ExistingBillingInfo_IdNotInDb_RedirectsToBillingInfo()
        {
            member.CreditCards = new List<MemberCreditCard>();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithMember(dbStub, member);
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingDetails);
            
            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.ExistingBillingInfo(creditCardId) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.BillingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo(null));
        }

        [Test]
        public async void ExistingBillingInfo_IdInDb_AddsCardIdToOrderDetailsAndReassignsToSession()
        {
            WebOrderCheckoutDetails details = validNotSavedShippingDetails;
            WebOrderCheckoutDetails setDetails = null;

            member.CreditCards = new List<MemberCreditCard> { memberCreditCard };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithMember(dbStub, member);
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(details);
            contextStub.
                SetupSet(c => c.HttpContext.Session[CheckoutController.OrderCheckoutDetailsKey] = It.IsAny<WebOrderCheckoutDetails>()).
                Callback((string name, object val) => setDetails = (WebOrderCheckoutDetails)val);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            await controller.ExistingBillingInfo(memberCreditCard.Id);

            //Assert.That(setDetails, Is.Not.Null);
            Assert.That(setDetails, Is.SameAs(details));
            Assert.That(details.MemberCreditCardId, Is.EqualTo(memberCreditCard.Id));
        }

        [Test]
        public async void ExistingBillingInfo_ValidState_RedirectsToConfirmOrder()
        {
            member.CreditCards = new List<MemberCreditCard> { memberCreditCard };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithMember(dbStub, member);
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingDetails);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object);

            var result = await controller.ExistingBillingInfo(memberCreditCard.Id) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.ConfirmOrder)));
            Assert.That(result.RouteValues["Controller"], Is.Null);
        }

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
                Throws<StripeException>();

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object, stripeService: stripeServiceMock.Object);

            var result = await controller.ConfirmOrder() as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.BillingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.Null);
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
                Throws<StripeException>();

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object, stripeService: stripeServiceMock.Object);

            var result = await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList()) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(CheckoutController.BillingInfo)));
            Assert.That(result.RouteValues["Controller"], Is.Null);
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
                Throws<StripeException>(); // Throw to end execution early

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
                Throws<StripeException>(); // Throw to end execution early

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
                Throws<StripeException>(); // Throw to end execution early

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
                Throws<StripeException>(); // Throw to end execution early

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
                Throws<StripeException>(); // Throw to end execution early

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
                Throws<StripeException>(); // Throw to end execution early

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
                Throws<StripeException>().
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
                Throws<StripeException>().
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
                Throws<StripeException>().
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
                Throws<StripeException>().
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
                Throws<StripeException>(); // Throw to end execution early

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
                Throws<StripeException>();

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
        public async void PlaceOrder_SuccessfulCharge_AddsChargeIdToWebOrder()
        {
            string stripeChargeId = "chargeToken";

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

            CheckoutController controller = CreateCheckoutController(
                dbStub.Object,
                context: contextStub.Object,
                stripeService: stripeServiceStub.Object,
                shippingCostService: shippingServiceStub.Object);

            await controller.PlaceOrder(cartWithNewAndUsed.Items.ToList());

            WebOrder newOrder = webOrders.FirstOrDefault();

            Assert.That(newOrder != null);
            Assert.That(newOrder.StripeChargeId, Is.EqualTo(stripeChargeId));
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
