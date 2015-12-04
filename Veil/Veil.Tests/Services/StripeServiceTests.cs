using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Veil.Tests.Services
{
    class StripeServiceTests
    {
        //[Test]
        //public void Register_IStripeServiceThrowing_Handles500LevelException()
        //{
        //    Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
        //    userManagerStub.
        //        Setup(um => um.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).
        //        ReturnsAsync(IdentityResult.Success);

        //    stripeServiceStub.
        //        Setup(ss => ss.CreateCustomer(It.IsAny<User>())).
        //        Throws(new StripeException(HttpStatusCode.InternalServerError, new StripeError(), "message"));

        //    RegisterViewModel viewModel = new RegisterViewModel();
        //    AccountController controller = new AccountController(userManagerStub.Object, null /*signInManager*/, stripeServiceStub.Object);

        //    Assert.That(async () => await controller.Register(viewModel, null), Throws.Nothing);
        //}

        //[Test]
        //public void Register_IStripeServiceThrowing_HandlesUnauthorizedException()
        //{
        //    Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
        //    userManagerStub.
        //        Setup(um => um.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).
        //        ReturnsAsync(IdentityResult.Success);

        //    stripeServiceStub.
        //        Setup(ss => ss.CreateCustomer(It.IsAny<User>())).
        //        Throws(new StripeException(HttpStatusCode.Unauthorized, new StripeError(), "message"));

        //    RegisterViewModel viewModel = new RegisterViewModel();

        //    AccountController controller = new AccountController(userManagerStub.Object, null /*signInManager*/, stripeServiceStub.Object);

        //    Assert.That(async () => await controller.Register(viewModel, null), Throws.Nothing);
        //}

        //[Test]
        //public void Register_IStripeServiceThrowing_HandlesUnknownException()
        //{
        //    Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/, null /*dataProtectionProvider*/);
        //    userManagerStub.
        //        Setup(um => um.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).
        //        ReturnsAsync(IdentityResult.Success);

        //    stripeServiceStub.
        //        Setup(ss => ss.CreateCustomer(It.IsAny<User>())).
        //        Throws(new StripeException(HttpStatusCode.BadRequest, new StripeError(), "message"));

        //    RegisterViewModel viewModel = new RegisterViewModel();

        //    AccountController controller = new AccountController(userManagerStub.Object, null /*signInManager*/, stripeServiceStub.Object);

        //    Assert.That(async () => await controller.Register(viewModel, null), Throws.Nothing);
        //}

    }
}
