using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Exceptions;
using Veil.Helpers;
using Veil.Models;
using Veil.Services.Interfaces;

namespace Veil.Tests.Controllers.CheckoutControllerTests
{
    public class NewBillingInfoTests : CheckoutControllerTestsBase
    {
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

            StripeServiceException exception = new StripeServiceException("message", StripeExceptionType.UnknownError);

            Mock<IStripeService> stripeServiceMock = new Mock<IStripeService>();
            stripeServiceMock.
                Setup(s => s.CreateCreditCard(It.IsAny<Member>(), It.IsAny<string>())).
                Throws(exception). // Throw exception to end test early as we have the knowledge we need
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
        public async void NewBillingInfo_StripeExceptionCardError_AddsCardErrorMessageToAlertMessages()
        {
            Member currentMember = member;
            string cardToken = "cardToken";
            string stripeErrorMessage = "A card error message";

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithMember(dbStub, currentMember);
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingDetails);

            StripeServiceException exception = new StripeServiceException(stripeErrorMessage, StripeExceptionType.CardError);

            Mock<IStripeService> stripeServiceMock = new Mock<IStripeService>();
            stripeServiceMock.
                Setup(s => s.CreateCreditCard(It.IsAny<Member>(), It.IsAny<string>())).
                Throws(exception);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object, stripeService: stripeServiceMock.Object);

            await controller.NewBillingInfo(cardToken, saveCard: true);

            Assert.That(controller.TempData[AlertHelper.ALERT_MESSAGE_KEY], Has.Some.Matches<AlertMessage>(am => am.Message == stripeErrorMessage));
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

            StripeServiceException exception = new StripeServiceException(stripeErrorMessage, StripeExceptionType.CardError);

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
        public async void NewBillingInfo_StripeExceptionApiKeyError_ReturnsInternalServerErrorCode()
        {
            Member currentMember = member;
            string cardToken = "cardToken";
            string stripeErrorMessage = "A card error message";

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            SetupVeilDataAccessWithCarts(dbStub, GetCartsListContainingCartWithNewAndUsed());
            SetupVeilDataAccessWithMember(dbStub, currentMember);
            Mock<ControllerContext> contextStub = GetControllerContextWithSessionSetupToReturn(validNotSavedShippingDetails);

            StripeServiceException exception = new StripeServiceException(stripeErrorMessage, StripeExceptionType.ApiKeyError);

            Mock<IStripeService> stripeServiceMock = new Mock<IStripeService>();
            stripeServiceMock.
                Setup(s => s.CreateCreditCard(It.IsAny<Member>(), It.IsAny<string>())).
                Throws(exception);

            CheckoutController controller = CreateCheckoutController(dbStub.Object, context: contextStub.Object, stripeService: stripeServiceMock.Object);

            var result = await controller.NewBillingInfo(cardToken, saveCard: true) as HttpStatusCodeResult;

            Assert.That(result != null);
            Assert.That(result.StatusCode, Is.GreaterThanOrEqualTo((int)HttpStatusCode.InternalServerError));
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
    }
}
