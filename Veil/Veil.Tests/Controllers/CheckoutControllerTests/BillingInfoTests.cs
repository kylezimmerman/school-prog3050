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
    public class BillingInfoTests : CheckoutControllerTestsBase
    {
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
    }
}
