using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Security.Principal;
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
using Veil.DataModels.Validation;
using Veil.Helpers;
using Veil.Models;
using Veil.Services;
using Veil.Services.Interfaces;

namespace Veil.Tests.Controllers
{
    [TestFixture]
    public class ManageControllerTests
    {
        private Guid memberId;
        private Guid addressId;

        [SetUp]
        public void Setup()
        {
            memberId = new Guid("59EF92BE-D71F-49ED-992D-DF15773DAF98");
            addressId = new Guid("53BE47E4-0C74-4D49-97BB-7246A7880B39");
        }

        private ManageController CreateManageController(
            VeilUserManager userManager = null, VeilSignInManager signInManager = null,
            IVeilDataAccess veilDataAccess = null, IGuidUserIdGetter idGetter = null,
            IStripeService stripeService = null)
        {
            return new ManageController(userManager, signInManager, veilDataAccess, idGetter, stripeService);
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

        private Mock<IVeilDataAccess> SetupVeilDataAccessFakeWithCountriesAndAddresses(List<Country> countries = null, List<MemberAddress> addresses = null)
        {
            addresses = addresses ?? new List<MemberAddress>();

            Mock<IVeilDataAccess> dbStub = SetupVeilDataAccessFakeWithCountries(countries);
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(addresses.AsQueryable());

            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);

            return dbStub;
        }

        private Mock<IVeilDataAccess> SetupVeilDataAccessFakeWithCountries(List<Country> countries = null)
        {
            countries = countries ?? new List<Country>();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Country>> countryDbSetStub = TestHelpers.GetFakeAsyncDbSet(countries.AsQueryable());
            countryDbSetStub.SetupForInclude();

            dbStub.
                Setup(db => db.Countries).
                Returns(countryDbSetStub.Object);

            return dbStub;
        }

        [Test]
        public async void ManageAddresses_WhenCalled_SetsUpViewModel()
        {
            List<Country> countries = GetCountries();
            List<MemberAddress> addresses = GetMemberAddresses();

            Mock<IVeilDataAccess> dbStub = SetupVeilDataAccessFakeWithCountriesAndAddresses(countries, addresses);

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object);

            controller.ControllerContext = contextStub.Object;

            var result = await controller.ManageAddresses() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<AddressViewModel>());

            var model = (AddressViewModel) result.Model;

