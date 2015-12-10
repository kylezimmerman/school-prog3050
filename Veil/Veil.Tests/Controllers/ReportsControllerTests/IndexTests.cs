using System.Web.Mvc;
using NUnit.Framework;
using Veil.Controllers;

namespace Veil.Tests.Controllers.ReportsControllerTests
{
    class IndexTests : ReportsControllerTestsBase
    {
        [Test]
        public void Index_WhenCalled_ReturnsIndexView()
        {
            ReportsController controller = new ReportsController(null);
            var result = controller.Index() as ViewResult;
            Assert.That(result != null);
        }
    }
}
