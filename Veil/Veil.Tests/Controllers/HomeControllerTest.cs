using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;

namespace Veil.Tests.Controllers
{
    [TestFixture]
    public class HomeControllerTest
    {
        [Test]
        public void Index_WhenCalled_ReturnsViewResult()
        {
            Mock<IVeilDataAccess> dbMock = new Mock<IVeilDataAccess>();
            HomeController controller = new HomeController(dbMock.Object);

            ViewResult result = controller.Index() as ViewResult;

            Assert.That(result != null);
        }

        [Test]
        public void About_WhenCalled_ReturnsViewResult()
        {
            Mock<IVeilDataAccess> dbMock = new Mock<IVeilDataAccess>();
            HomeController controller = new HomeController(dbMock.Object);

            ViewResult result = controller.About() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewBag.Message, Is.EqualTo("Your application description page."));
        }

        [Test]
        public void About_WhenCalled_SetsViewBagMessage()
        {
            Mock<IVeilDataAccess> dbMock = new Mock<IVeilDataAccess>();
            HomeController controller = new HomeController(dbMock.Object);

            ViewResult result = controller.About() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewBag.Message, Is.EqualTo("Your application description page."));
        }

        [Test]
        public void Contact_WhenCalled_ReturnsViewResult()
        {
            Mock<IVeilDataAccess> dbMock = new Mock<IVeilDataAccess>();
            HomeController controller = new HomeController(dbMock.Object);

            ViewResult result = controller.Contact() as ViewResult;

            Assert.That(result != null);
        }
    }
}
