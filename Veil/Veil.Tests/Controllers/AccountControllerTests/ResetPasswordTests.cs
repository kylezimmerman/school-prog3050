using System;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataModels.Models.Identity;
using Veil.Models;
using Veil.Services;

namespace Veil.Tests.Controllers.AccountControllerTests
{
    public class ResetPasswordTests : AccountControllerTestsBase
    {
        [Test]
        public void ResetPasswordGET_NullOrWhitespaceCode_DisplaysErrorView([Values(null, "", " ")]string code)
        {
            AccountController controller = new AccountController(userManager: null, signInManager: null, stripeService: null);

            var result = controller.ResetPassword(code) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.EqualTo("Error"));
        }

        [Test]
        public void ResetPasswordGET_ValidCode_DisplaysView()
        {
            AccountController controller = new AccountController(userManager: null, signInManager: null, stripeService: null);

            var result = controller.ResetPassword("passwordResetToken") as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.Empty.Or.EqualTo("ResetPassword"));
        }

        [Test]
        public async void ResetPassword_InvalidModelState_RedisplaysViewWithSameModel()
        {
            ResetPasswordViewModel viewModel = new ResetPasswordViewModel();

            AccountController controller = new AccountController(userManager: null, signInManager: null, stripeService: null);
            controller.ModelState.AddModelError(nameof(viewModel.Email), "Required");

            var result = await controller.ResetPassword(viewModel) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.Empty.Or.EqualTo("ResetPassword"));
            Assert.That(result.Model, Is.SameAs(viewModel));
        }

        [Test]
        public async void ResetPassword_ValidModelState_CallsUserManageFindByEmailAsyncWithModelEmail()
        {
            ResetPasswordViewModel viewModel = new ResetPasswordViewModel
            {
                Email = "example@example.com"
            };

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(null).
                Verifiable();

            AccountController controller = new AccountController(userManagerMock.Object, signInManager: null, stripeService: null);

            await controller.ResetPassword(viewModel);

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.FindByEmailAsync(viewModel.Email),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void ResetPassword_EmailNotRegistered_RedirectsToForgotPasswordConfirmation()
        {
            ResetPasswordViewModel viewModel = new ResetPasswordViewModel
            {
                Email = "example@example.com"
            };

            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerStub.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(null);

            AccountController controller = new AccountController(userManagerStub.Object, signInManager: null, stripeService: null);

            var result = await controller.ResetPassword(viewModel) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(AccountController.ResetPasswordConfirmation)));
            Assert.That(result.RouteValues["Controller"], Is.Null.Or.EqualTo("Account"));
        }

        [Test]
        public async void ResetPassword_EmailRegistered_CallsUserManageResetPasswordAsync()
        {
            ResetPasswordViewModel viewModel = new ResetPasswordViewModel
            {
                Email = "example@example.com",
                Code = "passwordResetToken",
                Password = "correct horse battery staple"
            };

            User user = new User
            {
                Id = new Guid("65ED1E57-D246-4A20-9937-E5C129E67064"),
                Email = viewModel.Email
            };

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(user);
            userManagerMock.
                Setup(um => um.ResetPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>())).
                ReturnsAsync(IdentityResult.Failed("an error.")).
                Verifiable();

            AccountController controller = new AccountController(userManagerMock.Object, signInManager: null, stripeService: null);

            await controller.ResetPassword(viewModel);

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.ResetPasswordAsync(user.Id, viewModel.Code, viewModel.Password),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void ResetPassword_ResetPasswordAsyncFails_AddsErrorsToModelState()
        {
            ResetPasswordViewModel viewModel = new ResetPasswordViewModel
            {
                Email = "example@example.com",
                Code = "passwordResetToken",
                Password = "correct horse battery staple"
            };

            User user = new User
            {
                Id = new Guid("65ED1E57-D246-4A20-9937-E5C129E67064"),
                Email = viewModel.Email
            };

            string[] identityErrors =
            {
                "error 1",
                "error 2",
                "error 3"
            };

            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerStub.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(user);
            userManagerStub.
                Setup(um => um.ResetPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>())).
                ReturnsAsync(IdentityResult.Failed(identityErrors));

            AccountController controller = new AccountController(userManagerStub.Object, signInManager: null, stripeService: null);

            await controller.ResetPassword(viewModel);

            Assert.That(controller.ModelState.Count, Is.EqualTo(1));
            Assert.That(controller.ModelState[string.Empty].Errors.Count, Is.EqualTo(identityErrors.Length));
        }

        [Test]
        public async void ResetPassword_ResetPasswordAsyncFails_RedisplaysViewWithSameModel()
        {
            ResetPasswordViewModel viewModel = new ResetPasswordViewModel
            {
                Email = "example@example.com",
                Code = "passwordResetToken",
                Password = "correct horse battery staple"
            };

            User user = new User
            {
                Id = new Guid("65ED1E57-D246-4A20-9937-E5C129E67064"),
                Email = viewModel.Email
            };

            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerStub.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(user);
            userManagerStub.
                Setup(um => um.ResetPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>())).
                ReturnsAsync(IdentityResult.Failed("an error."));

            AccountController controller = new AccountController(userManagerStub.Object, signInManager: null, stripeService: null);

            var result = await controller.ResetPassword(viewModel) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.Empty.Or.EqualTo("ResetPassword"));
            Assert.That(result.Model, Is.SameAs(viewModel));
        }

        [Test]
        public async void ResetPassword_PasswordChangedSuccessfully_CallsUserManagerUpdateSecurityStampAsync()
        {
            ResetPasswordViewModel viewModel = new ResetPasswordViewModel
            {
                Email = "example@example.com",
                Code = "passwordResetToken",
                Password = "correct horse battery staple"
            };

            User user = new User
            {
                Id = new Guid("65ED1E57-D246-4A20-9937-E5C129E67064"),
                Email = viewModel.Email
            };

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(user);
            userManagerMock.
                Setup(um => um.ResetPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>())).
                ReturnsAsync(IdentityResult.Success);
            userManagerMock.
                Setup(um => um.UpdateSecurityStampAsync(It.IsAny<Guid>())).
                ReturnsAsync(IdentityResult.Success).
                Verifiable();

            AccountController controller = new AccountController(userManagerMock.Object, signInManager: null, stripeService: null);

            await controller.ResetPassword(viewModel);

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.UpdateSecurityStampAsync(user.Id),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void ResetPassword_PasswordChangedSuccessfully_RedirectsToResetPasswordConfirmation()
        {
            ResetPasswordViewModel viewModel = new ResetPasswordViewModel
            {
                Email = "example@example.com",
                Code = "passwordResetToken",
                Password = "correct horse battery staple"
            };

            User user = new User
            {
                Id = new Guid("65ED1E57-D246-4A20-9937-E5C129E67064"),
                Email = viewModel.Email
            };

            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerStub.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(user);
            userManagerStub.
                Setup(um => um.ResetPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>())).
                ReturnsAsync(IdentityResult.Success);
            userManagerStub.
                Setup(um => um.UpdateSecurityStampAsync(It.IsAny<Guid>())).
                ReturnsAsync(IdentityResult.Success);

            AccountController controller = new AccountController(userManagerStub.Object, signInManager: null, stripeService: null);

            var result = await controller.ResetPassword(viewModel) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(AccountController.ResetPasswordConfirmation)));
            Assert.That(result.RouteValues["Controller"], Is.Empty.Or.EqualTo("Account"));
        }

        [Test]
        public void ResetPasswordConfirmation_WhenCalled_DisplaysView()
        {
            AccountController controller = new AccountController(userManager: null, signInManager: null, stripeService: null);

            var result = controller.ResetPasswordConfirmation() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.Empty.Or.EqualTo("ResetPasswordConfirmation"));
        }
    }
}
