using System;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataModels.Models.Identity;
using Veil.Models;
using Veil.Services;

namespace Veil.Tests.Controllers.AccountControllerTests
{
    public class ForgotPasswordTests : AccountControllerTestsBase
    {
        [Test]
        public void ForgotPasswordGET_Unauthenticated_ReturnsAViewResult()
        {
            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.
                Setup(c => c.HttpContext.User.Identity.IsAuthenticated).
                Returns(false);

            AccountController controller = new AccountController(userManager: null, signInManager: null, stripeService: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = controller.ForgotPassword() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.Empty.Or.EqualTo("ForgotPassword"));
        }

        [Test]
        public void ForgotPasswordGET_Authenticated_RedirectsToManageChangePassword()
        {
            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.
                Setup(c => c.HttpContext.User.Identity.IsAuthenticated).
                Returns(true);

            AccountController controller = new AccountController(userManager: null, signInManager: null, stripeService: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = controller.ForgotPassword() as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo("ChangePassword"));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Manage"));
        }

        [Test]
        public async void ForgotPassword_InvalidModelState_RedisplaysWithSameModel()
        {
            ForgotPasswordViewModel viewModel = new ForgotPasswordViewModel();

            AccountController controller = new AccountController(userManager: null, signInManager: null, stripeService: null);

            controller.ModelState.AddModelError(nameof(viewModel.Email), "Required");

            var result = await controller.ForgotPassword(viewModel) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<ForgotPasswordViewModel>());

            var model = (ForgotPasswordViewModel)result.Model;

            Assert.That(model, Is.SameAs(viewModel));
        }

