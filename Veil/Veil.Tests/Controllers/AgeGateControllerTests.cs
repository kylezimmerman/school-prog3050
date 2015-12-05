using System;
using System.Web;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.Models;

namespace Veil.Tests.Controllers
{
    [TestFixture]
    public class AgeGateControllerTests
    {
        private static int[] invalidYears =
        {
            DateTime.UtcNow.Year + 1,
            DateTime.MinValue.Year - 1
        };

        [Test]
        public void GetDateOfBirthValue_WithoutCookie_ReturnsNull()
        {
            HttpCookieCollection cookies = new HttpCookieCollection();

            var result = AgeGateController.GetDateOfBirthValue(cookies);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetDateOfBirthValue_InvalidCookieValue_ReturnsNull()
        {
            HttpCookieCollection cookies = new HttpCookieCollection
            {
                new HttpCookie(AgeGateController.DATE_OF_BIRTH_COOKIE, "this isn't a date")
            };

            var result = AgeGateController.GetDateOfBirthValue(cookies);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetDateOfBirthValue_ValidDate_ReturnsDate()
        {
            DateTime dateOfBirth = new DateTime(2015, 5, 20, 10, 59, 59);

            HttpCookieCollection cookies = new HttpCookieCollection
            {
                new HttpCookie(AgeGateController.DATE_OF_BIRTH_COOKIE, dateOfBirth.ToString())
            };

            var result = AgeGateController.GetDateOfBirthValue(cookies);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Value, Is.EqualTo(dateOfBirth));
        }

        [Test]
        public void Index_DayHigherThanNumberOfDaysInMonth_RedisplaysViewWithSameViewModel()
        {
            AgeGateViewModel model = new AgeGateViewModel
            {
                Day = 31, // Only 28 or 29 days
                Month = 2 // February
            };

            AgeGateController controller = new AgeGateController();

            var result = controller.Index(model) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.Empty.Or.EqualTo(nameof(AgeGateController.Index)));
            Assert.That(result.Model, Is.SameAs(model));
        }

        [Test]
        public void Index_MonthOutsideOfValidRange_RedisplaysViewWithSameViewModel([Values(-1, 13)]int month)
        {
            AgeGateViewModel model = new AgeGateViewModel
            {
                Day = 1,
                Month = month
            };

            AgeGateController controller = new AgeGateController();

            var result = controller.Index(model) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.Empty.Or.EqualTo(nameof(AgeGateController.Index)));
            Assert.That(result.Model, Is.SameAs(model));
        }

        [Test, TestCaseSource(typeof(AgeGateControllerTests), nameof(invalidYears))]
        public void Index_YearOutsideOfValidRange_RedisplaysViewWithSameViewModel(int year)
        {
            AgeGateViewModel model = new AgeGateViewModel
            {
                Day = 1,
                Month = 1,
                Year = year
            };

            AgeGateController controller = new AgeGateController();

            var result = controller.Index(model) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.Empty.Or.EqualTo(nameof(AgeGateController.Index)));
            Assert.That(result.Model, Is.SameAs(model));
        }

        [Test]
        public void Index_ValidDate_AddsCookieWithCorrectKeyAndValue()
        {
            AgeGateViewModel model = new AgeGateViewModel
            {
                Day = 4,
                Month = 12,
                Year = 2015
            };

            HttpCookieCollection cookieCollection = new HttpCookieCollection();

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.
                Setup(c => c.HttpContext.Response.Cookies).
                Returns(cookieCollection);

            Mock<UrlHelper> urlHelperStub = new Mock<UrlHelper>();
            urlHelperStub.
                Setup(uh => uh.IsLocalUrl(It.IsAny<string>())).
                Returns(false);

            AgeGateController controller = new AgeGateController
            {
                ControllerContext = contextStub.Object,
                Url = urlHelperStub.Object
            };

            controller.Index(model);

            HttpCookie cookie = cookieCollection[AgeGateController.DATE_OF_BIRTH_COOKIE];

            Assert.That(cookie != null);
            Assert.That(cookie.Name, Is.SameAs(AgeGateController.DATE_OF_BIRTH_COOKIE));
            Assert.That(cookie.Value, Is.EqualTo("12/4/2015"));
        }

        [Test]
        public void Index_ValidDate_NewCookieExpiresInADay()
        {
            AgeGateViewModel model = new AgeGateViewModel
            {
                Day = 4,
                Month = 12,
                Year = 2015
            };

            HttpCookieCollection cookieCollection = new HttpCookieCollection();

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.
                Setup(c => c.HttpContext.Response.Cookies).
                Returns(cookieCollection);

            Mock<UrlHelper> urlHelperStub = new Mock<UrlHelper>();
            urlHelperStub.
                Setup(uh => uh.IsLocalUrl(It.IsAny<string>())).
                Returns(false);

            AgeGateController controller = new AgeGateController
            {
                ControllerContext = contextStub.Object,
                Url = urlHelperStub.Object
            };

            controller.Index(model);

            HttpCookie cookie = cookieCollection[AgeGateController.DATE_OF_BIRTH_COOKIE];

            Assert.That(cookie != null);
            Assert.That(cookie.Expires, Is.EqualTo(DateTime.UtcNow.AddDays(1)).Within(1).Minutes);
        }

        [Test]
        public void Index_NonLocalUrl_RedirectsToHomeIndex()
        {
            AgeGateViewModel model = new AgeGateViewModel
            {
                Day = 4,
                Month = 12,
                Year = 2015
            };

            HttpCookieCollection cookieCollection = new HttpCookieCollection();

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.
                Setup(c => c.HttpContext.Response.Cookies).
                Returns(cookieCollection);

            Mock<UrlHelper> urlHelperStub = new Mock<UrlHelper>();
            urlHelperStub.
                Setup(uh => uh.IsLocalUrl(It.IsAny<string>())).
                Returns(false);

            AgeGateController controller = new AgeGateController
            {
                ControllerContext = contextStub.Object,
                Url = urlHelperStub.Object
            };

            var result = controller.Index(model) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(HomeController.Index)));
            Assert.That(result.RouteValues["Controller"], Is.EqualTo("Home"));
        }

        [Test]
        public void Index_LocalUrl_RedirectsToReturnUrl()
        {
            string returnUrl = "/Games/1";

            AgeGateViewModel model = new AgeGateViewModel
            {
                Day = 4,
                Month = 12,
                Year = 2015,
                ReturnUrl = returnUrl
            };

            HttpCookieCollection cookieCollection = new HttpCookieCollection();

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.
                Setup(c => c.HttpContext.Response.Cookies).
                Returns(cookieCollection);

            Mock<UrlHelper> urlHelperStub = new Mock<UrlHelper>();
            urlHelperStub.
                Setup(uh => uh.IsLocalUrl(It.IsAny<string>())).
                Returns(true);

            AgeGateController controller = new AgeGateController
            {
                ControllerContext = contextStub.Object,
                Url = urlHelperStub.Object
            };

            var result = controller.Index(model) as RedirectResult;

            Assert.That(result != null);
            Assert.That(result.Url, Is.SameAs(returnUrl));
        }
    }
}
