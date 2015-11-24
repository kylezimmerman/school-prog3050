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
    public class ExistingShippingInfoTests : CheckoutControllerTestsBase
    {
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
    }
}