        [Test]
        public async void ForgotPassword_ValidModelState_CallsUserManageFindByEmailAsyncWithModelEmail()
        {
            ForgotPasswordViewModel viewModel = new ForgotPasswordViewModel
            {
                Email = "example@example.com"
            };

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(null).
                Verifiable();

            AccountController controller = new AccountController(userManagerMock.Object, signInManager: null, stripeService: null);

            await controller.ForgotPassword(viewModel);

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.FindByEmailAsync(viewModel.Email),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void ForgotPassword_EmailNotRegistered_RedirectsToForgotPasswordConfirmation()
        {
            ForgotPasswordViewModel viewModel = new ForgotPasswordViewModel
            {
                Email = "example@example.com"
            };

            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerStub.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(null);

            AccountController controller = new AccountController(userManagerStub.Object, signInManager: null, stripeService: null);

            var result = await controller.ForgotPassword(viewModel) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(AccountController.ForgotPasswordConfirmation)));
            Assert.That(result.RouteValues["Controller"], Is.Null.Or.EqualTo("Account"));
        }

        [Test]
        public async void ForgotPassword_EmailRegistered_CallsUserManagerIsEmailConfirmedAsync()
        {
            ForgotPasswordViewModel viewModel = new ForgotPasswordViewModel
            {
                Email = "example@example.com"
            };

            User user = new User
            {
                Id = new Guid("65ED1E57-D246-4A20-9937-E5C129E67064"),
                Email = viewModel.Email
            };

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.FindByEmailAsync(viewModel.Email)).
                ReturnsAsync(user);
            userManagerMock.
                Setup(um => um.IsEmailConfirmedAsync(It.IsAny<Guid>())).
                ReturnsAsync(false).
                Verifiable();

            AccountController controller = new AccountController(userManagerMock.Object, signInManager: null, stripeService: null);

            await controller.ForgotPassword(viewModel);

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.IsEmailConfirmedAsync(user.Id),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void ForgotPassword_EmailNotConfirmed_RedirectsToForgotPasswordConfirmation()
        {
            ForgotPasswordViewModel viewModel = new ForgotPasswordViewModel
            {
                Email = "example@example.com"
            };

            User user = new User
            {
                Id = new Guid("65ED1E57-D246-4A20-9937-E5C129E67064"),
                Email = viewModel.Email
            };

            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerStub.
                Setup(um => um.FindByEmailAsync(viewModel.Email)).
                ReturnsAsync(user);
            userManagerStub.
                Setup(um => um.IsEmailConfirmedAsync(user.Id)).
                ReturnsAsync(false);

            AccountController controller = new AccountController(userManagerStub.Object, signInManager: null, stripeService: null);

            var result = await controller.ForgotPassword(viewModel) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(AccountController.ForgotPasswordConfirmation)));
            Assert.That(result.RouteValues["Controller"], Is.Null.Or.EqualTo("Account"));
        }

        [Test]
        public async void ForgotPassword_EmailConfirmed_GeneratesPasswordResetToken()
        {
            ForgotPasswordViewModel viewModel = new ForgotPasswordViewModel
            {
                Email = "example@example.com"
            };

            User user = new User
            {
                Id = new Guid("65ED1E57-D246-4A20-9937-E5C129E67064"),
                Email = viewModel.Email
            };

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.FindByEmailAsync(viewModel.Email)).
                ReturnsAsync(user);
            userManagerMock.
                Setup(um => um.IsEmailConfirmedAsync(user.Id)).
                ReturnsAsync(true);
            userManagerMock.
                Setup(um => um.GeneratePasswordResetTokenAsync(It.IsAny<Guid>())).
                ReturnsAsync("passwordResetToken").
                Verifiable();

            Mock<UrlHelper> urlHelperStub = new Mock<UrlHelper>();

            Uri requestUrl = new Uri("http://localhost/");

            Mock<HttpRequestBase> requestStub = new Mock<HttpRequestBase>();
            requestStub.
                SetupGet(r => r.Url).
                Returns(requestUrl);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.
                SetupGet(c => c.HttpContext.Request).
                Returns(requestStub.Object);

            AccountController controller = new AccountController(userManagerMock.Object, signInManager: null, stripeService: null)
            {
                Url = urlHelperStub.Object,
                ControllerContext = contextStub.Object
            };

            await controller.ForgotPassword(viewModel);

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.GeneratePasswordResetTokenAsync(user.Id),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void ForgotPassword_EmailConfirmed_CallsUserManageSendEmailAsync()
        {
            ForgotPasswordViewModel viewModel = new ForgotPasswordViewModel
            {
                Email = "example@example.com"
            };

            User user = new User
            {
                Id = new Guid("65ED1E57-D246-4A20-9937-E5C129E67064"),
                Email = viewModel.Email
            };

            string passwordResetToken = "passwordResetToken";

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.FindByEmailAsync(viewModel.Email)).
                ReturnsAsync(user);
            userManagerMock.
                Setup(um => um.IsEmailConfirmedAsync(user.Id)).
                ReturnsAsync(true);
            userManagerMock.
                Setup(um => um.GeneratePasswordResetTokenAsync(It.IsAny<Guid>())).
                ReturnsAsync(passwordResetToken);
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
                SetupGet(c => c.HttpContext.Request).
                Returns(requestStub.Object);

            AccountController controller = new AccountController(userManagerMock.Object, signInManager: null, stripeService: null)
            {
                Url = urlHelperStub.Object,
                ControllerContext = contextStub.Object
            };

            await controller.ForgotPassword(viewModel);

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.SendEmailAsync(user.Id, It.IsAny<string>(), It.IsAny<string>()),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void ForgotPassword_EmailConfirmed_RedirectsToForgotPasswordConfirmation()
        {
            ForgotPasswordViewModel viewModel = new ForgotPasswordViewModel
            {
                Email = "example@example.com"
            };

            User user = new User
            {
                Id = new Guid("65ED1E57-D246-4A20-9937-E5C129E67064"),
                Email = viewModel.Email
            };

            string passwordResetToken = "passwordResetToken";

            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerStub.
                Setup(um => um.FindByEmailAsync(viewModel.Email)).
                ReturnsAsync(user);
            userManagerStub.
                Setup(um => um.IsEmailConfirmedAsync(user.Id)).
                ReturnsAsync(true);
            userManagerStub.
                Setup(um => um.GeneratePasswordResetTokenAsync(It.IsAny<Guid>())).
                ReturnsAsync(passwordResetToken);
            userManagerStub.
                Setup(um => um.SendEmailAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>())).
                Returns(Task.FromResult(0));

            Mock<UrlHelper> urlHelperStub = new Mock<UrlHelper>();

            Uri requestUrl = new Uri("http://localhost/");

            Mock<HttpRequestBase> requestStub = new Mock<HttpRequestBase>();
            requestStub.
                SetupGet(r => r.Url).
                Returns(requestUrl);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.
                SetupGet(c => c.HttpContext.Request).
                Returns(requestStub.Object);

            AccountController controller = new AccountController(userManagerStub.Object, signInManager: null, stripeService: null)
            {
                Url = urlHelperStub.Object,
                ControllerContext = contextStub.Object
            };

            var result = await controller.ForgotPassword(viewModel) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(AccountController.ForgotPasswordConfirmation)));
            Assert.That(result.RouteValues["Controller"], Is.Null.Or.EqualTo("Account"));
        }

        [Test]
        public void ForgotPasswordConfirmation_WhenCalled_ReturnsViewResult()
        {
            AccountController controller = new AccountController(userManager: null, signInManager: null, stripeService: null);

            var result = controller.ForgotPasswordConfirmation() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.Empty.Or.EqualTo("ForgotPasswordConfirmation"));
        }
    }
}
