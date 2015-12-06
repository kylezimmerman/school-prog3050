using System;
using System.Linq;
using System.Web.Mvc;
using NUnit.Framework;
using Veil.Models;

namespace Veil.Tests.Models
{
    [TestFixture]
    public class AgeGateViewModelTests
    {
        [Test]
        public void Days_Contains31Days()
        {
            AgeGateViewModel model = new AgeGateViewModel();

            Assert.That(model.Days, Is.Not.Empty);
            Assert.That(model.Days.Count(), Is.EqualTo(31));
            Assert.That(model.Days, Has.Exactly(1).Matches<SelectListItem>(i => i.Text == "1"));
            Assert.That(model.Days, Has.Exactly(1).Matches<SelectListItem>(i => i.Text == "31"));
        }

        [Test]
        public void Months_ContainsAllMonths()
        {
            AgeGateViewModel model = new AgeGateViewModel();

            Assert.That(model.Months, Is.Not.Empty);
            Assert.That(model.Months.Count(), Is.EqualTo(12));
            Assert.That(model.Months, Has.Exactly(1).Matches<SelectListItem>(i => i.Text.Contains("January") && i.Text.Contains("01")));
            Assert.That(model.Months, Has.Exactly(1).Matches<SelectListItem>(i => i.Text.Contains("February") && i.Text.Contains("02")));
            Assert.That(model.Months, Has.Exactly(1).Matches<SelectListItem>(i => i.Text.Contains("March") && i.Text.Contains("03")));
            Assert.That(model.Months, Has.Exactly(1).Matches<SelectListItem>(i => i.Text.Contains("April") && i.Text.Contains("04")));
            Assert.That(model.Months, Has.Exactly(1).Matches<SelectListItem>(i => i.Text.Contains("May") && i.Text.Contains("05")));
            Assert.That(model.Months, Has.Exactly(1).Matches<SelectListItem>(i => i.Text.Contains("June") && i.Text.Contains("06")));
            Assert.That(model.Months, Has.Exactly(1).Matches<SelectListItem>(i => i.Text.Contains("July") && i.Text.Contains("07")));
            Assert.That(model.Months, Has.Exactly(1).Matches<SelectListItem>(i => i.Text.Contains("August") && i.Text.Contains("08")));
            Assert.That(model.Months, Has.Exactly(1).Matches<SelectListItem>(i => i.Text.Contains("September") && i.Text.Contains("09")));
            Assert.That(model.Months, Has.Exactly(1).Matches<SelectListItem>(i => i.Text.Contains("October") && i.Text.Contains("10")));
            Assert.That(model.Months, Has.Exactly(1).Matches<SelectListItem>(i => i.Text.Contains("November") && i.Text.Contains("11")));
            Assert.That(model.Months, Has.Exactly(1).Matches<SelectListItem>(i => i.Text.Contains("December") && i.Text.Contains("12")));
        }

        [Test]
        public void Years_ContainsCurrentYear()
        {
            AgeGateViewModel model = new AgeGateViewModel();

            Assert.That(model.Years, Is.Not.Empty);
            Assert.That(model.Years, Has.Exactly(1).Matches<SelectListItem>(i => i.Text == DateTime.Today.Year.ToString()));
        }

        [Test]
        public void Years_Contains120Years()
        {
            AgeGateViewModel model = new AgeGateViewModel();

            Assert.That(model.Years, Is.Not.Empty);
            Assert.That(model.Years.Count(), Is.EqualTo(120));
        }

        [Test]
        public void Year_IsSetToCurrentYearByDefault()
        {
            AgeGateViewModel model = new AgeGateViewModel();

            Assert.That(model.Year, Is.EqualTo(DateTime.Today.Year));
        }
    }
}
