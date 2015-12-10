/* CreateAddressTests.cs
 *      Drew Matheson, 2015.11.10: Created
 */

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

namespace Veil.Tests.Controllers.ManageControllerTests
{
    public class CreateAddressTests : ManageControllerTestsBase
    {
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
    }
}
