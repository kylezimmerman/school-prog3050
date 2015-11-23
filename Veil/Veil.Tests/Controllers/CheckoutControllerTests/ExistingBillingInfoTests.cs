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
    public class ExistingBillingInfoTests : CheckoutControllerTestsBase
    {
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
    }
}
