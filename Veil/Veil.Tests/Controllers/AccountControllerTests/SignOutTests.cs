using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.Services;

namespace Veil.Tests.Controllers.AccountControllerTests
{
    public class SignOutTests : AccountControllerTestsBase
    {
        [Test]
        public void SignOut_WhenCalled_CallsAuthenticationManager()
        {
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);

            Mock<IAuthenticationManager> authenticationManagerMock = new Mock<IAuthenticationManager>();
            authenticationManagerMock.
                Setup(am => am.SignOut(It.IsAny<string>())).
                Verifiable();

            Mock<VeilSignInManager> signInManagerStub = new Mock<VeilSignInManager>(userManagerStub.Object, authenticationManagerMock.Object);

            AccountController controller = new AccountController(userManager: null, signInManager: signInManagerStub.Object, stripeService: null);

            controller.LogOff();

            Assert.That(
                () =>
                    authenticationManagerMock.Verify(am => am.SignOut(DefaultAuthenticationTypes.ApplicationCookie),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public void SignOut_WhenCalled_RedirectsToHomeIndex()
        {
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);

            Mock<IAuthenticationManager> authenticationManagerMock = new Mock<IAuthenticationManager>();
            authenticationManagerMock.
                Setup(am => am.SignOut(It.IsAny<string>())).
                Verifiable();

            Mock<VeilSignInManager> signInManagerStub = new Mock<VeilSignInManager>(userManagerStub.Object, authenticationManagerMock.Object);

            AccountController controller = new AccountController(userManager: null, signInManager: signInManagerStub.Object, stripeService: null);

            var result = controller.LogOff() as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(HomeController.Index)));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Home"));
        }
    }
}
