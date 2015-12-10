using System;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.Services;

namespace Veil.Tests.Controllers.AccountControllerTests
{
    public class ConfirmEmailTests : AccountControllerTestsBase
    {
        [Test]
        public async void ConfirmEmail_DefaultId_ReturnsErrorView()
        {
            AccountController controller = new AccountController(userManager: null, signInManager: null, stripeService: null);

            var result = await controller.ConfirmEmail(Guid.Empty, "token") as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.EqualTo("Error"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public async void ConfirmEmail_InvalidCode_ReturnsErrorView(string code)
        {
            AccountController controller = new AccountController(userManager: null, signInManager: null, stripeService: null);

            var result = await controller.ConfirmEmail(Guid.ParseExact("3F14F913-9540-43EA-8F1A-5B08F89A3560", "D"), code) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.EqualTo("Error"));
        }

        [Test]
        public async void ConfirmEmail_ValidIdAndCodeButFailedEmailConfirmResult_ReturnsErrorView()
        {
            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.ConfirmEmailAsync(It.IsAny<Guid>(), It.IsAny<string>())).
                ReturnsAsync(IdentityResult.Failed());

            AccountController controller = new AccountController(userManager: userManagerMock.Object, signInManager: null, stripeService: null);

            var result = await controller.ConfirmEmail(Guid.ParseExact("3F14F913-9540-43EA-8F1A-5B08F89A3560", "D"), "token") as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.EqualTo("Error"));
        }

        [Test]
        public async void ConfirmEmail_ValidIdAndCode_CallsUserManagerConfirmEmailAsync()
        {
            Guid userId = Guid.ParseExact("3F14F913-9540-43EA-8F1A-5B08F89A3560", "D");
            string token = "token";

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.ConfirmEmailAsync(It.IsAny<Guid>(), It.IsAny<string>())).
                ReturnsAsync(IdentityResult.Failed() /* Return a failed result to minimize the code executed */).
                Verifiable();

            AccountController controller = new AccountController(userManager: userManagerMock.Object, signInManager: null, stripeService: null);

            await controller.ConfirmEmail(userId, token);

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.ConfirmEmailAsync(It.Is<Guid>(val => val == userId), It.Is<string>(val => val == token)), Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void ConfirmEmail_ValidIdAndCodeAndSuccessfulEmailConfirmResult_ReturnsConfirmEmailView()
        {
            Guid userId = Guid.ParseExact("3F14F913-9540-43EA-8F1A-5B08F89A3560", "D");
            string token = "token";

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.ConfirmEmailAsync(It.Is<Guid>(val => val == userId), It.Is<string>(val => val == token))).
                ReturnsAsync(IdentityResult.Success);
            userManagerMock.
                Setup(um => um.UpdateSecurityStampAsync(It.Is<Guid>(val => val == userId))).
                ReturnsAsync(IdentityResult.Success);

            AccountController controller = new AccountController(userManager: userManagerMock.Object, signInManager: null, stripeService: null);

            var result = await controller.ConfirmEmail(userId, token) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.EqualTo("ConfirmEmail"));
        }

        [Test]
        public async void ConfirmEmail_ValidIdAndCodeAndSuccessfulEmailConfirmResult_InvalidatesEmailToken()
        {
            Guid userId = Guid.ParseExact("3F14F913-9540-43EA-8F1A-5B08F89A3560", "D");
            string token = "token";

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.ConfirmEmailAsync(It.Is<Guid>(val => val == userId), It.Is<string>(val => val == token))).
                ReturnsAsync(IdentityResult.Success);
            userManagerMock.
                Setup(um => um.UpdateSecurityStampAsync(It.Is<Guid>(val => val == userId))).
                ReturnsAsync(IdentityResult.Success).
                Verifiable();

            AccountController controller = new AccountController(userManager: userManagerMock.Object, signInManager: null, stripeService: null);

            await controller.ConfirmEmail(userId, token);

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.UpdateSecurityStampAsync(It.Is<Guid>(val => val == userId)), Times.Exactly(1)),
                Throws.Nothing);
        }
    }
}
