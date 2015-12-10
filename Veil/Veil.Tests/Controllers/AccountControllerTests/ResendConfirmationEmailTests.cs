using System;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataModels.Models.Identity;
using Veil.Services;

namespace Veil.Tests.Controllers.AccountControllerTests
{
    public class ResendConfirmationEmailTests : AccountControllerTestsBase
    {
        [Test]
        public async void ResendConfirmationEmail_UnauthenticatedValidUserEmail_CallsUserManageSendEmailAsync()
        {
            string emailAddress = "fake@example.com";
            User user = new User
            {
                Email = emailAddress,
                Id = Guid.ParseExact("CF4A34A7-4246-48CF-81A7-5EE79A216E02", "D")
            };

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.FindByEmailAsync(It.Is<string>(val => val == emailAddress))).
                ReturnsAsync(user);
            userManagerMock.
                Setup(um => um.GenerateEmailConfirmationTokenAsync(It.Is<Guid>(val => val == user.Id))).
                ReturnsAsync("token");
            userManagerMock.
                Setup(um => um.SendEmailAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>())).
                Returns(Task.FromResult(0)).
                Verifiable();

            Mock<UrlHelper> urlHelperStub = new Mock<UrlHelper>();

            Uri requestUrl = new Uri("http://localhost/");

            Mock<HttpRequestBase> requestStub = new Mock<HttpRequestBase>();
            requestStub.
                SetupGet(r => r.Url).
                Returns(requestUrl);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.
                Setup(c => c.HttpContext.User.Identity.IsAuthenticated).
                Returns(false);
            contextStub.
                SetupGet(c => c.HttpContext.Request).
                Returns(requestStub.Object);

            AccountController controller = new AccountController(userManager: userManagerMock.Object, signInManager: null, stripeService: null)
            {
                Url = urlHelperStub.Object,
                ControllerContext = contextStub.Object
            };

            await controller.ResendConfirmationEmail(emailAddress);

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.SendEmailAsync(It.Is<Guid>(val => val == user.Id), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void ResendConfirmationEmail_AuthenticatedUser_RedirectsToHomeIndex()
        {
            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.
                Setup(c => c.HttpContext.User.Identity.IsAuthenticated).
                Returns(true);

            AccountController controller = new AccountController(userManager: null, signInManager: null, stripeService: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.ResendConfirmationEmail(null) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Index"));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Home"));
        }
    }
}
