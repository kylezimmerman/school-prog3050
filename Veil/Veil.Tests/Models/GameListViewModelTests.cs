using NUnit.Framework;
using Veil.Models;

namespace Veil.Tests.Models
{
    [TestFixture]
    public class GameListViewModelTests
    {
        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void CurrentPage_SetLessThan1_SetsTo1(int page)
        {
            var model = new GameListViewModel
            {
                CurrentPage = page
            };

            Assert.That(model.CurrentPage, Is.EqualTo(1));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(int.MaxValue)]
        public void CurrentPage_SetGreaterThanOrEqualTo1_SetsToValue(int page)
        {
            var model = new GameListViewModel
            {
                CurrentPage = page
            };

            Assert.That(model.CurrentPage, Is.EqualTo(page));
        }
    }
}
