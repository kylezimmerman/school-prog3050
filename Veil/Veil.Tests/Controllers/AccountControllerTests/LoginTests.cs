using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataModels;
using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;
using Veil.Models;
using Veil.Services;

namespace Veil.Tests.Controllers.AccountControllerTests
{
    public class LoginTests : AccountControllerTestsBase
    {
        [Test]
        public void Login_GETUnauthenticated_ReturnsLoginViewWithInitializedViewModel()
        {
            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.
                Setup(c => c.HttpContext.User.Identity.IsAuthenticated).
                Returns(false);

            AccountController controller = new AccountController(userManager: null, signInManager: null, stripeService: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = controller.Login(null) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model != null);
            Assert.That(result.Model, Is.InstanceOf<LoginRegisterViewModel>());

            var model = (LoginRegisterViewModel)result.Model;

            Assert.That(model.LoginViewModel != null);
            Assert.That(model.RegisterViewModel != null);
        }

        [Test]
        public void Login_GETAuthenticated_RedirectsToHomeIndex()
        {
            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.
                Setup(c => c.HttpContext.User.Identity.IsAuthenticated).
                Returns(true);

            AccountController controller = new AccountController(userManager: null, signInManager: null, stripeService: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = controller.Login(null) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Index"));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Home"));
        }

        [Test]
        public void Login_GET_SetsReturnUrlInViewBag()
        {
            string returnUrl = "/returnUrl";

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.
                Setup(c => c.HttpContext.User.Identity.IsAuthenticated).
                Returns(false);

            AccountController controller = new AccountController(userManager: null, signInManager: null, stripeService: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = controller.Login(returnUrl) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewBag.ReturnUrl, Is.EqualTo(returnUrl));
        }

