/* IndexTests.cs
 *      Drew Matheson, 2015.12.08: Created
 */

using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;
using Veil.Helpers;
using Veil.Models;
using Veil.Services;

namespace Veil.Tests.Controllers.ManageControllerTests
{
    public class IndexTests : ManageControllerTestsBase
    {
        [Test]
        public async void Index_NonNullManageMessageId_AddsAlert([Values(
            ManageController.ManageMessageId.Error,
            ManageController.ManageMessageId.AddPhoneSuccess,
            ManageController.ManageMessageId.ChangePasswordSuccess,
            ManageController.ManageMessageId.RemoveLoginSuccess,
            ManageController.ManageMessageId.RemovePhoneSuccess,
            ManageController.ManageMessageId.SetPasswordSuccess,
            ManageController.ManageMessageId.SetTwoFactorSuccess)] ManageController.ManageMessageId messageIdValue)
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();

            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/,
                null /*dataProtectionProvider*/);

            userManagerStub.Setup(um => um.FindByIdAsync(memberId)).ReturnsAsync(userWithMember);
            dbStub.Setup(db => db.UserStore).Returns(userStoreStub.Object);

            Mock<IAuthenticationManager> authenticationManagerStub = new Mock<IAuthenticationManager>();

            authenticationManagerStub.Setup(am => am.SignOut(It.IsAny<string>()));

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            Mock<VeilSignInManager> signInManagerStub = new Mock<VeilSignInManager>(userManagerStub.Object, authenticationManagerStub.Object);

            ManageController controller = CreateManageController(
                userManagerStub.Object, signInManagerStub.Object, dbStub.Object, idGetterStub.Object);

            controller.ControllerContext = contextStub.Object;

            await controller.Index(messageIdValue);