            Assert.That(model.Countries, Is.Not.Empty);
            Assert.That(model.Countries, Has.Count.EqualTo(countries.Count));
            Assert.That(model.Addresses, Is.Not.Empty);
            Assert.That(model.Addresses.Count(), Is.EqualTo(addresses.Count));
        }

        [Test]
        public async void ManageAddresses_WhenCalled_OnlyRetrievesMatchingMemberAddresses()
        {
            List<MemberAddress> matchingAddresses = GetMemberAddresses();

            List<MemberAddress> allAddresses = new List<MemberAddress>(matchingAddresses)
            {
                new MemberAddress()
            };

            Mock<IVeilDataAccess> dbStub = SetupVeilDataAccessFakeWithCountriesAndAddresses(addresses: allAddresses);

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object);
            controller.ControllerContext = contextStub.Object;
            
            var result = await controller.ManageAddresses() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<AddressViewModel>());

            var model = (AddressViewModel)result.Model;

            Assert.That(model.Addresses, Is.Not.Empty);
            Assert.That(model.Addresses.Count(), Is.EqualTo(matchingAddresses.Count));
        }

        [Test]
        public async void ManageAddresses_WhenCalled_IncludesProvinces()
        {
            List<Country> countries = GetCountries();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());

            Mock<DbSet<Country>> countryDbSetMock = TestHelpers.GetFakeAsyncDbSet(countries.AsQueryable());
            countryDbSetMock.
                Setup(cdb => cdb.Include(It.IsAny<string>())).
                Returns(countryDbSetMock.Object).
                Verifiable();

            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);
            dbStub.
                Setup(db => db.Countries).
                Returns(countryDbSetMock.Object);

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object);
            controller.ControllerContext = contextStub.Object;

            await controller.ManageAddresses();

            Assert.That(
                () => 
                    countryDbSetMock.Verify<DbQuery<Country>>(cdb => cdb.Include(It.Is<string>(val => val.Contains(nameof(IVeilDataAccess.Provinces)))),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void CreateAddress_InvalidModelState_RedisplaysViewWithSameViewModel()
        {
            AddressViewModel viewModel = new AddressViewModel();

            Mock<IVeilDataAccess> dbStub = SetupVeilDataAccessFakeWithCountriesAndAddresses();

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object);
            controller.ControllerContext = contextStub.Object;

            controller.ModelState.AddModelError(nameof(AddressViewModel.City), "Required");

            var result = await controller.CreateAddress(viewModel) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.EqualTo(viewModel));
        }

        [TestCase("CA")]
        [TestCase("US")]
        public async void CreateAddress_InvalidPostalCodeModelStateWithCountryCodeSupplied_ReplacesErrorMessage(string countryCode)
        {
            AddressViewModel viewModel = new AddressViewModel
            {
                CountryCode = countryCode
            };

            string postalCodeErrorMessage = "Required";

            Mock<IVeilDataAccess> dbStub = SetupVeilDataAccessFakeWithCountriesAndAddresses();

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object);
            controller.ControllerContext = contextStub.Object;

            controller.ModelState.AddModelError(nameof(AddressViewModel.PostalCode), postalCodeErrorMessage);

            await controller.CreateAddress(viewModel);

            Assert.That(controller.ModelState[nameof(AddressViewModel.PostalCode)].Errors, Has.None.Matches<ModelError>(modelError => modelError.ErrorMessage == postalCodeErrorMessage));
        }

        [Test]
        public async void CreateAddress_InvalidPostalCodeModelStateWithoutCountryCodeSupplied_LeavesErrorMessage()
        {
            AddressViewModel viewModel = new AddressViewModel();

            string postalCodeErrorMessage = "Required";

            Mock<IVeilDataAccess> dbStub = SetupVeilDataAccessFakeWithCountriesAndAddresses();

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object);
            controller.ControllerContext = contextStub.Object;

            controller.ModelState.AddModelError(nameof(AddressViewModel.PostalCode), postalCodeErrorMessage);

            await controller.CreateAddress(viewModel);

            Assert.That(controller.ModelState[nameof(AddressViewModel.PostalCode)].Errors, Has.Some.Matches<ModelError>(modelError => modelError.ErrorMessage == postalCodeErrorMessage));
        }

        [Test]
        public async void CreateAddress_InvalidModelState_SetsUpViewModelWithCountries()
        {
            AddressViewModel viewModel = new AddressViewModel();

            List<Country> countries = GetCountries();
            List<MemberAddress> addresses = GetMemberAddresses();

            Mock<IVeilDataAccess> dbStub = SetupVeilDataAccessFakeWithCountriesAndAddresses(countries, addresses);

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object);
            controller.ControllerContext = contextStub.Object;

            controller.ModelState.AddModelError(nameof(AddressViewModel.City), "Required");

            var result = await controller.CreateAddress(viewModel) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.EqualTo(viewModel));
            Assert.That(result.Model, Is.InstanceOf<AddressViewModel>());
            Assert.That(viewModel.Countries, Is.Not.Empty);
            Assert.That(viewModel.Countries, Has.Count.EqualTo(countries.Count));
            Assert.That(viewModel.Addresses, Is.Not.Empty);
            Assert.That(viewModel.Addresses.Count(), Is.EqualTo(addresses.Count));
        }

        [Test]
        public async void CreateAddress_ValidModel_MapsViewModelToNewModel()
        {
            MemberAddress newAddress = null;
            Guid currentMemberId = memberId;
            
            AddressViewModel viewModel = new AddressViewModel
            {
                City = "Waterloo",
                CountryCode = "CA",
                ProvinceCode = "ON",
                PostalCode = "N2L 6R2",
                StreetAddress = "445 Wes Graham Way",
                POBoxNumber = "123"
            };
            
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.Add(It.IsAny<MemberAddress>())).
                Returns<MemberAddress>(ma => ma).
                Callback<MemberAddress>(ma => newAddress = ma);

            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(currentMemberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object);
            controller.ControllerContext = contextStub.Object;

            await controller.CreateAddress(viewModel);

            Assert.That(newAddress != null);
            Assert.That(newAddress.MemberId, Is.EqualTo(currentMemberId));
            Assert.That(newAddress.Address.City, Is.EqualTo(viewModel.City));
            Assert.That(newAddress.CountryCode, Is.EqualTo(viewModel.CountryCode));
            Assert.That(newAddress.ProvinceCode, Is.EqualTo(viewModel.ProvinceCode));
            Assert.That(newAddress.Address.PostalCode, Is.EqualTo(viewModel.PostalCode));
            Assert.That(newAddress.Address.StreetAddress, Is.EqualTo(viewModel.StreetAddress));
            Assert.That(newAddress.Address.POBoxNumber, Is.EqualTo(viewModel.POBoxNumber));
        }

        [TestCase("N2L-6R2", "CA")]
        [TestCase("n2l-6r2", "CA")]
        [TestCase("N2L6R2", "CA")]
        [TestCase("n2l6r2", "CA")]
        [TestCase("12345 6789", "US")]
        public async void CreateAddress_ValidModel_ReformatsPostalCodeToMatchStoredPostalCodeRegex(string postalCode, string countryCode)
        {
            MemberAddress newAddress = null;
            Guid currentMemberId = memberId;

            AddressViewModel viewModel = new AddressViewModel
            {
                City = "Waterloo",
                CountryCode = countryCode,
                ProvinceCode = "ON",
                PostalCode = postalCode,
                StreetAddress = "445 Wes Graham Way"
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.Add(It.IsAny<MemberAddress>())).
                Returns<MemberAddress>(ma => ma).
                Callback<MemberAddress>(ma => newAddress = ma);

            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(currentMemberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object);
            controller.ControllerContext = contextStub.Object;

            await controller.CreateAddress(viewModel);

            Assert.That(newAddress != null);
            Assert.That(newAddress.Address.PostalCode, Is.StringMatching(ValidationRegex.STORED_POSTAL_CODE));
        }

        [Test]
        public async void CreateAddress_ValidModel_CallsSaveChangesAsync()
        {
            AddressViewModel viewModel = new AddressViewModel();

            Mock<IVeilDataAccess> dbMock = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.Add(It.IsAny<MemberAddress>())).
                Returns<MemberAddress>(ma => ma);

            dbMock.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);
            dbMock.
                Setup(db => db.SaveChangesAsync()).
                ReturnsAsync(1).
                Verifiable();

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbMock.Object, idGetter: idGetterStub.Object);
            controller.ControllerContext = contextStub.Object;

            await controller.CreateAddress(viewModel);

            Assert.That(
                () => 
                    dbMock.Verify(db => db.SaveChangesAsync(),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public void CreateAddress_SaveChangesAsyncThrowingProvinceForeignKeyViolationException_HandlesException()
        {
            AddressViewModel viewModel = new AddressViewModel();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.Add(It.IsAny<MemberAddress>())).
                Returns<MemberAddress>(ma => ma);

            Mock<DbSet<Country>> countryDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Country>().AsQueryable());
            countryDbSetStub.SetupForInclude();

            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);
            dbStub.
                Setup(db => db.Countries).
                Returns(countryDbSetStub.Object);

            DbUpdateException provinceConstraintException = new DbUpdateException("See inner",
                SqlExceptionCreator.Create( // This message was copied verbatim from the actual exception being thrown
                    "The INSERT statement conflicted with the FOREIGN KEY constraint " +
                    "\"FK_dbo.MemberAddress_dbo.Province_ProvinceCode_CountryCode\". " +
                    "The conflict occurred in database \"prog3050\", table " +
                    "\"dbo.Province\".\r\nThe statement has been terminated.",
                    (int)SqlErrorNumbers.ConstraintViolation));

            dbStub.
                Setup(db => db.SaveChangesAsync()).
                ThrowsAsync(provinceConstraintException);

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object);
            controller.ControllerContext = contextStub.Object;

            Assert.That(async () => await controller.CreateAddress(viewModel), Throws.Nothing);
        }

        [Test]
        public void CreateAddress_SaveChangesAsyncThrowing_HandlesException()
        {
            AddressViewModel viewModel = new AddressViewModel();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.Add(It.IsAny<MemberAddress>())).
                Returns<MemberAddress>(ma => ma);

            Mock<DbSet<Country>> countryDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Country>().AsQueryable());
            countryDbSetStub.SetupForInclude();

            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);
            dbStub.
                Setup(db => db.Countries).
                Returns(countryDbSetStub.Object);

            dbStub.
                Setup(db => db.SaveChangesAsync()).
                ThrowsAsync(new DbUpdateException());

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object);
            controller.ControllerContext = contextStub.Object;

            Assert.That(async () => await controller.CreateAddress(viewModel), Throws.Nothing);
        }

        [Test]
        public async void CreateAddress_SaveChangesAsyncThrowing_SetsUpViewModel()
        {
            AddressViewModel viewModel = new AddressViewModel();

            List<Country> countries = GetCountries();
            List<MemberAddress> addresses = GetMemberAddresses();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(addresses.AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.Add(It.IsAny<MemberAddress>())).
                Returns<MemberAddress>(ma => ma);

            Mock<DbSet<Country>> countryDbSetStub = TestHelpers.GetFakeAsyncDbSet(countries.AsQueryable());
            countryDbSetStub.SetupForInclude();

            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);
            dbStub.
                Setup(db => db.Countries).
                Returns(countryDbSetStub.Object);

            dbStub.
                Setup(db => db.SaveChangesAsync()).
                ThrowsAsync(new DbUpdateException());

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object);
            controller.ControllerContext = contextStub.Object;

            var result = await controller.CreateAddress(viewModel) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.EqualTo(viewModel));
            Assert.That(result.Model, Is.InstanceOf<AddressViewModel>());
            Assert.That(viewModel.Countries, Is.Not.Empty);
            Assert.That(viewModel.Countries, Has.Count.EqualTo(countries.Count));
            Assert.That(viewModel.Addresses, Is.Not.Empty);
            Assert.That(viewModel.Addresses.Count(), Is.EqualTo(addresses.Count));
        }

        [Test]
        public async void CreateAddress_SaveChangesAsyncThrowing_RedisplaysViewWithSameViewModel()
        {
            AddressViewModel viewModel = new AddressViewModel();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.Add(It.IsAny<MemberAddress>())).
                Returns<MemberAddress>(ma => ma);

            Mock<DbSet<Country>> countryDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Country>().AsQueryable());
            countryDbSetStub.SetupForInclude();

            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);
            dbStub.
                Setup(db => db.Countries).
                Returns(countryDbSetStub.Object);

            dbStub.
                Setup(db => db.SaveChangesAsync()).
                ThrowsAsync(new DbUpdateException());

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object);
            controller.ControllerContext = contextStub.Object;

            var result = await controller.CreateAddress(viewModel) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.EqualTo(viewModel));
        }

        [Test]
        public async void CreateAddress_SuccessfulCreate_RedirectsToManageAddress()
        {
            AddressViewModel viewModel = new AddressViewModel();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.Add(It.IsAny<MemberAddress>())).
                Returns<MemberAddress>(ma => ma);

            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);
            dbStub.
                Setup(db => db.SaveChangesAsync()).
                ReturnsAsync(1);

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object);
            controller.ControllerContext = contextStub.Object;

            var result = await controller.CreateAddress(viewModel) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(ManageController.ManageAddresses)));
        }

        [Test]
        public void EditAddressGET_IdNotInDb_Throws404Exception()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.FindAsync(It.IsAny<Guid>())).
                ReturnsAsync(null);
            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object);

            Assert.That(async () => await controller.EditAddress(addressId), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public void EditAddressGET_NullId_Throws404Exception()
        {
            ManageController controller = CreateManageController();

            Assert.That(async () => await controller.EditAddress(null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void EditAddressGET_IdInDb_SetsUpViewModelWithCountries()
        {
            MemberAddress addressToEdit = new MemberAddress { Address = new Address() };
            List<Country> countries = GetCountries();

            Mock<IVeilDataAccess> dbStub = SetupVeilDataAccessFakeWithCountries(countries);
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.FindAsync(It.IsAny<Guid>())).
                ReturnsAsync(addressToEdit);

            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object);

            var result = await controller.EditAddress(addressId) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<AddressViewModel>());

            var model = (AddressViewModel) result.Model;

            Assert.That(model.Countries, Is.Not.Empty);
            Assert.That(model.Countries, Has.Count.EqualTo(countries.Count));
        }

        [Test]
        public async void EditAddressGET_IdInDb_MapsMatchingAddressToViewModel()
        {
            MemberAddress addressToEdit = new MemberAddress
            {
                Address = new Address
                {
                    StreetAddress = "445 Wes Graham Way",
                    City = "Waterloo",
                    POBoxNumber = "123",
                    PostalCode = "N2L 6R2"
                },
                Id = addressId,
                CountryCode = "CA",
                ProvinceCode = "ON",
            };

            Mock<IVeilDataAccess> dbStub = SetupVeilDataAccessFakeWithCountries();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.FindAsync(It.IsAny<Guid>())).
                ReturnsAsync(addressToEdit);
            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object);

            var result = await controller.EditAddress(addressId) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<AddressViewModel>());

            var model = (AddressViewModel) result.Model;

            Assert.That(model.StreetAddress, Is.EqualTo(addressToEdit.Address.StreetAddress));
            Assert.That(model.City, Is.EqualTo(addressToEdit.Address.City));
            Assert.That(model.POBoxNumber, Is.EqualTo(addressToEdit.Address.POBoxNumber));
            Assert.That(model.PostalCode, Is.EqualTo(addressToEdit.Address.PostalCode));
            Assert.That(model.Id, Is.EqualTo(addressToEdit.Id));
            Assert.That(model.CountryCode, Is.EqualTo(addressToEdit.CountryCode));
            Assert.That(model.ProvinceCode, Is.EqualTo(addressToEdit.ProvinceCode));
        }

        [Test]
        public async void EditAddress_InvalidModelState_RedisplaysViewWithSameViewModel()
        {
            AddressViewModel viewModel = new AddressViewModel();
            Mock<IVeilDataAccess> dbStub = SetupVeilDataAccessFakeWithCountries();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object);
            controller.ModelState.AddModelError(nameof(AddressViewModel.City), "Required");

            var result = await controller.EditAddress(addressId, viewModel) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.EqualTo(viewModel));
        }

        [TestCase("CA")]
        [TestCase("US")]
        public async void EditAddress_InvalidPostalCodeModelStateWithCountryCodeSupplied_ReplacesErrorMessage(string countryCode)
        {
            AddressViewModel viewModel = new AddressViewModel
            {
                CountryCode = countryCode
            };

            string postalCodeErrorMessage = "Required";
            Mock<IVeilDataAccess> dbStub = SetupVeilDataAccessFakeWithCountries();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object);
            controller.ModelState.AddModelError(nameof(AddressViewModel.PostalCode), postalCodeErrorMessage);

            await controller.EditAddress(addressId, viewModel);

            Assert.That(controller.ModelState[nameof(AddressViewModel.PostalCode)].Errors, Has.None.Matches<ModelError>(modelError => modelError.ErrorMessage == postalCodeErrorMessage));
        }

        [Test]
        public async void EditAddress_InvalidPostalCodeModelStateWithoutCountryCodeSupplied_LeavesErrorMessage()
        {
            AddressViewModel viewModel = new AddressViewModel();
            string postalCodeErrorMessage = "Required";
            Mock<IVeilDataAccess> dbStub = SetupVeilDataAccessFakeWithCountries();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object);
            controller.ModelState.AddModelError(nameof(AddressViewModel.PostalCode), postalCodeErrorMessage);

            await controller.EditAddress(addressId, viewModel);

            Assert.That(controller.ModelState[nameof(AddressViewModel.PostalCode)].Errors, Has.Some.Matches<ModelError>(modelError => modelError.ErrorMessage == postalCodeErrorMessage));
        }

        [Test]
        public async void EditAddress_InvalidModelState_SetsUpViewModelWithCountries()
        {
            AddressViewModel viewModel = new AddressViewModel();
            List<Country> countries = GetCountries();
            Mock<IVeilDataAccess> dbStub = SetupVeilDataAccessFakeWithCountries(countries);

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object);
            controller.ModelState.AddModelError(nameof(AddressViewModel.City), "Required");

            var result = await controller.EditAddress(addressId, viewModel) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.EqualTo(viewModel));
            Assert.That(result.Model, Is.InstanceOf<AddressViewModel>());
            Assert.That(viewModel.Countries, Is.Not.Empty);
            Assert.That(viewModel.Countries, Has.Count.EqualTo(countries.Count));
        }

        [Test]
        public void EditAddress_IdNotInDb_Throws404Exception()
        {
            AddressViewModel viewModel = new AddressViewModel();
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.FindAsync(It.IsAny<Guid>())).
                ReturnsAsync(null);
            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object);

            Assert.That(async () => await controller.EditAddress(addressId, viewModel), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void EditAddress_ValidModel_MapsViewModelToNewModel()
        {
            MemberAddress addressToEdit = new MemberAddress { Address = new Address() };
            AddressViewModel viewModel = new AddressViewModel
            {
                City = "Waterloo",
                CountryCode = "CA",
                ProvinceCode = "ON",
                PostalCode = "N2L 6R2",
                StreetAddress = "445 Wes Graham Way",
                POBoxNumber = "123"
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.FindAsync(It.IsAny<Guid>())).
                ReturnsAsync(addressToEdit);
            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object);

            await controller.EditAddress(addressId, viewModel);

            Assert.That(addressToEdit != null);
            Assert.That(addressToEdit.Address.City, Is.EqualTo(viewModel.City));
            Assert.That(addressToEdit.CountryCode, Is.EqualTo(viewModel.CountryCode));
            Assert.That(addressToEdit.ProvinceCode, Is.EqualTo(viewModel.ProvinceCode));
            Assert.That(addressToEdit.Address.PostalCode, Is.EqualTo(viewModel.PostalCode));
            Assert.That(addressToEdit.Address.StreetAddress, Is.EqualTo(viewModel.StreetAddress));
            Assert.That(addressToEdit.Address.POBoxNumber, Is.EqualTo(viewModel.POBoxNumber));
        }

        [TestCase("N2L-6R2", "CA")]
        [TestCase("n2l-6r2", "CA")]
        [TestCase("N2L6R2", "CA")]
        [TestCase("n2l6r2", "CA")]
        [TestCase("12345 6789", "US")]
        public async void EditAddress_ValidModel_ReformatsPostalCodeToMatchStoredPostalCodeRegex(string postalCode, string countryCode)
        {
            MemberAddress addressToEdit = new MemberAddress { Address = new Address() };
            AddressViewModel viewModel = new AddressViewModel
            {
                CountryCode = countryCode,
                PostalCode = postalCode,
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.FindAsync(It.IsAny<Guid>())).
                ReturnsAsync(addressToEdit);
            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object);

            await controller.EditAddress(addressId, viewModel);

            Assert.That(addressToEdit.Address.PostalCode, Is.StringMatching(ValidationRegex.STORED_POSTAL_CODE));
        }

        [Test]
        public async void EditAddress_ValidModel_CallsSaveChangesAsync()
        {
            MemberAddress addressToEdit = new MemberAddress { Address = new Address() };
            AddressViewModel viewModel = new AddressViewModel();
            Mock<IVeilDataAccess> dbMock = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.FindAsync(It.IsAny<Guid>())).
                ReturnsAsync(addressToEdit);
            dbMock.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);
            dbMock.
                Setup(db => db.SaveChangesAsync()).
                ReturnsAsync(1).
                Verifiable();

            ManageController controller = CreateManageController(veilDataAccess: dbMock.Object);

            await controller.EditAddress(addressId, viewModel);

            Assert.That(
                () =>
                    dbMock.Verify(db => db.SaveChangesAsync(),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public void EditAddress_SaveChangesAsyncThrowingProvinceForeignKeyViolationException_HandlesException()
        {
            MemberAddress addressToEdit = new MemberAddress();
            AddressViewModel viewModel = new AddressViewModel();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.FindAsync(It.IsAny<Guid>())).
                ReturnsAsync(addressToEdit);

            Mock<DbSet<Country>> countryDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Country>().AsQueryable());
            countryDbSetStub.SetupForInclude();

            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);
            dbStub.
                Setup(db => db.Countries).
                Returns(countryDbSetStub.Object);

            DbUpdateException provinceConstraintException = new DbUpdateException("See inner",
                SqlExceptionCreator.Create( // This message was copied verbatim from the actual exception being thrown
                    "The INSERT statement conflicted with the FOREIGN KEY constraint " +
                    "\"FK_dbo.MemberAddress_dbo.Province_ProvinceCode_CountryCode\". " +
                    "The conflict occurred in database \"prog3050\", table " +
                    "\"dbo.Province\".\r\nThe statement has been terminated.",
                    (int)SqlErrorNumbers.ConstraintViolation));

            dbStub.
                Setup(db => db.SaveChangesAsync()).
                ThrowsAsync(provinceConstraintException);

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object);

            Assert.That(async () => await controller.EditAddress(addressId, viewModel), Throws.Nothing);
        }

        [Test]
        public void EditAddress_SaveChangesAsyncThrowing_HandlesException()
        {
            MemberAddress addressToEdit = new MemberAddress();
            AddressViewModel viewModel = new AddressViewModel();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.FindAsync(It.IsAny<Guid>())).
                ReturnsAsync(addressToEdit);

            Mock<DbSet<Country>> countryDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Country>().AsQueryable());
            countryDbSetStub.SetupForInclude();

            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);
            dbStub.
                Setup(db => db.Countries).
                Returns(countryDbSetStub.Object);
            dbStub.
                Setup(db => db.SaveChangesAsync()).
                ThrowsAsync(new DbUpdateException());

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object);

            Assert.That(async () => await controller.EditAddress(addressId, viewModel), Throws.Nothing);
        }

        [Test]
        public async void EditAddress_SaveChangesAsyncThrowing_SetsUpViewModel()
        {
            MemberAddress addressToEdit = new MemberAddress();
            AddressViewModel viewModel = new AddressViewModel();
            List<Country> countries = GetCountries();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.FindAsync(It.IsAny<Guid>())).
                ReturnsAsync(addressToEdit);

            Mock<DbSet<Country>> countryDbSetStub = TestHelpers.GetFakeAsyncDbSet(countries.AsQueryable());
            countryDbSetStub.SetupForInclude();

            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);
            dbStub.
                Setup(db => db.Countries).
                Returns(countryDbSetStub.Object);
            dbStub.
                Setup(db => db.SaveChangesAsync()).
                ThrowsAsync(new DbUpdateException());

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object);

            var result = await controller.EditAddress(addressId, viewModel) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.EqualTo(viewModel));
            Assert.That(result.Model, Is.InstanceOf<AddressViewModel>());
            Assert.That(viewModel.Countries, Is.Not.Empty);
            Assert.That(viewModel.Countries, Has.Count.EqualTo(countries.Count));
        }

        [Test]
        public async void EditAddress_SaveChangesAsyncThrowing_RedisplaysViewWithSameViewModel()
        {
            MemberAddress addressToEdit = new MemberAddress();
            AddressViewModel viewModel = new AddressViewModel();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.FindAsync(It.IsAny<Guid>())).
                ReturnsAsync(addressToEdit);

            Mock<DbSet<Country>> countryDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Country>().AsQueryable());
            countryDbSetStub.SetupForInclude();

            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);
            dbStub.
                Setup(db => db.Countries).
                Returns(countryDbSetStub.Object);

            dbStub.
                Setup(db => db.SaveChangesAsync()).
                ThrowsAsync(new DbUpdateException());

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object);

            var result = await controller.EditAddress(addressId, viewModel) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.EqualTo(viewModel));
        }

        [Test]
        public async void EditAddress_SuccessfulSave_RedirectsToManageAddress()
        {
            MemberAddress addressToEdit = new MemberAddress();
            AddressViewModel viewModel = new AddressViewModel();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.FindAsync(It.IsAny<Guid>())).
                ReturnsAsync(addressToEdit);

            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);
            dbStub.
                Setup(db => db.SaveChangesAsync()).
                ReturnsAsync(1);

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object);

            var result = await controller.EditAddress(addressId, viewModel) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(ManageController.ManageAddresses)));
        }

        [Test]
        public async void EditAddress_ValidModel_CallsFindAsyncWithPassedId()
        {
            MemberAddress addressToEdit = new MemberAddress();
            AddressViewModel viewModel = new AddressViewModel();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.FindAsync(It.IsAny<Guid>())).
                ReturnsAsync(addressToEdit).
                Verifiable();

            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object);

            await controller.EditAddress(addressId, viewModel);

            Assert.That(
                () => 
                    addressDbSetStub.Verify(adb => adb.FindAsync(addressId),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void ManageCreditCards_WhenCalled_SetsUpViewModel()
        {
            List<Country> countries = GetCountries();
            List<MemberCreditCard> creditCards = new List<MemberCreditCard>
            {
                new MemberCreditCard
                {
                    CardholderName = "Jane Shepard",
                    ExpiryMonth = 4,
                    ExpiryYear = 2154,
                    Last4Digits = "4242",
                    MemberId = memberId
                },
                new MemberCreditCard
                {
                    CardholderName = "John Shepard",
                    ExpiryMonth = 4,
                    ExpiryYear = 2154,
                    Last4Digits = "1881",
                    MemberId = memberId
                }
            };
            List<Member> members = new List<Member>
            {
                new Member { UserId = memberId, CreditCards = creditCards }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Member>> memberDbStub = TestHelpers.GetFakeAsyncDbSet(members.AsQueryable());

            Mock<DbSet<Country>> countryDbSetStub = TestHelpers.GetFakeAsyncDbSet(countries.AsQueryable());
            countryDbSetStub.SetupForInclude();

            dbStub.
                Setup(db => db.Members).
                Returns(memberDbStub.Object);
            dbStub.
                Setup(db => db.Countries).
                Returns(countryDbSetStub.Object);

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object);

            controller.ControllerContext = contextStub.Object;

            var result = await controller.ManageCreditCards() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<BillingInfoViewModel>());

            var model = (BillingInfoViewModel)result.Model;

            Assert.That(model.Countries, Is.Not.Empty);
            Assert.That(model.Countries, Has.Count.EqualTo(countries.Count));
            Assert.That(model.CreditCards, Is.Not.Empty);
            Assert.That(model.CreditCards.Count(), Is.EqualTo(creditCards.Count));
            Assert.That(model.Years, Is.Not.Empty);
            Assert.That(model.Years.Count(), Is.EqualTo(20));
            Assert.That(model.Months, Is.Not.Empty);
            Assert.That(model.Months.Count(), Is.EqualTo(12));
            Assert.That(model.Months, Has.Exactly(1).Matches<SelectListItem>(i => i.Text.Contains("January") && i.Text.Contains("01")));
            Assert.That(model.Months, Has.Exactly(1).Matches<SelectListItem>(i => i.Text.Contains("February") && i.Text.Contains("02")));
            Assert.That(model.Months, Has.Exactly(1).Matches<SelectListItem>(i => i.Text.Contains("March") && i.Text.Contains("03")));
            Assert.That(model.Months, Has.Exactly(1).Matches<SelectListItem>(i => i.Text.Contains("April") && i.Text.Contains("04")));
            Assert.That(model.Months, Has.Exactly(1).Matches<SelectListItem>(i => i.Text.Contains("May") && i.Text.Contains("05")));
            Assert.That(model.Months, Has.Exactly(1).Matches<SelectListItem>(i => i.Text.Contains("June") && i.Text.Contains("06")));
            Assert.That(model.Months, Has.Exactly(1).Matches<SelectListItem>(i => i.Text.Contains("July") && i.Text.Contains("07")));
            Assert.That(model.Months, Has.Exactly(1).Matches<SelectListItem>(i => i.Text.Contains("August") && i.Text.Contains("08")));
            Assert.That(model.Months, Has.Exactly(1).Matches<SelectListItem>(i => i.Text.Contains("September") && i.Text.Contains("09")));
            Assert.That(model.Months, Has.Exactly(1).Matches<SelectListItem>(i => i.Text.Contains("October") && i.Text.Contains("10")));
            Assert.That(model.Months, Has.Exactly(1).Matches<SelectListItem>(i => i.Text.Contains("November") && i.Text.Contains("11")));
            Assert.That(model.Months, Has.Exactly(1).Matches<SelectListItem>(i => i.Text.Contains("December") && i.Text.Contains("12")));
        }

        [Test]
        public async void ManageCreditCards_WhenCalled_IncludesProvinces()
        {
            List<Country> countries = GetCountries();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member>().AsQueryable());

            Mock<DbSet<Country>> countryDbSetMock = TestHelpers.GetFakeAsyncDbSet(countries.AsQueryable());
            countryDbSetMock.
                Setup(cdb => cdb.Include(It.IsAny<string>())).
                Returns(countryDbSetMock.Object).
                Verifiable();

            dbStub.
                Setup(db => db.Members).
                Returns(memberDbSetStub.Object);
            dbStub.
                Setup(db => db.Countries).
                Returns(countryDbSetMock.Object);

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object);
            controller.ControllerContext = contextStub.Object;

            await controller.ManageCreditCards();

            Assert.That(
                () =>
                    countryDbSetMock.Verify<DbQuery<Country>>(mdb => mdb.Include(It.Is<string>(val => val.Contains(nameof(IVeilDataAccess.Provinces)))),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public async void CreateCreditCard_InvalidStripeToken_RedirectsToManageCreditCards(string token)
        {
            ManageController controller = CreateManageController();

            var result = await controller.CreateCreditCard(token) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(controller.ManageCreditCards)));
        }

        [Test]
        public async void CreateCreditCard_ValidModel_RetrievesMemberMatchingCurrentUserId()
        {
            Member member = new Member
            {
                UserId = memberId,
                CreditCards = new List<MemberCreditCard>()
            };

            string stripeCardToken = "stripeCardToken";

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Member>> memberDbSetMock = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetMock.
                Setup(mdb => mdb.FindAsync(memberId)).
                ReturnsAsync(member).
                Verifiable();

            dbStub.
                Setup(db => db.Members).
                Returns(memberDbSetMock.Object);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.CreateCreditCard(It.IsAny<Member>(), It.IsAny<string>())).
                Returns<MemberCreditCard>(null);

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object, stripeService: stripeServiceStub.Object);
            controller.ControllerContext = contextStub.Object;

            await controller.CreateCreditCard(stripeCardToken);

            Assert.That(
                () => 
                    memberDbSetMock.Verify(mdb => mdb.FindAsync(memberId),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void CreateCreditCard_MemberNotInDb_ReturnsInternalServerError()
        {
            string stripeCardToken = "stripeCardToken";

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member>().AsQueryable());
            memberDbSetStub.
                Setup(mdb => mdb.FindAsync(memberId)).
                ReturnsAsync(null);

            dbStub.
                Setup(db => db.Members).
                Returns(memberDbSetStub.Object);

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object);
            controller.ControllerContext = contextStub.Object;

            var result = await controller.CreateCreditCard(stripeCardToken) as HttpStatusCodeResult;

            Assert.That(result != null);
            Assert.That(result.StatusCode, Is.EqualTo((int)HttpStatusCode.InternalServerError));
        }

        [Test]
        public async void CreateCreditCard_MemberInDb_CallsIStripeServiceCreateCreditCardWithMember()
        {
            Member member = new Member
            {
                UserId = memberId,
                CreditCards = new List<MemberCreditCard>()
            };

            string stripeCardToken = "stripeCardToken";

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.
                Setup(mdb => mdb.FindAsync(memberId)).
                ReturnsAsync(member);

            dbStub.
                Setup(db => db.Members).
                Returns(memberDbSetStub.Object);

            Mock<IStripeService> stripeServiceMock = new Mock<IStripeService>();
            stripeServiceMock.
                Setup(s => s.CreateCreditCard(It.IsAny<Member>(), It.IsAny<string>())).
                Returns<MemberCreditCard>(null).
                Verifiable();

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object, stripeService: stripeServiceMock.Object);
            controller.ControllerContext = contextStub.Object;

            await controller.CreateCreditCard(stripeCardToken);

            Assert.That(
                () => 
                    stripeServiceMock.Verify(s => s.CreateCreditCard(member, It.IsAny<string>()),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void CreateCreditCard_MemberInDb_CallsIStripeServiceCreateCreditCardWithPassedToken()
        {
            Member member = new Member
            {
                UserId = memberId,
                CreditCards = new List<MemberCreditCard>()
            };

            string stripeCardToken = "stripeCardToken";

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.
                Setup(mdb => mdb.FindAsync(memberId)).
                ReturnsAsync(member);

            dbStub.
                Setup(db => db.Members).
                Returns(memberDbSetStub.Object);

            Mock<IStripeService> stripeServiceMock = new Mock<IStripeService>();
            stripeServiceMock.
                Setup(s => s.CreateCreditCard(It.IsAny<Member>(), It.IsAny<string>())).
                Returns<MemberCreditCard>(null).
                Verifiable();

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object, stripeService: stripeServiceMock.Object);
            controller.ControllerContext = contextStub.Object;

            await controller.CreateCreditCard(stripeCardToken);

            Assert.That(
                () =>
                    stripeServiceMock.Verify(s => s.CreateCreditCard(It.IsAny<Member>(), stripeCardToken),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public void CreateCreditCard_IStripeServiceThrowsStripeException_HandlesException()
        {
            Member member = new Member
            {
                UserId = memberId
            };

            string stripeCardToken = "stripeCardToken";

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.
                Setup(mdb => mdb.FindAsync(memberId)).
                ReturnsAsync(member);

            dbStub.
                Setup(db => db.Members).
                Returns(memberDbSetStub.Object);

            StripeException exception = new StripeException(
                HttpStatusCode.BadRequest, 
                new StripeError
                {
                    Code = "Any"
                },
                "message");

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.CreateCreditCard(It.IsAny<Member>(), It.IsAny<string>())).
                Throws(exception);

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object, stripeService: stripeServiceStub.Object);
            controller.ControllerContext = contextStub.Object;

            Assert.That(async () => await controller.CreateCreditCard(stripeCardToken), Throws.Nothing);
        }

        [Test]
        public async void CreateCreditCard_IStripeServiceThrowsStripeException_RedirectsToManageCreditCard()
        {
            Member member = new Member
            {
                UserId = memberId
            };

            string stripeCardToken = "stripeCardToken";

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.
                Setup(mdb => mdb.FindAsync(memberId)).
                ReturnsAsync(member);

            dbStub.
                Setup(db => db.Members).
                Returns(memberDbSetStub.Object);

            StripeException exception = new StripeException(
                HttpStatusCode.BadRequest,
                new StripeError
                {
                    Code = "Any"
                },
                "message");

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.CreateCreditCard(It.IsAny<Member>(), It.IsAny<string>())).
                Throws(exception);

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object, stripeService: stripeServiceStub.Object);
            controller.ControllerContext = contextStub.Object;

            var result = await controller.CreateCreditCard(stripeCardToken) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(controller.ManageCreditCards)));
        }

        [Test]
        public async void CreateCreditCard_IStripeServiceThrowsStripeExceptionCardError_AddsErrorToModelState()
        {
            Member member = new Member
            {
                UserId = memberId
            };

            string stripeCardToken = "stripeCardToken";

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.
                Setup(mdb => mdb.FindAsync(memberId)).
                ReturnsAsync(member);

            dbStub.
                Setup(db => db.Members).
                Returns(memberDbSetStub.Object);

            string stripeErrorMessage = "A card Error Message";

            StripeException exception = new StripeException(
                HttpStatusCode.BadRequest,
                new StripeError
                {
                    Code = "card_error"
                },
                stripeErrorMessage);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.CreateCreditCard(It.IsAny<Member>(), It.IsAny<string>())).
                Throws(exception);

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object, stripeService: stripeServiceStub.Object);
            controller.ControllerContext = contextStub.Object;

            await controller.CreateCreditCard(stripeCardToken);

            Assert.That(controller.ModelState[ManageController.STRIPE_ISSUES_MODELSTATE_KEY].Errors, Has.Some.Matches<ModelError>(modelError => modelError.ErrorMessage == stripeErrorMessage));
        }

        [Test]
        public async void CreateCreditCard_StripeServiceSuccess_AddsReturnedCreditCardToMembersCreditCards()
        {
            Member member = new Member
            {
                UserId = memberId,
                CreditCards = new List<MemberCreditCard>()
            };

            MemberCreditCard creditCard = new MemberCreditCard
            {
                Id = new Guid("F406AB6C-CC58-4370-AB49-89D622C51768")
            };

            string stripeCardToken = "stripeCardToken";

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.
                Setup(mdb => mdb.FindAsync(memberId)).
                ReturnsAsync(member);

            dbStub.
                Setup(db => db.Members).
                Returns(memberDbSetStub.Object);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.CreateCreditCard(It.IsAny<Member>(), It.IsAny<string>())).
                Returns(creditCard);

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object, stripeService: stripeServiceStub.Object);
            controller.ControllerContext = contextStub.Object;

            await controller.CreateCreditCard(stripeCardToken);

            Assert.That(member.CreditCards, Is.Not.Empty);
            Assert.That(member.CreditCards, Has.Member(creditCard));
        }

        [Test]
        public async void CreateCreditCard_StripeServiceSuccess_CallsSaveChangesAsync()
        {
            Member member = new Member
            {
                UserId = memberId,
                CreditCards = new List<MemberCreditCard>()
            };

            MemberCreditCard creditCard = new MemberCreditCard();

            string stripeCardToken = "stripeCardToken";

            Mock<IVeilDataAccess> dbMock = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.
                Setup(mdb => mdb.FindAsync(memberId)).
                ReturnsAsync(member);

            dbMock.
                Setup(db => db.Members).
                Returns(memberDbSetStub.Object);
            dbMock.
                Setup(db => db.SaveChangesAsync()).
                ReturnsAsync(1).
                Verifiable();

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.CreateCreditCard(It.IsAny<Member>(), It.IsAny<string>())).
                Returns(creditCard);

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbMock.Object, idGetter: idGetterStub.Object, stripeService: stripeServiceStub.Object);
            controller.ControllerContext = contextStub.Object;

            await controller.CreateCreditCard(stripeCardToken);

            Assert.That(
                () => 
                    dbMock.Verify(db => db.SaveChangesAsync(),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void CreateCreditCard_SuccessfulCreate_RedirectsToManageCreditCards()
        {
            Member member = new Member
            {
                UserId = memberId,
                CreditCards = new List<MemberCreditCard>()
            };

            MemberCreditCard creditCard = new MemberCreditCard();

            string stripeCardToken = "stripeCardToken";

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.
                Setup(mdb => mdb.FindAsync(memberId)).
                ReturnsAsync(member);

            dbStub.
                Setup(db => db.Members).
                Returns(memberDbSetStub.Object);
            dbStub.
                Setup(db => db.SaveChangesAsync()).
                ReturnsAsync(1);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.CreateCreditCard(It.IsAny<Member>(), It.IsAny<string>())).
                Returns(creditCard);

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object, stripeService: stripeServiceStub.Object);
            controller.ControllerContext = contextStub.Object;

            var result = await controller.CreateCreditCard(stripeCardToken) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(controller.ManageCreditCards)));
        }

        [Test]
        public void UpdateUserInformation_NoEmailChange()
        {      
            IndexViewModel viewModel = new IndexViewModel()
            {
                MemberEmail = "person@email.com",
                MemberFirstName = "firstName",
                MemberLastName = "lastName",
                MemberVisibility = WishListVisibility.FriendsOnly,
                ReceivePromotionalEmals = true
            };

            Member member = new Member()
            {
                UserId = memberId,
                WishListVisibility = WishListVisibility.FriendsOnly,
                ReceivePromotionalEmails = true
            };

            User user = new User()
            {
                Id = memberId,
                Email = "person@email.com",
                Member = member
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();   
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);

            userManagerStub.Setup(um => um.FindByIdAsync(memberId)).ReturnsAsync(user);
            dbStub.Setup(db => db.UserStore).Returns(userStoreStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(memberId);

            ManageController controller = new ManageController(userManagerStub.Object, null, dbStub.Object, idGetterStub.Object, null)
            {
                ControllerContext = context.Object
            };
        }

        [Test]
        public async void ManagePlatforms_ReturnsMatchingModel()
        {
            Member member = new Member
            {
                UserId = memberId,
                FavoritePlatforms = new List<Platform>
                {
                    new Platform
                    {
                        PlatformCode = "TPlat",
                        PlatformName = "Test Platform"
                    }
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            ManageController controller = new ManageController(userManager: null, signInManager: null,
                veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object, stripeService: null)
            {
                ControllerContext = context.Object
            };

            var result = await controller.ManagePlatforms() as ViewResult;

            Assert.That(result != null);

            var model = (List<Platform>)result.Model;

            Assert.That(model.Count, Is.EqualTo(1));
            Assert.That(model[0].PlatformCode, Is.EqualTo("TPlat"));
        }

        [Test]
        public async void ManagePlatformsPOST_AddingPlatforms_ReturnsUpdatedModel()
        {
            List<Platform> platforms = new List<Platform>
            {
                new Platform
                {
                    PlatformCode = "TPlat",
                    PlatformName = "Test Platform"
                },
                new Platform
                {
                    PlatformCode = "2Plat",
                    PlatformName = "Second Platform"
                }
            };

            List<string> platformStrings = new List<string>
            {
                "TPlat",
                "2Plat"
            };

            Member member = new Member
            {
                UserId = memberId,
                FavoritePlatforms = new List<Platform>
                {
                    platforms[0]
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(platforms.AsQueryable());
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            ManageController controller = new ManageController(userManager: null, signInManager: null,
                veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object, stripeService: null)
            {
                ControllerContext = context.Object
            };

            await controller.ManagePlatforms(platformStrings);

            Assert.That(member.FavoritePlatforms.Count, Is.EqualTo(2));
            Assert.That(member.FavoritePlatforms.Any(p => p.PlatformCode == "TPlat"));
            Assert.That(member.FavoritePlatforms.Any(p => p.PlatformCode == "2Plat"));
        }

        [Test]
        [Ignore]
        public async void ManagePlatformsPOST_RemovePlatforms_ReturnsUpdatedModel()
        {
            // TODO
        }

        [Test]
        public async void ManagePlatformsPOST_NullClearsPlatforms_ReturnsUpdatedModel()
        {
            List<Platform> platforms = new List<Platform>
            {
                new Platform
                {
                    PlatformCode = "TPlat",
                    PlatformName = "Test Platform"
                },
                new Platform
                {
                    PlatformCode = "2Plat",
                    PlatformName = "Second Platform"
                }
            };

            Member member = new Member
            {
                UserId = memberId,
                FavoritePlatforms = platforms
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(platforms.AsQueryable());
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            ManageController controller = new ManageController(userManager: null, signInManager: null,
                veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object, stripeService: null)
            {
                ControllerContext = context.Object
            };

            await controller.ManagePlatforms(null);

            Assert.That(member.FavoritePlatforms.Count, Is.EqualTo(0));
        }

        [Test]
        public async void ManageTags_ReturnsMatchingModel()
        {
            Member member = new Member
            {
                UserId = memberId,
                FavoriteTags = new List<Tag>
                {
                    new Tag
                    {
                        Name = "TestTag"
                    }
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            ManageController controller = new ManageController(userManager: null, signInManager: null,
                veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object, stripeService: null)
            {
                ControllerContext = context.Object
            };

            var result = await controller.ManageTags() as ViewResult;

            Assert.That(result != null);

            var model = (List<Tag>)result.Model;

            Assert.That(model.Count, Is.EqualTo(1));
            Assert.That(model[0].Name, Is.EqualTo("TestTag"));
        }

        [Test]
        public async void ManageTagsPOST_AddingTags_ReturnsUpdatedModel()
        {
            List<Tag> tags = new List<Tag>
            {
                new Tag
                {
                    Name = "Test Tag"
                },
                new Tag
                {
                    Name = "Second Tag"
                }
            };

            List<string> tagStrings = new List<string>
            {
                "Test Tag",
                "Second Tag"
            };

            Member member = new Member
            {
                UserId = memberId,
                FavoriteTags = new List<Tag>
                {
                    tags[0]
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Tag>> tagDbSetStub = TestHelpers.GetFakeAsyncDbSet(tags.AsQueryable());
            dbStub.Setup(db => db.Tags).Returns(tagDbSetStub.Object);

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            ManageController controller = new ManageController(userManager: null, signInManager: null,
                veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object, stripeService: null)
            {
                ControllerContext = context.Object
            };

            await controller.ManageTags(tagStrings);

            Assert.That(member.FavoriteTags.Count, Is.EqualTo(2));
            Assert.That(member.FavoriteTags.Any(t => t.Name == "Test Tag"));
            Assert.That(member.FavoriteTags.Any(t => t.Name == "Second Tag"));
        }

        [Test]
        [Ignore]
        public async void ManageTagsPOST_RemoveTags_ReturnsUpdatedModel()
        {
            // TODO
        }

        [Test]
        public async void ManageTagsPOST_NullClearsTags_ReturnsUpdatedModel()
        {
            List<Tag> tags = new List<Tag>
            {
                new Tag
                {
                    Name = "Test Tag"
                },
                new Tag
                {
                    Name = "Second Tag"
                }
            };

            Member member = new Member
            {
                UserId = memberId,
                FavoriteTags = new List<Tag>
                {
                    tags[0]
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Tag>> tagDbSetStub = TestHelpers.GetFakeAsyncDbSet(tags.AsQueryable());
            dbStub.Setup(db => db.Tags).Returns(tagDbSetStub.Object);

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            ManageController controller = new ManageController(userManager: null, signInManager: null,
                veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object, stripeService: null)
            {
                ControllerContext = context.Object
            };

            await controller.ManageTags(null);

            Assert.That(member.FavoriteTags.Count, Is.EqualTo(0));
        }
    }
}