        [Test]
        public async void Login_InvalidModelState_RedisplaysLogin()
        {
            LoginViewModel viewModel = new LoginViewModel();

            AccountController controller = new AccountController(userManager: null, signInManager: null, stripeService: null);
            controller.ModelState.AddModelError("Email", "Invalid");

            var result = await controller.Login(viewModel, null) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.Empty);
        }

        [Test]
        public async void Login_InvalidModelState_ReturnsInitializedModel()
        {
            LoginViewModel viewModel = new LoginViewModel
            {
                LoginEmail = "fake@example.com"
            };

            AccountController controller = new AccountController(userManager: null, signInManager: null, stripeService: null);
            controller.ModelState.AddModelError("Email", "Invalid");

            var result = await controller.Login(viewModel, null) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model != null);
            Assert.That(result.Model, Is.InstanceOf<LoginRegisterViewModel>());

            var model = (LoginRegisterViewModel)result.Model;

            Assert.That(model.LoginViewModel, Is.EqualTo(viewModel));
            Assert.That(model.RegisterViewModel != null);
        }

        [Test]
        public async void Login_ValidModel_UsesEmailToFindUser()
        {
            LoginViewModel viewModel = new LoginViewModel
            {
                LoginEmail = "fake@example.com"
            };

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(null /*Return null to reduce executed code path*/).
                Verifiable();

            AccountController controller = new AccountController(userManagerMock.Object, signInManager: null, stripeService: null);

            await controller.Login(viewModel, null);

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.FindByEmailAsync(It.Is<string>(val => val == viewModel.LoginEmail)),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void Login_ValidModelUserNotFound_RedisplaysLoginWithInitializedViewModel()
        {
            LoginViewModel viewModel = new LoginViewModel
            {
                LoginEmail = "fake@example.com"
            };

            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerStub.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(null /*Return null to reduce executed code path*/);

            AccountController controller = new AccountController(userManagerStub.Object, signInManager: null, stripeService: null);

            var result = await controller.Login(viewModel, null) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.Empty);
            Assert.That(result.Model, Is.InstanceOf<LoginRegisterViewModel>());

            var model = (LoginRegisterViewModel)result.Model;

            Assert.That(model.LoginViewModel, Is.EqualTo(viewModel));
            Assert.That(model.RegisterViewModel != null);
        }

        [Test]
        public async void Login_ValidModelAndUserFound_CallsUserManagerCheckPasswordAsync()
        {
            LoginViewModel viewModel = new LoginViewModel
            {
                LoginEmail = "fake@example.com",
                LoginPassword = "password"
            };

            User user = new User
            {
                Id = new Guid("65ED1E57-D246-4A20-9937-E5C129E67064"),
                Email = viewModel.LoginEmail
            };

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(user);
            userManagerMock.
                Setup(um => um.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(false).
                Verifiable();

            AccountController controller = new AccountController(userManagerMock.Object, signInManager: null, stripeService: null);

            await controller.Login(viewModel, null);

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.CheckPasswordAsync(It.Is<User>(val => val == user), It.Is<string>(val => val == viewModel.LoginPassword)),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void Login_IncorrectPassword_CallsUserManagerAccessFailedAsync()
        {
            LoginViewModel viewModel = new LoginViewModel
            {
                LoginEmail = "fake@example.com",
                LoginPassword = "password"
            };

            User user = new User
            {
                Id = new Guid("65ED1E57-D246-4A20-9937-E5C129E67064"),
                Email = viewModel.LoginEmail
            };

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(user);
            userManagerMock.
                Setup(um => um.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(false);
            userManagerMock.
                Setup(um => um.AccessFailedAsync(It.IsAny<Guid>())).
                ReturnsAsync(IdentityResult.Success).
                Verifiable();

            AccountController controller = new AccountController(userManagerMock.Object, signInManager: null, stripeService: null);

            await controller.Login(viewModel, null);

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.AccessFailedAsync(user.Id),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void Login_ValidModelAndUserFoundAndCorrectPassword_CallsUserManagerIsEmailConfirmedAsync()
        {
            LoginViewModel viewModel = new LoginViewModel
            {
                LoginEmail = "fake@example.com",
                LoginPassword = "password"
            };

            User user = new User
            {
                Id = Guid.ParseExact("65ED1E57-D246-4A20-9937-E5C129E67064", "D"),
                Email = viewModel.LoginEmail
            };

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(user);
            userManagerMock.
                Setup(um => um.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(true);
            userManagerMock.
                Setup(um => um.IsEmailConfirmedAsync(It.IsAny<Guid>())).
                ReturnsAsync(false).
                Verifiable();

            AccountController controller = new AccountController(userManagerMock.Object, signInManager: null, stripeService: null);

            await controller.Login(viewModel, null);

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.IsEmailConfirmedAsync(It.Is<Guid>(val => val == user.Id)),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void Login_ValidModelAndUserFoundAndCorrectPasswordButUnverifiedEmail_DisplaysConfirmResendConfirmationEmail()
        {
            LoginViewModel viewModel = new LoginViewModel
            {
                LoginEmail = "fake@example.com",
                LoginPassword = "password"
            };

            User user = new User
            {
                Id = Guid.ParseExact("65ED1E57-D246-4A20-9937-E5C129E67064", "D"),
                Email = viewModel.LoginEmail
            };

            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerStub.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(user);
            userManagerStub.
                Setup(um => um.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(true);
            userManagerStub.
                Setup(um => um.IsEmailConfirmedAsync(It.IsAny<Guid>())).
                ReturnsAsync(false);

            AccountController controller = new AccountController(userManagerStub.Object, signInManager: null, stripeService: null);

            var result = await controller.Login(viewModel, null) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.EqualTo("ConfirmResendConfirmationEmail"));
        }

        [Test]
        public async void Login_SuccessfulSignIn_CallsSignInManagerPasswordSignInAsyncWithUsername()
        {
            LoginViewModel viewModel = new LoginViewModel
            {
                LoginEmail = "fake@example.com",
                LoginPassword = "password",
                RememberMe = true
            };

            User user = new User
            {
                Id = Guid.ParseExact("65ED1E57-D246-4A20-9937-E5C129E67064", "D"),
                Email = viewModel.LoginEmail,
                UserName = "userName"
            };

            Mock<Member> memberStub = new Mock<Member>();
            memberStub.Setup(m => m.Cart.Items.Count).Returns(0);

            user.Member = memberStub.Object;

            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerStub.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(user);
            userManagerStub.
                Setup(um => um.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(true);
            userManagerStub.
                Setup(um => um.IsEmailConfirmedAsync(It.IsAny<Guid>())).
                ReturnsAsync(true);

            Mock<IAuthenticationManager> authenticationManagerStub = new Mock<IAuthenticationManager>();

            Mock<VeilSignInManager> signInManagerMock = new Mock<VeilSignInManager>(userManagerStub.Object, authenticationManagerStub.Object);
            signInManagerMock.
                Setup(sim => sim.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>())).
                ReturnsAsync(SignInStatus.Success).
                Verifiable();

            Mock<UrlHelper> urlHelperStub = new Mock<UrlHelper>();

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupSet(c => c.HttpContext.Session[CartController.CART_QTY_SESSION_KEY] = It.IsAny<int>());

            AccountController controller = new AccountController(userManagerStub.Object, signInManagerMock.Object, stripeService: null)
            {
                Url = urlHelperStub.Object,
                ControllerContext = contextStub.Object
            };

            await controller.Login(viewModel, null);

            Assert.That(
                () =>
                    signInManagerMock.
                        Verify(sim => sim.PasswordSignInAsync(It.Is<string>(val => val == user.UserName),
                                                                It.Is<string>(val => val == viewModel.LoginPassword),
                                                                It.Is<bool>(val => val == viewModel.RememberMe),
                                                                It.IsAny<bool>()),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void Login_SuccessfulSignInWithNonLocalReturnUrl_RedirectsToHomeIndex()
        {
            string returnUrl = "http://google.ca";

            LoginViewModel viewModel = new LoginViewModel
            {
                LoginEmail = "fake@example.com",
                LoginPassword = "password",
                RememberMe = true
            };

            User user = new User
            {
                Id = Guid.ParseExact("65ED1E57-D246-4A20-9937-E5C129E67064", "D"),
                Email = viewModel.LoginEmail,
                UserName = "userName"
            };

            Mock<Member> memberStub = new Mock<Member>();
            memberStub.Setup(m => m.Cart.Items.Count).Returns(0);

            user.Member = memberStub.Object;

            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerStub.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(user);
            userManagerStub.
                Setup(um => um.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(true);
            userManagerStub.
                Setup(um => um.IsEmailConfirmedAsync(It.IsAny<Guid>())).
                ReturnsAsync(true);

            Mock<IAuthenticationManager> authenticationManagerStub = new Mock<IAuthenticationManager>();

            Mock<VeilSignInManager> signInManagerStub = new Mock<VeilSignInManager>(userManagerStub.Object, authenticationManagerStub.Object);
            signInManagerStub.
                Setup(sim => sim.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>())).
                ReturnsAsync(SignInStatus.Success);

            Mock<UrlHelper> urlHelperStub = new Mock<UrlHelper>();
            urlHelperStub.
                Setup(uh => uh.IsLocalUrl(It.Is<string>(val => val == returnUrl))).
                Returns(false);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupSet(c => c.HttpContext.Session[CartController.CART_QTY_SESSION_KEY] = It.IsAny<int>());

            AccountController controller = new AccountController(userManagerStub.Object, signInManagerStub.Object, stripeService: null)
            {
                Url = urlHelperStub.Object,
                ControllerContext = contextStub.Object
            };

            var result = await controller.Login(viewModel, returnUrl) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Index"));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Home"));
        }

        [Test]
        public async void Login_SuccessfulSignInWithLocalReturnUrl_RedirectsToReturnUrl()
        {
            string returnUrl = "/Games";

            LoginViewModel viewModel = new LoginViewModel
            {
                LoginEmail = "fake@example.com",
                LoginPassword = "password",
                RememberMe = true
            };

            User user = new User
            {
                Id = Guid.ParseExact("65ED1E57-D246-4A20-9937-E5C129E67064", "D"),
                Email = viewModel.LoginEmail,
                UserName = "userName"
            };

            Mock<Member> memberStub = new Mock<Member>();
            memberStub.Setup(m => m.Cart.Items.Count).Returns(0);

            user.Member = memberStub.Object;

            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerStub.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(user);
            userManagerStub.
                Setup(um => um.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(true);
            userManagerStub.
                Setup(um => um.IsEmailConfirmedAsync(It.IsAny<Guid>())).
                ReturnsAsync(true);

            Mock<IAuthenticationManager> authenticationManagerStub = new Mock<IAuthenticationManager>();

            Mock<VeilSignInManager> signInManagerStub = new Mock<VeilSignInManager>(userManagerStub.Object, authenticationManagerStub.Object);
            signInManagerStub.
                Setup(sim => sim.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>())).
                ReturnsAsync(SignInStatus.Success);

            Mock<UrlHelper> urlHelperStub = new Mock<UrlHelper>();
            urlHelperStub.
                Setup(uh => uh.IsLocalUrl(It.Is<string>(val => val == returnUrl))).
                Returns(true);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupSet(c => c.HttpContext.Session[CartController.CART_QTY_SESSION_KEY] = It.IsAny<int>());

            AccountController controller = new AccountController(userManagerStub.Object, signInManagerStub.Object, stripeService: null)
            {
                Url = urlHelperStub.Object,
                ControllerContext = contextStub.Object
            };

            var result = await controller.Login(viewModel, returnUrl) as RedirectResult;

            Assert.That(result != null);
            Assert.That(result.Url, Is.EqualTo(returnUrl));
        }

        [Test]
        public async void Login_LockedOutAccount_DisplaysLockoutView()
        {
            LoginViewModel viewModel = new LoginViewModel
            {
                LoginEmail = "fake@example.com",
                LoginPassword = "password",
                RememberMe = true
            };

            User user = new User
            {
                Id = Guid.ParseExact("65ED1E57-D246-4A20-9937-E5C129E67064", "D"),
                Email = viewModel.LoginEmail,
                UserName = "userName"
            };

            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerStub.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(user);
            userManagerStub.
                Setup(um => um.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(true);
            userManagerStub.
                Setup(um => um.IsEmailConfirmedAsync(It.IsAny<Guid>())).
                ReturnsAsync(true);

            Mock<IAuthenticationManager> authenticationManagerStub = new Mock<IAuthenticationManager>();

            Mock<VeilSignInManager> signInManagerStub = new Mock<VeilSignInManager>(userManagerStub.Object, authenticationManagerStub.Object);
            signInManagerStub.
                Setup(sim => sim.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>())).
                ReturnsAsync(SignInStatus.LockedOut);

            AccountController controller = new AccountController(userManagerStub.Object, signInManagerStub.Object, stripeService: null);

            var result = await controller.Login(viewModel, null) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.EqualTo("Lockout"));
        }

        [Test]
        public async void Login_SignInStatusFailure_AddsErrorToModelState()
        {
            LoginViewModel viewModel = new LoginViewModel
            {
                LoginEmail = "fake@example.com",
                LoginPassword = "password",
                RememberMe = true
            };

            User user = new User
            {
                Id = Guid.ParseExact("65ED1E57-D246-4A20-9937-E5C129E67064", "D"),
                Email = viewModel.LoginEmail,
                UserName = "userName"
            };

            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerStub.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(user);
            userManagerStub.
                Setup(um => um.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(true);
            userManagerStub.
                Setup(um => um.IsEmailConfirmedAsync(It.IsAny<Guid>())).
                ReturnsAsync(true);

            Mock<IAuthenticationManager> authenticationManagerStub = new Mock<IAuthenticationManager>();

            Mock<VeilSignInManager> signInManagerStub = new Mock<VeilSignInManager>(userManagerStub.Object, authenticationManagerStub.Object);
            signInManagerStub.
                Setup(sim => sim.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>())).
                ReturnsAsync(SignInStatus.Failure);

            AccountController controller = new AccountController(userManagerStub.Object, signInManagerStub.Object, stripeService: null);

            await controller.Login(viewModel, null);

            Assert.That(controller.ModelState.Count, Is.GreaterThan(0));
        }

        [Test]
        public async void Login_SignInSuccessOfUserWithMemberInMemberRole_DoesNotAddsOrRemoveUserFromMemberRole()
        {
            string returnUrl = "/Games";

            LoginViewModel viewModel = new LoginViewModel
            {
                LoginEmail = "fake@example.com",
                LoginPassword = "password",
                RememberMe = true
            };

            User user = new User
            {
                Id = Guid.ParseExact("65ED1E57-D246-4A20-9937-E5C129E67064", "D"),
                Email = viewModel.LoginEmail,
                UserName = "userName",
                Member = new Member()
            };

            Mock<Member> memberStub = new Mock<Member>();
            memberStub.Setup(m => m.Cart.Items.Count).Returns(0);

            user.Member = memberStub.Object;

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(user);
            userManagerMock.
                Setup(um => um.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(true);
            userManagerMock.
                Setup(um => um.IsEmailConfirmedAsync(It.IsAny<Guid>())).
                ReturnsAsync(true);
            userManagerMock.
                Setup(um => um.IsInRoleAsync(It.Is<Guid>(val => val == user.Id), VeilRoles.MEMBER_ROLE)).
                ReturnsAsync(true);
            userManagerMock.
                Setup(um => um.AddToRoleAsync(It.Is<Guid>(val => val == user.Id), VeilRoles.MEMBER_ROLE)).
                ReturnsAsync(IdentityResult.Success).
                Verifiable();
            userManagerMock.
                Setup(um => um.RemoveFromRoleAsync(user.Id, VeilRoles.MEMBER_ROLE)).
                ReturnsAsync(IdentityResult.Success).
                Verifiable();

            Mock<IAuthenticationManager> authenticationManagerStub = new Mock<IAuthenticationManager>();

            Mock<VeilSignInManager> signInManagerStub = new Mock<VeilSignInManager>(userManagerMock.Object, authenticationManagerStub.Object);
            signInManagerStub.
                Setup(sim => sim.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>())).
                ReturnsAsync(SignInStatus.Success);

            Mock<UrlHelper> urlHelperStub = new Mock<UrlHelper>();

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupSet(c => c.HttpContext.Session[CartController.CART_QTY_SESSION_KEY] = It.IsAny<int>());

            AccountController controller = new AccountController(userManagerMock.Object, signInManagerStub.Object, stripeService: null)
            {
                Url = urlHelperStub.Object,
                ControllerContext = contextStub.Object
            };

            await controller.Login(viewModel, returnUrl);

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.AddToRoleAsync(user.Id, VeilRoles.MEMBER_ROLE),
                    Times.Never),
                Throws.Nothing);

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.RemoveFromRoleAsync(user.Id, VeilRoles.MEMBER_ROLE),
                    Times.Never),
                Throws.Nothing);
        }

        [Test]
        public async void Login_SignInSuccessOfUserWithEmployeeInEmployeeRole_DoesNotAddsOrRemoveUserFromEmployeeRole()
        {
            string returnUrl = "/Games";

            LoginViewModel viewModel = new LoginViewModel
            {
                LoginEmail = "fake@example.com",
                LoginPassword = "password",
                RememberMe = true
            };

            User user = new User
            {
                Id = Guid.ParseExact("65ED1E57-D246-4A20-9937-E5C129E67064", "D"),
                Email = viewModel.LoginEmail,
                UserName = "userName",
                Employee = new Employee()
            };

            Mock<Member> memberStub = new Mock<Member>();
            memberStub.Setup(m => m.Cart.Items.Count).Returns(0);

            user.Member = memberStub.Object;

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(user);
            userManagerMock.
                Setup(um => um.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(true);
            userManagerMock.
                Setup(um => um.IsEmailConfirmedAsync(It.IsAny<Guid>())).
                ReturnsAsync(true);
            userManagerMock.
                Setup(um => um.IsInRoleAsync(user.Id, VeilRoles.EMPLOYEE_ROLE)).
                ReturnsAsync(true);
            userManagerMock.
                Setup(um => um.AddToRoleAsync(user.Id, VeilRoles.EMPLOYEE_ROLE)).
                ReturnsAsync(IdentityResult.Success).
                Verifiable();
            userManagerMock.
                Setup(um => um.RemoveFromRoleAsync(user.Id, VeilRoles.EMPLOYEE_ROLE)).
                ReturnsAsync(IdentityResult.Success).
                Verifiable();

            Mock<IAuthenticationManager> authenticationManagerStub = new Mock<IAuthenticationManager>();

            Mock<VeilSignInManager> signInManagerStub = new Mock<VeilSignInManager>(userManagerMock.Object, authenticationManagerStub.Object);
            signInManagerStub.
                Setup(sim => sim.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>())).
                ReturnsAsync(SignInStatus.Success);

            Mock<UrlHelper> urlHelperStub = new Mock<UrlHelper>();

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupSet(c => c.HttpContext.Session[CartController.CART_QTY_SESSION_KEY] = It.IsAny<int>());

            AccountController controller = new AccountController(userManagerMock.Object, signInManagerStub.Object, stripeService: null)
            {
                Url = urlHelperStub.Object,
                ControllerContext = contextStub.Object
            };

            await controller.Login(viewModel, returnUrl);

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.AddToRoleAsync(user.Id, VeilRoles.EMPLOYEE_ROLE),
                    Times.Never),
                Throws.Nothing);

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.RemoveFromRoleAsync(user.Id, VeilRoles.EMPLOYEE_ROLE),
                    Times.Never),
                Throws.Nothing);
        }

        [Test]
        public async void Login_SignInSuccessOfUserWithMemberNotInMemberRole_AddsUserToMemberRole()
        {
            string returnUrl = "/Games";

            LoginViewModel viewModel = new LoginViewModel
            {
                LoginEmail = "fake@example.com",
                LoginPassword = "password",
                RememberMe = true
            };

            User user = new User
            {
                Id = Guid.ParseExact("65ED1E57-D246-4A20-9937-E5C129E67064", "D"),
                Email = viewModel.LoginEmail,
                UserName = "userName",
                Member = new Member()
            };

            Mock<Member> memberStub = new Mock<Member>();
            memberStub.Setup(m => m.Cart.Items.Count).Returns(0);

            user.Member = memberStub.Object;

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(user);
            userManagerMock.
                Setup(um => um.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(true);
            userManagerMock.
                Setup(um => um.IsEmailConfirmedAsync(It.IsAny<Guid>())).
                ReturnsAsync(true);
            userManagerMock.
                Setup(um => um.IsInRoleAsync(It.Is<Guid>(val => val == user.Id), VeilRoles.MEMBER_ROLE)).
                ReturnsAsync(false);
            userManagerMock.
                Setup(um => um.AddToRoleAsync(It.Is<Guid>(val => val == user.Id), VeilRoles.MEMBER_ROLE)).
                ReturnsAsync(IdentityResult.Success).
                Verifiable();

            Mock<IAuthenticationManager> authenticationManagerStub = new Mock<IAuthenticationManager>();

            Mock<VeilSignInManager> signInManagerStub = new Mock<VeilSignInManager>(userManagerMock.Object, authenticationManagerStub.Object);
            signInManagerStub.
                Setup(sim => sim.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>())).
                ReturnsAsync(SignInStatus.Success);

            Mock<UrlHelper> urlHelperStub = new Mock<UrlHelper>();

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupSet(c => c.HttpContext.Session[CartController.CART_QTY_SESSION_KEY] = It.IsAny<int>());

            AccountController controller = new AccountController(userManagerMock.Object, signInManagerStub.Object, stripeService: null)
            {
                Url = urlHelperStub.Object,
                ControllerContext = contextStub.Object
            };

            await controller.Login(viewModel, returnUrl);

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.AddToRoleAsync(user.Id, VeilRoles.MEMBER_ROLE),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void Login_SignInSuccessOfUserWithEmployeeNotInEmployeeRole_AddsUserToEmployeeRole()
        {
            string returnUrl = "/Games";

            LoginViewModel viewModel = new LoginViewModel
            {
                LoginEmail = "fake@example.com",
                LoginPassword = "password",
                RememberMe = true
            };

            User user = new User
            {
                Id = Guid.ParseExact("65ED1E57-D246-4A20-9937-E5C129E67064", "D"),
                Email = viewModel.LoginEmail,
                UserName = "userName",
                Employee = new Employee()
            };

            Mock<Member> memberStub = new Mock<Member>();
            memberStub.Setup(m => m.Cart.Items.Count).Returns(0);

            user.Member = memberStub.Object;

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(user);
            userManagerMock.
                Setup(um => um.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(true);
            userManagerMock.
                Setup(um => um.IsEmailConfirmedAsync(It.IsAny<Guid>())).
                ReturnsAsync(true);
            userManagerMock.
                Setup(um => um.IsInRoleAsync(user.Id, VeilRoles.EMPLOYEE_ROLE)).
                ReturnsAsync(false);
            userManagerMock.
                Setup(um => um.AddToRoleAsync(user.Id, VeilRoles.EMPLOYEE_ROLE)).
                ReturnsAsync(IdentityResult.Success).
                Verifiable();

            Mock<IAuthenticationManager> authenticationManagerStub = new Mock<IAuthenticationManager>();

            Mock<VeilSignInManager> signInManagerStub = new Mock<VeilSignInManager>(userManagerMock.Object, authenticationManagerStub.Object);
            signInManagerStub.
                Setup(sim => sim.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>())).
                ReturnsAsync(SignInStatus.Success);

            Mock<UrlHelper> urlHelperStub = new Mock<UrlHelper>();

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupSet(c => c.HttpContext.Session[CartController.CART_QTY_SESSION_KEY] = It.IsAny<int>());

            AccountController controller = new AccountController(userManagerMock.Object, signInManagerStub.Object, stripeService: null)
            {
                Url = urlHelperStub.Object,
                ControllerContext = contextStub.Object
            };

            await controller.Login(viewModel, returnUrl);

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.AddToRoleAsync(user.Id, VeilRoles.EMPLOYEE_ROLE),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void Login_SignInSuccessOfUserWithoutMemberInMemberRole_RemovesUserFromMemberRole()
        {
            string returnUrl = "/Games";

            LoginViewModel viewModel = new LoginViewModel
            {
                LoginEmail = "fake@example.com",
                LoginPassword = "password",
                RememberMe = true
            };

            User user = new User
            {
                Id = Guid.ParseExact("65ED1E57-D246-4A20-9937-E5C129E67064", "D"),
                Email = viewModel.LoginEmail,
                UserName = "userName"
            };

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(user);
            userManagerMock.
                Setup(um => um.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(true);
            userManagerMock.
                Setup(um => um.IsEmailConfirmedAsync(It.IsAny<Guid>())).
                ReturnsAsync(true);
            userManagerMock.
                SetupSequence(um => um.IsInRoleAsync(user.Id, VeilRoles.MEMBER_ROLE)).
                Returns(Task.FromResult(true)).
                Returns(Task.FromResult(false));
            userManagerMock.
                Setup(um => um.RemoveFromRoleAsync(user.Id, VeilRoles.MEMBER_ROLE)).
                ReturnsAsync(IdentityResult.Success).
                Verifiable();

            Mock<IAuthenticationManager> authenticationManagerStub = new Mock<IAuthenticationManager>();

            Mock<VeilSignInManager> signInManagerStub = new Mock<VeilSignInManager>(userManagerMock.Object, authenticationManagerStub.Object);
            signInManagerStub.
                Setup(sim => sim.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>())).
                ReturnsAsync(SignInStatus.Success);

            Mock<UrlHelper> urlHelperStub = new Mock<UrlHelper>();

            AccountController controller = new AccountController(userManagerMock.Object, signInManagerStub.Object, stripeService: null)
            {
                Url = urlHelperStub.Object
            };

            await controller.Login(viewModel, returnUrl);

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.RemoveFromRoleAsync(user.Id, VeilRoles.MEMBER_ROLE),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void Login_SignInSuccessOfUserWithoutEmployeeInEmployeeRole_RemovesUserFromEmployeeRole()
        {
            string returnUrl = "/Games";

            LoginViewModel viewModel = new LoginViewModel
            {
                LoginEmail = "fake@example.com",
                LoginPassword = "password",
                RememberMe = true
            };

            User user = new User
            {
                Id = Guid.ParseExact("65ED1E57-D246-4A20-9937-E5C129E67064", "D"),
                Email = viewModel.LoginEmail,
                UserName = "userName"
            };

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(user);
            userManagerMock.
                Setup(um => um.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(true);
            userManagerMock.
                Setup(um => um.IsEmailConfirmedAsync(It.IsAny<Guid>())).
                ReturnsAsync(true);
            userManagerMock.
                Setup(um => um.IsInRoleAsync(user.Id, VeilRoles.EMPLOYEE_ROLE)).
                ReturnsAsync(true);
            userManagerMock.
                Setup(um => um.RemoveFromRoleAsync(user.Id, VeilRoles.EMPLOYEE_ROLE)).
                ReturnsAsync(IdentityResult.Success).
                Verifiable();

            Mock<IAuthenticationManager> authenticationManagerStub = new Mock<IAuthenticationManager>();

            Mock<VeilSignInManager> signInManagerStub = new Mock<VeilSignInManager>(userManagerMock.Object, authenticationManagerStub.Object);
            signInManagerStub.
                Setup(sim => sim.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>())).
                ReturnsAsync(SignInStatus.Success);

            Mock<UrlHelper> urlHelperStub = new Mock<UrlHelper>();

            AccountController controller = new AccountController(userManagerMock.Object, signInManagerStub.Object, stripeService: null)
            {
                Url = urlHelperStub.Object
            };

            await controller.Login(viewModel, returnUrl);

            Assert.That(
                () =>
                    userManagerMock.Verify(um => um.RemoveFromRoleAsync(user.Id, VeilRoles.EMPLOYEE_ROLE),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void Login_SignInSuccessOfUserWithMember_AddsCartCountToSession()
        {
            int cartCount = 10;

            LoginViewModel viewModel = new LoginViewModel
            {
                LoginEmail = "fake@example.com",
                LoginPassword = "password",
                RememberMe = true
            };

            User user = new User
            {
                Id = Guid.ParseExact("65ED1E57-D246-4A20-9937-E5C129E67064", "D"),
                Email = viewModel.LoginEmail,
                UserName = "userName",
                Member = new Member()
            };

            Mock<Member> memberStub = new Mock<Member>();
            memberStub.Setup(m => m.Cart.Items.Count).Returns(cartCount);

            user.Member = memberStub.Object;

            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerStub.
                Setup(um => um.FindByEmailAsync(It.IsAny<string>())).
                ReturnsAsync(user);
            userManagerStub.
                Setup(um => um.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(true);
            userManagerStub.
                Setup(um => um.IsEmailConfirmedAsync(It.IsAny<Guid>())).
                ReturnsAsync(true);
            userManagerStub.
                Setup(um => um.IsInRoleAsync(It.Is<Guid>(val => val == user.Id), VeilRoles.MEMBER_ROLE)).
                ReturnsAsync(true);

            Mock<IAuthenticationManager> authenticationManagerStub = new Mock<IAuthenticationManager>();

            Mock<VeilSignInManager> signInManagerStub = new Mock<VeilSignInManager>(userManagerStub.Object, authenticationManagerStub.Object);
            signInManagerStub.
                Setup(sim => sim.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>())).
                ReturnsAsync(SignInStatus.Success);

            Mock<UrlHelper> urlHelperStub = new Mock<UrlHelper>();

            Mock<ControllerContext> contextMock = new Mock<ControllerContext>();
            contextMock.
                SetupSet(c => c.HttpContext.Session[CartController.CART_QTY_SESSION_KEY] = It.IsAny<int>()).
                Verifiable();

            AccountController controller = new AccountController(userManagerStub.Object, signInManagerStub.Object, stripeService: null)
            {
                Url = urlHelperStub.Object,
                ControllerContext = contextMock.Object
            };

            await controller.Login(viewModel, returnUrl: null);

            Assert.That(
                () =>
                    contextMock.VerifySet(c => c.HttpContext.Session[CartController.CART_QTY_SESSION_KEY] = cartCount,
                    Times.Once),
                Throws.Nothing);
        }
    }
}
