/* ChangePasswordTests.cs
 *      Sean Coombes, 2015.12.09: Created
 */

using System;
using System.Data.Entity.Validation;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models.Identity;
using Veil.Helpers;
using Veil.Models;
using Veil.Services;

namespace Veil.Tests.Controllers.ManageControllerTests
{
    public class ChangePasswordTests : ManageControllerTestsBase
    {
        [Test]
        public void ChangePasswordGET_WhenCalled_ReturnsView()
        {
            ManageController controller = new ManageController(userManager: null, signInManager: null, veilDataAccess: null, idGetter: null, stripeService: null);

            var result = controller.ChangePassword();

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.Empty.Or.EqualTo(nameof(ManageController.ChangePassword)));
        }

        [Test]
        public async void ChangePassword_SuccessfulChange()
        {

            ChangePasswordViewModel passwordModel = new ChangePasswordViewModel()
            {
                OldPassword = "oldPassword",
                NewPassword = "newPassword",
            };

            User user = new User()
            {
                Id = memberId,
                Email = "person@email.com",
                NewEmail = "newEmail@email.com"
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<IAuthenticationManager> authenticationManagerStub = new Mock<IAuthenticationManager>();
            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/,
                null /*dataProtectionProvider*/);

            userManagerStub.Setup(um => um.FindByIdAsync(memberId)).ReturnsAsync(user);
            userManagerStub.Setup(um => um.ChangePasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            dbStub.Setup(db => db.UserStore).Returns(userStoreStub.Object);

            Mock<VeilSignInManager> signInManagerMock = new Mock<VeilSignInManager>(userManagerStub.Object, authenticationManagerStub.Object);
            signInManagerMock.Setup(sm => sm.SignInAsync(It.IsAny<User>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(Task.FromResult(0));

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(memberId);

            ManageController controller = new ManageController(userManagerStub.Object, signInManagerMock.Object, dbStub.Object,
                idGetterStub.Object, null)
            {
                ControllerContext = context.Object
            };

            var result = await controller.ChangePassword(passwordModel) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Index"));
        }

        [Test]
        public async void ChangePassword_InvalidModelState()
        {
            ChangePasswordViewModel passwordModel = new ChangePasswordViewModel();
            ManageController controller = new ManageController(null, null, null, null, null);

            controller.ModelState.AddModelError("error", "this is an error");

            var result = await controller.ChangePassword(passwordModel);

            Assert.That(result != null);
        }

        [Test]
        public async void ChangePassword_ThrowsDbEntityValidationException()
        {

            ChangePasswordViewModel passwordModel = new ChangePasswordViewModel()
            {
                NewPassword = "newPassword",
                OldPassword = "oldPassword"
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/,
                null /*dataProtectionProvider*/);

            userManagerStub.Setup(um => um.ChangePasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new DbEntityValidationException());

            dbStub.Setup(db => db.UserStore).Returns(userStoreStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(memberId);

            ManageController controller = new ManageController(userManagerStub.Object, null, dbStub.Object, idGetterStub.Object, null)
            {
                ControllerContext = context.Object
            };

            var result = await controller.ChangePassword(passwordModel);

            Assert.That(result != null);
        }

        [Test]
        public async void ChangePassword_ResultEqualsIdentityResultFailed()
        {

            ChangePasswordViewModel passwordModel = new ChangePasswordViewModel()
            {
                OldPassword = "oldPassword",
                NewPassword = "newPassword",
                ConfirmPassword = "newPassword"
            };

            User user = new User()
            {
                Id = memberId,
                Email = "person@email.com",
                NewEmail = "newEmail@email.com"
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<IAuthenticationManager> authenticationManagerStub = new Mock<IAuthenticationManager>();
            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/,
                null /*dataProtectionProvider*/);

            userManagerStub.Setup(um => um.FindByIdAsync(memberId)).ReturnsAsync(user);
            userManagerStub.Setup(um => um.ChangePasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed());

            dbStub.Setup(db => db.UserStore).Returns(userStoreStub.Object);

            Mock<VeilSignInManager> signInManagerMock = new Mock<VeilSignInManager>(userManagerStub.Object, authenticationManagerStub.Object);
            signInManagerMock.Setup(sm => sm.SignInAsync(It.IsAny<User>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(Task.FromResult(0));



            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(memberId);

            ManageController controller = new ManageController(userManagerStub.Object, signInManagerMock.Object, dbStub.Object,
                idGetterStub.Object, null)
            {
                ControllerContext = context.Object
            };

            var result = await controller.ChangePassword(passwordModel);

            Assert.That(result != null);
        }
    }
}
