using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;

namespace Veil.Tests.Controllers
{
    public class MockController : BaseController
    {
        public ExceptionContext MockThrowing404Exception()
        {
            HttpException exception = new HttpException(NotFound, "error");

            ExceptionContext context = new ExceptionContext(ControllerContext, exception);

            OnException(context);

            return context;
        }

        public ExceptionContext MockThrowingOtherException()
        {
            Exception exception = new Exception("error");

            ExceptionContext context = new ExceptionContext(ControllerContext, exception);

            OnException(context);

            return context;
        }
    }

    [TestFixture]
    public class BaseControllerTests
    {
        private void SetupViewEngineStub(string viewString = null)
        {
            viewString = viewString ?? string.Empty;

            Mock<IView> partialViewStub = new Mock<IView>();
            partialViewStub.
                Setup(pvs => pvs.Render(It.IsAny<ViewContext>(), It.IsAny<TextWriter>())).
                Callback((ViewContext vc, TextWriter tw) => tw.Write(viewString));

            Mock<IViewEngine> viewEngineStub = new Mock<IViewEngine>();
            var viewEngineResult = new ViewEngineResult(partialViewStub.Object, viewEngineStub.Object);
            viewEngineStub.Setup(ve => ve.FindView(It.IsAny<ControllerContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(viewEngineResult);
            ViewEngines.Engines.Clear();
            ViewEngines.Engines.Add(viewEngineStub.Object);
        }

        [Test]
        public void OnException_404Exception_HandlesException()
        {
            Mock<ControllerContext> controllerContext = new Mock<ControllerContext>();
            controllerContext.SetupSet(cc => cc.HttpContext.Response.StatusCode = It.IsAny<int>());

            SetupViewEngineStub();

            MockController controller = new MockController
            {
                ControllerContext = controllerContext.Object
            };

            ExceptionContext context = controller.MockThrowing404Exception();

            Assert.That(context.ExceptionHandled, Is.True);
        }

        [Test]
        public void OnException_OtherException_DoesNotDirectlyHandleException()
        {
            Mock<ControllerContext> controllerContext = new Mock<ControllerContext>();

            MockController controller = new MockController
            {
                ControllerContext = controllerContext.Object
            };

            ExceptionContext context = controller.MockThrowingOtherException();

            Assert.That(context.ExceptionHandled, Is.False);
        }
    }
}
