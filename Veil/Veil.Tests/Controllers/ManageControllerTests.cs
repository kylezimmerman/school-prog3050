using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.DataModels.Validation;
using Veil.Helpers;
using Veil.Models;

namespace Veil.Tests.Controllers
{
    [TestFixture]
    public class ManageControllerTests
    {
        private Guid memberId;

        [SetUp]
        public void Setup()
        {
            memberId = new Guid("59EF92BE-D71F-49ED-992D-DF15773DAF98");
        }

        private List<MemberAddress> GetMemberAddresses()
        {
            return new List<MemberAddress>
            {
                new MemberAddress
                {
                    City = "A city",
                    CountryCode = "CA",
                    MemberId = memberId
                },
                new MemberAddress
                {
                    City = "Waterloo",
                    CountryCode = "CA",
                    ProvinceCode = "ON",
                    PostalCode = "N2L 6R2",
                    StreetAddress = "445 Wes Graham Way",
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
            countries = countries ?? new List<Country>();
            addresses = addresses ?? new List<MemberAddress>();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(addresses.AsQueryable());

            Mock<DbSet<Country>> countryDbSetStub = TestHelpers.GetFakeAsyncDbSet(countries.AsQueryable());
            countryDbSetStub.SetupForInclude();

            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);
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

            ManageController controller = new ManageController(userManager: null, signInManager: null, veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.ManageAddresses() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<ManageAddressViewModel>());

            var model = (ManageAddressViewModel) result.Model;

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

            ManageController controller = new ManageController(userManager: null, signInManager: null, veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.ManageAddresses() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<ManageAddressViewModel>());

            var model = (ManageAddressViewModel)result.Model;

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

            ManageController controller = new ManageController(userManager: null, signInManager: null, veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object)
            {
                ControllerContext = contextStub.Object
            };

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
            ManageAddressViewModel viewModel = new ManageAddressViewModel();

            Mock<IVeilDataAccess> dbStub = SetupVeilDataAccessFakeWithCountriesAndAddresses();

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = new ManageController(userManager: null, signInManager: null, veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            controller.ModelState.AddModelError(nameof(ManageAddressViewModel.City), "Required");

            var result = await controller.CreateAddress(viewModel) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.EqualTo(viewModel));
        }

        [TestCase("CA")]
        [TestCase("US")]
        public async void CreateAddress_InvalidPostalCodeModelStateWithCountryCodeSupplied_ReplacesErrorMessage(string countryCode)
        {
            ManageAddressViewModel viewModel = new ManageAddressViewModel
            {
                CountryCode = countryCode
            };

            string postalCodeErrorMessage = "Required";

            Mock<IVeilDataAccess> dbStub = SetupVeilDataAccessFakeWithCountriesAndAddresses();

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = new ManageController(userManager: null, signInManager: null, veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            controller.ModelState.AddModelError(nameof(ManageAddressViewModel.PostalCode), postalCodeErrorMessage);

            await controller.CreateAddress(viewModel);

            Assert.That(controller.ModelState[nameof(ManageAddressViewModel.PostalCode)].Errors, Has.None.Matches<ModelError>(modelError => modelError.ErrorMessage == postalCodeErrorMessage));
        }

        [Test]
        public async void CreateAddress_InvalidPostalCodeModelStateWithoutCountryCodeSupplied_LeavesErrorMessage()
        {
            ManageAddressViewModel viewModel = new ManageAddressViewModel();

            string postalCodeErrorMessage = "Required";

            Mock<IVeilDataAccess> dbStub = SetupVeilDataAccessFakeWithCountriesAndAddresses();

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = new ManageController(userManager: null, signInManager: null, veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            controller.ModelState.AddModelError(nameof(ManageAddressViewModel.PostalCode), postalCodeErrorMessage);

            await controller.CreateAddress(viewModel);

            Assert.That(controller.ModelState[nameof(ManageAddressViewModel.PostalCode)].Errors, Has.Some.Matches<ModelError>(modelError => modelError.ErrorMessage == postalCodeErrorMessage));
        }

        [Test]
        public async void CreateAddress_InvalidModelState_SetsUpViewModelWithCountries()
        {
            ManageAddressViewModel viewModel = new ManageAddressViewModel();

            List<Country> countries = GetCountries();
            List<MemberAddress> addresses = GetMemberAddresses();

            Mock<IVeilDataAccess> dbStub = SetupVeilDataAccessFakeWithCountriesAndAddresses(countries, addresses);

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = new ManageController(userManager: null, signInManager: null, veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            controller.ModelState.AddModelError(nameof(ManageAddressViewModel.City), "Required");

            var result = await controller.CreateAddress(viewModel) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.EqualTo(viewModel));
            Assert.That(result.Model, Is.InstanceOf<ManageAddressViewModel>());
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
            
            ManageAddressViewModel viewModel = new ManageAddressViewModel
            {
                City = "Waterloo",
                CountryCode = "CA",
                ProvinceCode = "ON",
                PostalCode = "N2L 6R2",
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

            ManageController controller = new ManageController(userManager: null, signInManager: null, veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            await controller.CreateAddress(viewModel);

            Assert.That(newAddress != null);
            Assert.That(newAddress.MemberId, Is.EqualTo(currentMemberId));
            Assert.That(newAddress.City, Is.EqualTo(viewModel.City));
            Assert.That(newAddress.CountryCode, Is.EqualTo(viewModel.CountryCode));
            Assert.That(newAddress.ProvinceCode, Is.EqualTo(viewModel.ProvinceCode));
            Assert.That(newAddress.PostalCode, Is.EqualTo(viewModel.PostalCode));
            Assert.That(newAddress.StreetAddress, Is.EqualTo(viewModel.StreetAddress));
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

            ManageAddressViewModel viewModel = new ManageAddressViewModel
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

            ManageController controller = new ManageController(userManager: null, signInManager: null, veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            await controller.CreateAddress(viewModel);

            Assert.That(newAddress != null);
            Assert.That(newAddress.PostalCode, Is.StringMatching(ValidationRegex.STORED_POSTAL_CODE));
        }

        [Test]
        public async void CreateAddress_ValidModel_CallsSaveChangesAsync()
        {
            ManageAddressViewModel viewModel = new ManageAddressViewModel();

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

            ManageController controller = new ManageController(userManager: null, signInManager: null, veilDataAccess: dbMock.Object, idGetter: idGetterStub.Object)
            {
                ControllerContext = contextStub.Object
            };

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
            ManageAddressViewModel viewModel = new ManageAddressViewModel();

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

            ManageController controller = new ManageController(userManager: null, signInManager: null, veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            Assert.That(async () => await controller.CreateAddress(viewModel), Throws.Nothing);
        }

        [Test]
        public void CreateAddress_SaveChangesAsyncThrowing_HandlesException()
        {
            ManageAddressViewModel viewModel = new ManageAddressViewModel();

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

            ManageController controller = new ManageController(userManager: null, signInManager: null, veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            Assert.That(async () => await controller.CreateAddress(viewModel), Throws.Nothing);
        }

        [Test]
        public async void CreateAddress_SaveChangesAsyncThrowing_SetsUpViewModel()
        {
            ManageAddressViewModel viewModel = new ManageAddressViewModel();

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

            ManageController controller = new ManageController(userManager: null, signInManager: null, veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.CreateAddress(viewModel) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.EqualTo(viewModel));
            Assert.That(result.Model, Is.InstanceOf<ManageAddressViewModel>());
            Assert.That(viewModel.Countries, Is.Not.Empty);
            Assert.That(viewModel.Countries, Has.Count.EqualTo(countries.Count));
            Assert.That(viewModel.Addresses, Is.Not.Empty);
            Assert.That(viewModel.Addresses.Count(), Is.EqualTo(addresses.Count));
        }

        [Test]
        public async void CreateAddress_SaveChangesAsyncThrowing_RedisplaysViewWithSameViewModel()
        {
            ManageAddressViewModel viewModel = new ManageAddressViewModel();

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

            ManageController controller = new ManageController(userManager: null, signInManager: null, veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.CreateAddress(viewModel) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.EqualTo(viewModel));
        }

        [Test]
        public async void CreateAddress_SuccessfulCreate_RedirectsToManageAddress()
        {
            ManageAddressViewModel viewModel = new ManageAddressViewModel();

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

            ManageController controller = new ManageController(userManager: null, signInManager: null, veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.CreateAddress(viewModel) as RedirectToRouteResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(ManageController.ManageAddresses)));
        }
    }
}
