/* EditAddressTests.cs
 *      Drew Matheson, 2015.11.14: Created
 */

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.DataModels.Validation;
using Veil.Models;

namespace Veil.Tests.Controllers.ManageControllerTests
{
    public class EditAddressTests : ManageControllerTestsBase
    {
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

            var model = (AddressViewModel)result.Model;

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

            var model = (AddressViewModel)result.Model;

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
    }
}
