using System.Web.Mvc;
using NUnit.Framework;
using Veil.Controllers;

namespace Veil.Tests.Controllers
{
    [TestFixture]
    public class HomeControllerTest
    {
        [Test]
        public void Index_WhenCalled_ReturnsViewResult()
        {
            // Arrange
            HomeController controller = new HomeController();

            // Act
            ViewResult result = controller.Index() as ViewResult;

            // Assert
            Assert.That(result != null);
        }

        [Test]
        public void About_WhenCalled_ReturnsViewResult()
        {
            // Arrange
            HomeController controller = new HomeController();

            // Act
            ViewResult result = controller.About() as ViewResult;

            // Assert
            Assert.That(result != null);
            Assert.That(result.ViewBag.Message, Is.EqualTo("Your application description page."));
        }

        [Test]
        public void About_WhenCalled_SetsViewBagMessage()
        {
            // Arrange
            HomeController controller = new HomeController();

            // Act
            ViewResult result = controller.About() as ViewResult;

            // Assert
            Assert.That(result != null);
            Assert.That(result.ViewBag.Message, Is.EqualTo("Your application description page."));
        }

        [Test]
        public void Contact_WhenCalled_ReturnsViewResult()
        {
            // Arrange
            HomeController controller = new HomeController();

            // Act
            ViewResult result = controller.Contact() as ViewResult;

            // Assert
            Assert.That(result != null);
        }
    }
}
