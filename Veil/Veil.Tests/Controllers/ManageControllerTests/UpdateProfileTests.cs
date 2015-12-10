/* UpdateProfileTests.cs
 *      Sean Coombes, 2015.12.01: Created
 */

using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
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
    public class UpdateProfileTests : ManageControllerTestsBase
    {
        [Test]
        public async void UpdateUserInformation_NoEmailChange()
        {
            IndexViewModel viewModel = new IndexViewModel()
            {
                MemberEmail = "person@email.com",
                MemberFirstName = "firstName",
                MemberLastName = "lastName",
                MemberVisibility = WishListVisibility.FriendsOnly,
                ReceivePromotionalEmails = true
            };

            Member member = new Member()
            {
                UserId = memberId,
                WishListVisibility = WishListVisibility.FriendsOnly,
                ReceivePromotionalEmails = true
            };

            User user = new User()
            {
                Id = memberId,
                Email = "person@email.com",
                Member = member
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/,
                null /*dataProtectionProvider*/);

            userManagerStub.Setup(um => um.FindByIdAsync(memberId)).ReturnsAsync(user);
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

            var result = await controller.UpdateProfile(viewModel) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Index"));
        }

        [Test]
        public async void UpdateProfile_NullUser()
        {
            IndexViewModel viewModel = new IndexViewModel()
            {
                MemberEmail = "person@email.com",
                MemberFirstName = "firstName",
                MemberLastName = "lastName",
                MemberVisibility = WishListVisibility.FriendsOnly,
                ReceivePromotionalEmails = true
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            Mock<IAuthenticationManager> authenticationManagerStub = new Mock<IAuthenticationManager>();

            authenticationManagerStub.Setup(am => am.SignOut(It.IsAny<string>()));
            userManagerStub.Setup(um => um.FindByIdAsync(memberId)).ReturnsAsync(null);
            dbStub.Setup(db => db.UserStore).Returns(userStoreStub.Object);

            Mock<VeilSignInManager> signInManagerStub = new Mock<VeilSignInManager>(userManagerStub.Object, authenticationManagerStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(memberId);

            ManageController controller = new ManageController(userManagerStub.Object, signInManagerStub.Object, dbStub.Object, idGetterStub.Object, null)
            {
                ControllerContext = context.Object
            };

            var result = await controller.UpdateProfile(viewModel) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Index"));
        }

        [Test]
        public async void UpdateProfile_MemberNull()
        {
            IndexViewModel viewModel = new IndexViewModel()
            {
                MemberEmail = "person@email.com",
                MemberFirstName = "firstName",
                MemberLastName = "lastName",
                MemberVisibility = WishListVisibility.FriendsOnly,
                ReceivePromotionalEmails = true
            };

            User user = new User()
            {
                Id = memberId,
                Email = "person@email.com",
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/,
                null /*dataProtectionProvider*/);

            userManagerStub.Setup(um => um.FindByIdAsync(memberId)).ReturnsAsync(user);
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

            var result = await controller.UpdateProfile(viewModel) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Index"));
        }

        [Test]
        public async void UpdateProfile_InvalidModelState_RedisplaysIndexViewWithSameViewModel()
        {
            IndexViewModel viewModel = new IndexViewModel()
            {
                MemberFirstName = "firstName",
                MemberLastName = "lastName",
                MemberVisibility = WishListVisibility.FriendsOnly,
                ReceivePromotionalEmails = true
            };

            Member member = new Member()
            {
                UserId = memberId,
                WishListVisibility = WishListVisibility.FriendsOnly,
                ReceivePromotionalEmails = true,
                FavoritePlatforms = new List<Platform> { new Platform(), new Platform() },
                FavoriteTags = new List<Tag> { new Tag(), new Tag() }
            };

            User user = new User()
            {
                Id = memberId,
                Email = "person@email.com",
                Member = member
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/,
                null /*dataProtectionProvider*/);

            userManagerStub.Setup(um => um.FindByIdAsync(memberId)).ReturnsAsync(user);
            userManagerStub.Setup(um => um.GenerateEmailConfirmationTokenAsync(It.IsAny<Guid>())).ReturnsAsync("emailToken");
            userManagerStub.Setup(um => um.SendNewEmailConfirmationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(0));

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

            controller.ModelState.AddModelError(nameof(viewModel.MemberEmail), "Required");

            var result = await controller.UpdateProfile(viewModel) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.Empty.Or.EqualTo(nameof(ManageController.Index)));
            Assert.That(result.Model, Is.SameAs(viewModel));
            Assert.That(viewModel.FavoritePlatformCount, Is.EqualTo(member.FavoritePlatforms.Count));
            Assert.That(viewModel.FavoriteTagCount, Is.EqualTo(member.FavoriteTags.Count));
        }

        [Test]
        public async void UpdateProfile_SetNewEmail()
        {
            IndexViewModel viewModel = new IndexViewModel()
            {
                MemberEmail = "newEmail@email.com",
                MemberFirstName = "firstName",
                MemberLastName = "lastName",
                MemberVisibility = WishListVisibility.FriendsOnly,
                ReceivePromotionalEmails = true
            };

            Member member = new Member()
            {
                UserId = memberId,
                WishListVisibility = WishListVisibility.FriendsOnly,
                ReceivePromotionalEmails = true
            };

            User user = new User()
            {
                Id = memberId,
                Email = "person@email.com",
                Member = member
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/,
                null /*dataProtectionProvider*/);

            userManagerStub.Setup(um => um.FindByIdAsync(memberId)).ReturnsAsync(user);
            userManagerStub.Setup(um => um.GenerateEmailConfirmationTokenAsync(It.IsAny<Guid>())).ReturnsAsync("emailToken");
            userManagerStub.Setup(um => um.SendNewEmailConfirmationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(0));

            dbStub.Setup(db => db.UserStore).Returns(userStoreStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(memberId);

            Mock<UrlHelper> urlHelperStub = new Mock<UrlHelper>();
            Uri requestUrl = new Uri("http://localhost/");
            Mock<HttpRequestBase> requestStub = new Mock<HttpRequestBase>();
            requestStub.SetupGet(r => r.Url).Returns(requestUrl);
            context.SetupGet(c => c.HttpContext.Request).Returns(requestStub.Object);

            ManageController controller = new ManageController(userManagerStub.Object, null, dbStub.Object, idGetterStub.Object, null)
            {
                ControllerContext = context.Object,
                Url = urlHelperStub.Object
            };

            var result = await controller.UpdateProfile(viewModel) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Index"));
        }

        [Test]
        public async void UpdateProfile_ReplaceNewEmailWithNewerEmail()
        {
            IndexViewModel viewModel = new IndexViewModel()
            {
                MemberEmail = "newerEmail@email.com",
                MemberFirstName = "firstName",
                MemberLastName = "lastName",
                MemberVisibility = WishListVisibility.FriendsOnly,
                ReceivePromotionalEmails = true
            };

            Member member = new Member()
            {
                UserId = memberId,
                WishListVisibility = WishListVisibility.FriendsOnly,
                ReceivePromotionalEmails = true
            };

            User user = new User()
            {
                Id = memberId,
                Email = "person@email.com",
                Member = member,
                NewEmail = "newEmail@email.com"
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/,
                null /*dataProtectionProvider*/);

            userManagerStub.Setup(um => um.FindByIdAsync(memberId)).ReturnsAsync(user);
            userManagerStub.Setup(um => um.GenerateEmailConfirmationTokenAsync(It.IsAny<Guid>())).ReturnsAsync("emailToken");
            userManagerStub.Setup(um => um.SendNewEmailConfirmationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(0));

            dbStub.Setup(db => db.UserStore).Returns(userStoreStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(memberId);

            Mock<UrlHelper> urlHelperStub = new Mock<UrlHelper>();
            Uri requestUrl = new Uri("http://localhost/");
            Mock<HttpRequestBase> requestStub = new Mock<HttpRequestBase>();
            requestStub.SetupGet(r => r.Url).Returns(requestUrl);
            context.SetupGet(c => c.HttpContext.Request).Returns(requestStub.Object);

            ManageController controller = new ManageController(userManagerStub.Object, null, dbStub.Object, idGetterStub.Object, null)
            {
                ControllerContext = context.Object,
                Url = urlHelperStub.Object
            };

            var result = await controller.UpdateProfile(viewModel) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Index"));
        }

        [Test]
        public async void UpdateProfile_CatchOnSave()
        {
            IndexViewModel viewModel = new IndexViewModel()
            {
                MemberEmail = "person@email.com",
                MemberFirstName = "firstName",
                MemberLastName = "lastName",
                MemberVisibility = WishListVisibility.FriendsOnly,
                ReceivePromotionalEmails = true
            };

            Member member = new Member()
            {
                UserId = memberId,
                WishListVisibility = WishListVisibility.FriendsOnly,
                ReceivePromotionalEmails = true
            };

            User user = new User()
            {
                Id = memberId,
                Email = "person@email.com",
                Member = member
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/,
                null /*dataProtectionProvider*/);

            userManagerStub.Setup(um => um.FindByIdAsync(memberId)).ReturnsAsync(user);
            dbStub.Setup(db => db.UserStore).Returns(userStoreStub.Object);
            dbStub.Setup(db => db.SaveChangesAsync()).Throws<DbUpdateException>();

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(memberId);

            ManageController controller = new ManageController(userManagerStub.Object, null, dbStub.Object, idGetterStub.Object, null)
            {
                ControllerContext = context.Object
            };

            var result = await controller.UpdateProfile(viewModel) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Index"));
        }
    }
}
