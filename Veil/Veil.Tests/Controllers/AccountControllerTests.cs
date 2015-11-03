using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Moq;
using NUnit.Framework;
using Stripe;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels;
using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;
using Veil.Models;
using Veil.Services;
using Veil.Services.Interfaces;

namespace Veil.Tests.Controllers
{
    [TestFixture]
    public class AccountControllerTests
    {
        // Identity Stubs
        private Mock<IUserStore<User, Guid>> userStoreStub;
        private Mock<IStripeService> stripeServiceStub; 

        // Db Stub
        private Mock<IVeilDataAccess> dbStub;

        [SetUp]
        public void Setup()
        {
            userStoreStub = new Mock<IUserStore<User, Guid>>();
            stripeServiceStub = new Mock<IStripeService>();

            dbStub = new Mock<IVeilDataAccess>();
            dbStub.Setup(db => db.UserStore).Returns(userStoreStub.Object);
        }

        #region Login Tests
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

            var model = (LoginRegisterViewModel) result.Model;

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

            var model = (LoginRegisterViewModel) result.Model;

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

            var model = (LoginRegisterViewModel) result.Model;

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
                Id = Guid.ParseExact("65ED1E57-D246-4A20-9937-E5C129E67064", "D"),
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

            AccountController controller = new AccountController(userManagerStub.Object, signInManagerMock.Object, stripeService: null)
            {
                Url = urlHelperStub.Object
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

            AccountController controller = new AccountController(userManagerStub.Object, signInManagerStub.Object, stripeService: null)
            {
                Url = urlHelperStub.Object
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

            AccountController controller = new AccountController(userManagerStub.Object, signInManagerStub.Object, stripeService: null)
            {
                Url = urlHelperStub.Object
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
        public async void Login_RequiresTwoFactorAuth_RedirectsToSendCodeWithReturnUrlAndRememberMe()
        {
            string returnUrl = "/returnUrl";

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
                ReturnsAsync(SignInStatus.RequiresVerification);

            AccountController controller = new AccountController(userManagerStub.Object, signInManagerStub.Object, stripeService: null);

            var result = await controller.Login(viewModel, returnUrl) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo("SendCode"));
            Assert.That(result.RouteValues["ReturnUrl"], Is.EqualTo(returnUrl));
            Assert.That(result.RouteValues["RememberMe"], Is.EqualTo(viewModel.RememberMe));
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

            AccountController controller = new AccountController(userManagerMock.Object, signInManagerStub.Object, stripeService: null)
            {
                Url = urlHelperStub.Object
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

            AccountController controller = new AccountController(userManagerMock.Object, signInManagerStub.Object, stripeService: null)
            {
                Url = urlHelperStub.Object
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
                Setup(um => um.IsInRoleAsync(user.Id, VeilRoles.MEMBER_ROLE)).
                ReturnsAsync(true);
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

        #endregion Login Tests

        #region Register Tests
        [Test]
        public async void Register_ValidModel_CallsUserManagerCreateAsync()
        {
            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(IdentityResult.Failed() /* Return a failed result to minimize the code executed */).
                Verifiable();

            RegisterViewModel viewModel = new RegisterViewModel();

            AccountController controller = new AccountController(userManagerMock.Object, null /*signInManager*/, stripeServiceStub.Object);

            await controller.Register(viewModel, null);

            Assert.That(
                () => 
                    userManagerMock.Verify(um => um.CreateAsync(It.IsAny<User>(), It.IsAny<string>()),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void Register_ValidModelAndUserCreated_CallsIStripeServiceCreateCustomer()
        {
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerStub.
                Setup(um => um.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(IdentityResult.Success);
            userManagerStub.
                Setup(um => um.UpdateAsync(It.IsAny<User>())).
                ReturnsAsync(IdentityResult.Failed() /* Return a failed result to minimize the code executed */);

            Mock<IStripeService> stripeServiceMock = stripeServiceStub;
            stripeServiceMock.
                Setup(ss => ss.CreateCustomer(It.IsAny<User>())).
                Returns("customerString").
                Verifiable();

            RegisterViewModel viewModel = new RegisterViewModel();

            AccountController controller = new AccountController(userManagerStub.Object, null /*signInManager*/, stripeServiceMock.Object);

            await controller.Register(viewModel, null);

            Assert.That(
                () => 
                    stripeServiceMock.Verify(ss => ss.CreateCustomer(It.IsAny<User>()), 
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void Register_ValidModelAndUserCreatedAndStripeCustomerCreated_UpdatesUserWithMemberEntryWithCorrectValues()
        {
            string stripeCustomerId = "customerIdString";
            WishListVisibility wishListVisibility = WishListVisibility.Private;
            bool receivePromotionalEmail = true;

            // Auth Mocks
            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(IdentityResult.Success);
            userManagerMock.
                Setup(um => um.UpdateAsync(It.IsAny<User>())).
                ReturnsAsync(IdentityResult.Failed() /* Return a failed result to minimize the code executed */).
                Verifiable();

            stripeServiceStub.
                Setup(ss => ss.CreateCustomer(It.IsAny<User>())).
                Returns(stripeCustomerId);

            RegisterViewModel viewModel = new RegisterViewModel
            {
                ReceivePromotionalEmail = receivePromotionalEmail,
                WishListVisibility = wishListVisibility
            };

            AccountController controller = new AccountController(userManagerMock.Object, null /*signInManager*/, stripeServiceStub.Object);

            await controller.Register(viewModel, null);

            Assert.That(
                () => 
                    userManagerMock.Verify(
                        um => um.UpdateAsync(It.Is<User>(u => u.Member != null)),
                        Times.Exactly(1)),
                Throws.Nothing);

            Assert.That(
                () =>
                    userManagerMock.Verify(
                        um => um.UpdateAsync(It.Is<User>(u => u.Member.ReceivePromotionalEmails == receivePromotionalEmail)),
                        Times.Exactly(1)),
                Throws.Nothing);

            Assert.That(
                () =>
                    userManagerMock.Verify(
                        um => um.UpdateAsync(It.Is<User>(u => u.Member.WishListVisibility == wishListVisibility)),
                        Times.Exactly(1)),
                Throws.Nothing);

            Assert.That(
                () =>
                    userManagerMock.Verify(
                        um => um.UpdateAsync(It.Is<User>(u => u.Member.StripeCustomerId == stripeCustomerId)),
                        Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void Register_ValidModelAndNoErrors_AddsUserToMemberRole()
        {
            string memberRole = VeilRoles.MEMBER_ROLE;

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(IdentityResult.Success);
            userManagerMock.
                Setup(um => um.UpdateAsync(It.IsAny<User>())).
                ReturnsAsync(IdentityResult.Success);
            userManagerMock.
                Setup(um => um.AddToRoleAsync(It.IsAny<Guid>(), It.IsAny<string>())).
                ReturnsAsync(IdentityResult.Failed() /* Return a failed result to shorten the code path */).
                Verifiable();

            stripeServiceStub.
                Setup(ss => ss.CreateCustomer(It.IsAny<User>())).
                Returns("customerIdString");

            RegisterViewModel viewModel = new RegisterViewModel();

            AccountController controller = new AccountController(userManagerMock.Object, null /*signInManager*/, stripeServiceStub.Object);

            await controller.Register(viewModel, null);

            Assert.That(
                () =>
                    userManagerMock.Verify(
                        um => um.AddToRoleAsync(It.IsAny<Guid>(), It.Is<string>(s => s == memberRole)),
                        Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void Register_ValidModelAndNoErrors_CallsSendEmailAsync()
        {
            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(IdentityResult.Success);
            userManagerMock.
                Setup(um => um.UpdateAsync(It.IsAny<User>())).
                ReturnsAsync(IdentityResult.Success);
            userManagerMock.
                Setup(um => um.AddToRoleAsync(It.IsAny<Guid>(), It.IsAny<string>())).
                ReturnsAsync(IdentityResult.Success);
            userManagerMock.
                Setup(um => um.GenerateEmailConfirmationTokenAsync(It.IsAny<Guid>())).
                ReturnsAsync("token");
            userManagerMock.
                Setup(um => um.SendEmailAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>())).
                Returns(Task.FromResult(0)).
                Verifiable();

            stripeServiceStub.
                Setup(ss => ss.CreateCustomer(It.IsAny<User>())).
                Returns("customerIdString");

            // Controller setup stubs
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

            RegisterViewModel viewModel = new RegisterViewModel();

            AccountController controller = new AccountController(userManagerMock.Object, null /*signInManager*/, stripeServiceStub.Object)
            {
                Url = urlHelperStub.Object,
                ControllerContext = contextStub.Object
            };

            await controller.Register(viewModel, null);

            Assert.That(
                () =>
                    userManagerMock.Verify(
                        um => um.SendEmailAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void Register_InvalidModel_RedisplaysLogin()
        {
            RegisterViewModel viewModel = new RegisterViewModel();
            AccountController controller = new AccountController(null /*userManager*/, null /*signInManager*/, null /*stripeService*/);
            controller.ModelState.AddModelError(nameof(viewModel.Email), "Error");

            var result = await controller.Register(viewModel, null) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewData.ModelState.IsValid, Is.False);
            Assert.That(result.ViewName, Is.EqualTo("Login"));
        }

        [Test]
        public async void Register_InvalidModel_SetsReturnUrlInViewBag()
        {
            string returnUrl = "/returnUrl";
            AccountController controller = new AccountController(null /*userManager*/, null /*signInManager*/, null /*stripeService*/);
            controller.ModelState.AddModelError(nameof(RegisterViewModel.Email), "Error");

            var result = await controller.Register(null, returnUrl) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewData.ModelState.IsValid, Is.False);
            Assert.That(result.ViewBag.ReturnUrl, Is.EqualTo(returnUrl));
        }

        [Test]
        public void Register_IStripeServiceThrowing_Handles500LevelException()
        {
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerStub.
                Setup(um => um.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(IdentityResult.Success);

            stripeServiceStub.
                Setup(ss => ss.CreateCustomer(It.IsAny<User>())).
                Throws(new StripeException(HttpStatusCode.InternalServerError, new StripeError(), "message"));

            RegisterViewModel viewModel = new RegisterViewModel();
            AccountController controller = new AccountController(userManagerStub.Object, null /*signInManager*/, stripeServiceStub.Object);

            Assert.That(async () => await controller.Register(viewModel, null), Throws.Nothing);
        }

        [Test]
        public void Register_IStripeServiceThrowing_HandlesUnauthorizedException()
        {
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerStub.
                Setup(um => um.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(IdentityResult.Success);

            stripeServiceStub.
                Setup(ss => ss.CreateCustomer(It.IsAny<User>())).
                Throws(new StripeException(HttpStatusCode.Unauthorized, new StripeError(), "message"));

            RegisterViewModel viewModel = new RegisterViewModel();

            AccountController controller = new AccountController(userManagerStub.Object, null /*signInManager*/, stripeServiceStub.Object);

            Assert.That(async () => await controller.Register(viewModel, null), Throws.Nothing);
        }

        [Test]
        public void Register_IStripeServiceThrowing_HandlesUnknownException()
        {
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerStub.
                Setup(um => um.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(IdentityResult.Success);

            stripeServiceStub.
                Setup(ss => ss.CreateCustomer(It.IsAny<User>())).
                Throws(new StripeException(HttpStatusCode.BadRequest, new StripeError(), "message"));

            RegisterViewModel viewModel = new RegisterViewModel();

            AccountController controller = new AccountController(userManagerStub.Object, null /*signInManager*/, stripeServiceStub.Object);

            Assert.That(async () => await controller.Register(viewModel, null), Throws.Nothing);
        }

        [Test]
        public async void Register_InvalidModel_ReturnsInitializedViewModel()
        {
            AccountController controller = new AccountController(null /*userManager*/, null /*signInManager*/, null /*stripeService*/);
            controller.ModelState.AddModelError(nameof(RegisterViewModel.Email), "Error");
            RegisterViewModel viewModel = new RegisterViewModel();

            var result = await controller.Register(viewModel, null) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewData.ModelState.IsValid, Is.False);
            Assert.That(result.Model, Is.InstanceOf<LoginRegisterViewModel>());

            var model = (LoginRegisterViewModel) result.Model;

            Assert.That(model.RegisterViewModel != null);
            Assert.That(model.RegisterViewModel, Is.EqualTo(viewModel));
            Assert.That(model.LoginViewModel != null);
        }

        [Test]
        public async void Register_StripeThrowingError_ReturnsLoginViewWithInitializedViewModel()
        {
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerStub.
                Setup(um => um.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(IdentityResult.Success);

            stripeServiceStub.
                Setup(ss => ss.CreateCustomer(It.IsAny<User>())).
                Throws(new StripeException(HttpStatusCode.InternalServerError, new StripeError(), "message"));

            RegisterViewModel viewModel = new RegisterViewModel();
            AccountController controller = new AccountController(userManagerStub.Object, null /*signInManager*/, stripeServiceStub.Object);

            var result = await controller.Register(viewModel, null) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<LoginRegisterViewModel>());

            var model = (LoginRegisterViewModel) result.Model;

            Assert.That(model.RegisterViewModel != null);
            Assert.That(model.RegisterViewModel, Is.EqualTo(viewModel));
            Assert.That(model.LoginViewModel != null);
        }

        [Test]
        public void Register_UnauthenticatedGET_ReturnsLoginViewWithInitializedViewModel()
        {
            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.
                Setup(c => c.HttpContext.User.Identity.IsAuthenticated).
                Returns(false);

            AccountController controller = new AccountController(null /*userManager*/, null /*signInManager*/, null /*stripeService*/)
            {
                ControllerContext = contextStub.Object
            };

            var result = controller.Register(null) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<LoginRegisterViewModel>());

            var model = (LoginRegisterViewModel) result.Model;

            Assert.That(model.LoginViewModel != null);
            Assert.That(model.RegisterViewModel != null);
        }

        [Test]
        public void Register_UnauthenticatedGET_SetsReturnUrlInViewBag()
        {
            string returnUrl = "/returnUrl";

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.
                Setup(c => c.HttpContext.User.Identity.IsAuthenticated).
                Returns(false);

            AccountController controller = new AccountController(null /*userManager*/, null /*signInManager*/, null /*stripeService*/)
            {
                ControllerContext = contextStub.Object
            };

            var result = controller.Register(returnUrl) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewBag.ReturnUrl, Is.EqualTo(returnUrl));
        }

        [Test]
        public void Register_AuthenticatedGET_RedirectsToHomeIndex()
        {
            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.
                Setup(c => c.HttpContext.User.Identity.IsAuthenticated).
                Returns(true);

            AccountController controller = new AccountController(null /*userManager*/, null /*signInManager*/, null /*stripeService*/)
            {
                ControllerContext = contextStub.Object
            };

            var result = controller.Register(null) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Index"));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Home"));
        }

        [Test]
        public async void Register_FailedIdentityResult_AddsAllErrorsToModelState()
        {
            string[] identityErrors =
            {
                "Error 1",
                "Error 2",
                "Error 3"
            };
            IdentityResult failedResult = IdentityResult.Failed(identityErrors);

            Mock<VeilUserManager> userManagerMock = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerMock.
                Setup(um => um.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(failedResult);

            RegisterViewModel viewModel = new RegisterViewModel();

            AccountController controller = new AccountController(userManagerMock.Object, null /*signInManager*/, null /*stripeService*/);

            await controller.Register(viewModel, null);

            Assert.That(controller.ModelState.Count, Is.EqualTo(1));
            Assert.That(controller.ModelState.First().Value.Errors.Count, Is.EqualTo(identityErrors.Length));
        }
    #endregion Register Tests

        #region ConfirmEmail Tests
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
        #endregion ConfirmEmail

        #region ConfirmResendConfirmationEmail Tests
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void ConfirmResendConfirmationEmail_InvalidEmails_ReturnsErrorView(string emailAddress)
        {
            AccountController controller = new AccountController(userManager: null, signInManager: null, stripeService: null);

            var result = controller.ConfirmResendConfirmationEmail(emailAddress) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.EqualTo("Error"));
        }

        [Test]
        public void ConfirmResendConfirmationEmail_ValidEmail_ReturnsViewWithEmailAddModel()
        {
            string emailAddress = "fake@example.com";

            AccountController controller = new AccountController(userManager: null, signInManager: null, stripeService: null);

            var result = controller.ConfirmResendConfirmationEmail(emailAddress) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.EqualTo(emailAddress));
            Assert.That(result.ViewName, Is.EqualTo("ConfirmResendConfirmationEmail"));
        }
        #endregion ConfirmResendConfirmationEmail Tests

        #region ResendConfirmationEmail Tests
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
    #endregion ResendConfirmationEmail Tests
    }
}
