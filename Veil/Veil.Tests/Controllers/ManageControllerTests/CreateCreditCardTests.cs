/* CreateCreditCardTests.cs
 *      Drew Matheson, 2015.11.11: Created
 */

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
using Veil.Services.Interfaces;

namespace Veil.Tests.Controllers.ManageControllerTests
{
    public class CreateCreditCardTests : ManageControllerTestsBase
    {
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

            StripeServiceException exception = new StripeServiceException("message", StripeExceptionType.UnknownError);

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
        public async void CreateCreditCard_IStripeServiceThrowsApiKeyException_ReturnsInternalServerError()
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

            StripeServiceException exception = new StripeServiceException("message", StripeExceptionType.ApiKeyError);

            Mock<IStripeService> stripeServiceStub = new Mock<IStripeService>();
            stripeServiceStub.
                Setup(s => s.CreateCreditCard(It.IsAny<Member>(), It.IsAny<string>())).
                Throws(exception);

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object, stripeService: stripeServiceStub.Object);
            controller.ControllerContext = contextStub.Object;

            var result = await controller.CreateCreditCard(stripeCardToken) as HttpStatusCodeResult;

            Assert.That(result != null);
            Assert.That(result.StatusCode, Is.GreaterThanOrEqualTo((int)HttpStatusCode.InternalServerError));
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

            StripeServiceException exception = new StripeServiceException("message", StripeExceptionType.UnknownError);

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

            StripeServiceException exception = new StripeServiceException(stripeErrorMessage, StripeExceptionType.CardError);

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
    }
}