            Assert.That(controller.TempData[AlertHelper.ALERT_MESSAGE_KEY],
                Is.Not.Empty);
        }

        [Test]
        public async void Index_NullUser_LogsOut()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();

            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/,
                null /*dataProtectionProvider*/);

            userManagerStub.Setup(um => um.FindByIdAsync(memberId)).ReturnsAsync(null);
            dbStub.Setup(db => db.UserStore).Returns(userStoreStub.Object);

            Mock<IAuthenticationManager> authenticationManagerMock = new Mock<IAuthenticationManager>();
            authenticationManagerMock.
                Setup(am => am.SignOut(It.IsAny<string>())).
                Verifiable();

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            Mock<VeilSignInManager> signInManagerStub = new Mock<VeilSignInManager>(userManagerStub.Object, authenticationManagerMock.Object);

            ManageController controller = CreateManageController(
                userManagerStub.Object, signInManagerStub.Object, dbStub.Object, idGetterStub.Object);

            controller.ControllerContext = contextStub.Object;

            await controller.Index(null);

            Assert.That(
                () =>
                    authenticationManagerMock.Verify(ams => ams.SignOut(It.IsAny<string>()),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void Index_NullUser_RedirectsToHomeIndex()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();

            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/,
                null /*dataProtectionProvider*/);

            userManagerStub.Setup(um => um.FindByIdAsync(memberId)).ReturnsAsync(null);
            dbStub.Setup(db => db.UserStore).Returns(userStoreStub.Object);

            Mock<IAuthenticationManager> authenticationManagerStub = new Mock<IAuthenticationManager>();
            authenticationManagerStub.
                Setup(am => am.SignOut(It.IsAny<string>()));

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            Mock<VeilSignInManager> signInManagerStub = new Mock<VeilSignInManager>(userManagerStub.Object, authenticationManagerStub.Object);

            ManageController controller = CreateManageController(
                userManagerStub.Object, signInManagerStub.Object, dbStub.Object, idGetterStub.Object);

            controller.ControllerContext = contextStub.Object;

            var result = await controller.Index(null) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(HomeController.Index)));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Home"));
        }

        [Test]
        public async void Index_NullMemberOnUser_RedirectsToHomeIndex()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();

            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/,
                null /*dataProtectionProvider*/);

            userWithMember.Member = null;

            userManagerStub.Setup(um => um.FindByIdAsync(memberId)).ReturnsAsync(userWithMember);
            dbStub.Setup(db => db.UserStore).Returns(userStoreStub.Object);

            Mock<IAuthenticationManager> authenticationManagerStub = new Mock<IAuthenticationManager>();
            authenticationManagerStub.
                Setup(am => am.SignOut(It.IsAny<string>()));

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            Mock<VeilSignInManager> signInManagerStub = new Mock<VeilSignInManager>(userManagerStub.Object, authenticationManagerStub.Object);

            ManageController controller = CreateManageController(
                userManagerStub.Object, signInManagerStub.Object, dbStub.Object, idGetterStub.Object);

            controller.ControllerContext = contextStub.Object;

            var result = await controller.Index(null) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(HomeController.Index)));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Home"));
        }

        [Test]
        public async void Index_ValidUser_SetsUpViewModel()
        {
            userWithMember.PhoneNumber = "800 555 5199";
            userWithMember.FirstName = "John";
            userWithMember.LastName = "Doe";
            userWithMember.Email = "john.doe@example.com";
            userWithMember.Member.ReceivePromotionalEmails = true;
            userWithMember.Member.FavoritePlatforms = new List<Platform> { new Platform(), new Platform() };
            userWithMember.Member.FavoriteTags = new List<Tag> { new Tag(), new Tag() };
            userWithMember.Member.WishListVisibility = WishListVisibility.Private;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();

            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/,
                null /*dataProtectionProvider*/);

            userManagerStub.Setup(um => um.FindByIdAsync(memberId)).ReturnsAsync(userWithMember);
            dbStub.Setup(db => db.UserStore).Returns(userStoreStub.Object);

            Mock<IAuthenticationManager> authenticationManagerStub = new Mock<IAuthenticationManager>();
            authenticationManagerStub.
                Setup(am => am.SignOut(It.IsAny<string>()));

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            Mock<VeilSignInManager> signInManagerStub = new Mock<VeilSignInManager>(userManagerStub.Object, authenticationManagerStub.Object);

            ManageController controller = CreateManageController(
                userManagerStub.Object, signInManagerStub.Object, dbStub.Object, idGetterStub.Object);

            controller.ControllerContext = contextStub.Object;

            var result = await controller.Index(null) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<IndexViewModel>());

            var model = (IndexViewModel)result.Model;

            Assert.That(model.PhoneNumber, Is.SameAs(userWithMember.PhoneNumber));
            Assert.That(model.MemberFirstName, Is.SameAs(userWithMember.FirstName));
            Assert.That(model.MemberLastName, Is.SameAs(userWithMember.LastName));
            Assert.That(model.MemberEmail, Is.SameAs(userWithMember.Email));
            Assert.That(model.MemberVisibility, Is.EqualTo(userWithMember.Member.WishListVisibility));
            Assert.That(model.ReceivePromotionalEmails, Is.EqualTo(userWithMember.Member.ReceivePromotionalEmails));
            Assert.That(model.FavoritePlatformCount, Is.EqualTo(userWithMember.Member.FavoritePlatforms.Count));
            Assert.That(model.FavoriteTagCount, Is.EqualTo(userWithMember.Member.FavoriteTags.Count));
        }

        [Test]
        public async void Index_ValidUser_ReturnsView()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();

            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/,
                null /*dataProtectionProvider*/);

            userManagerStub.Setup(um => um.FindByIdAsync(memberId)).ReturnsAsync(userWithMember);
            dbStub.Setup(db => db.UserStore).Returns(userStoreStub.Object);

            Mock<IAuthenticationManager> authenticationManagerStub = new Mock<IAuthenticationManager>();
            authenticationManagerStub.
                Setup(am => am.SignOut(It.IsAny<string>()));

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            Mock<VeilSignInManager> signInManagerStub = new Mock<VeilSignInManager>(userManagerStub.Object, authenticationManagerStub.Object);

            ManageController controller = CreateManageController(
                userManagerStub.Object, signInManagerStub.Object, dbStub.Object, idGetterStub.Object);

            controller.ControllerContext = contextStub.Object;

            var result = await controller.Index(null) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.Empty.Or.EqualTo("Index"));
        }
    }
}
