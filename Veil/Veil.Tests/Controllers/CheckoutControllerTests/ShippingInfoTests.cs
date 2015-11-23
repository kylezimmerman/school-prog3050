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
    public class ShippingInfoTests : CheckoutControllerTestsBase
    {
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
    }
}
