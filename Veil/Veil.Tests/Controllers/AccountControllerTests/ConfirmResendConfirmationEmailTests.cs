using System.Web.Mvc;
using NUnit.Framework;
using Veil.Controllers;

namespace Veil.Tests.Controllers.AccountControllerTests
{
    public class ConfirmResendConfirmationEmailTests : AccountControllerTestsBase
    {
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
    }
}
