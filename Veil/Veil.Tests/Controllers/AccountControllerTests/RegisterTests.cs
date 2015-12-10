using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataModels;
using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;
using Veil.Exceptions;
using Veil.Models;
using Veil.Services;
using Veil.Services.Interfaces;

namespace Veil.Tests.Controllers.AccountControllerTests
{
    public class RegisterTests : AccountControllerTestsBase
    {
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
        public void Register_IStripeServiceThrowing_HandlesStripeServiceException()
        {
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerStub.
                Setup(um => um.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(IdentityResult.Success);

            stripeServiceStub.
                Setup(ss => ss.CreateCustomer(It.IsAny<User>())).
                Throws(new StripeServiceException("message", StripeExceptionType.UnknownError));

            RegisterViewModel viewModel = new RegisterViewModel();
            AccountController controller = new AccountController(userManagerStub.Object, null /*signInManager*/, stripeServiceStub.Object);

            Assert.That(async () => await controller.Register(viewModel, null), Throws.Nothing);
        }

        [Test]
        public void Register_IStripeServiceThrowing_HandlesApiKeyException()
        {
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerStub.
                Setup(um => um.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(IdentityResult.Success);

            stripeServiceStub.
                Setup(ss => ss.CreateCustomer(It.IsAny<User>())).
                Throws(new StripeServiceException("message", StripeExceptionType.ApiKeyError));

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

            var model = (LoginRegisterViewModel)result.Model;

            Assert.That(model.RegisterViewModel != null);
            Assert.That(model.RegisterViewModel, Is.EqualTo(viewModel));
            Assert.That(model.LoginViewModel != null);
        }

        [Test]
        public async void Register_IStripeServiceThrowingNonApiKeyError_ReturnsLoginViewWithInitializedViewModel(
            [Values(StripeExceptionType.UnknownError, StripeExceptionType.ServiceError)]StripeExceptionType exceptionType)
        {
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
            userManagerStub.
                Setup(um => um.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).
                ReturnsAsync(IdentityResult.Success);

            stripeServiceStub.
                Setup(ss => ss.CreateCustomer(It.IsAny<User>())).
                Throws(new StripeServiceException("message", exceptionType));

            RegisterViewModel viewModel = new RegisterViewModel();
            AccountController controller = new AccountController(userManagerStub.Object, null /*signInManager*/, stripeServiceStub.Object);

            var result = await controller.Register(viewModel, null) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<LoginRegisterViewModel>());

            var model = (LoginRegisterViewModel)result.Model;

            Assert.That(model.RegisterViewModel != null);
            Assert.That(model.RegisterViewModel, Is.EqualTo(viewModel));
            Assert.That(model.LoginViewModel, Is.Not.Null);
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

            var model = (LoginRegisterViewModel)result.Model;

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
    }
}
