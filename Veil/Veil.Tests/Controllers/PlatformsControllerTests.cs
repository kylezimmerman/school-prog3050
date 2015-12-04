using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Models;

namespace Veil.Tests.Controllers
{
    [TestFixture]
    public class PlatformsControllerTests
    {
        [Test]
        public void Index_WhenCalls_SetsUpViewModelWithAllPlatformsAndSelectedList()
        {
            List<Platform> selectedPlatforms = new List<Platform>();
            List<Platform> allPlatforms = new List<Platform>
            {
                new Platform(),
                new Platform()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(allPlatforms.AsQueryable());

            dbStub.
                Setup(db => db.Platforms).
                Returns(platformDbSetStub.Object);

            PlatformsController controller = new PlatformsController(dbStub.Object);

            var result = controller.Index(selectedPlatforms);

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<PlatformViewModel>());

            var model = (PlatformViewModel) result.Model;

            Assert.That(model.Selected, Is.SameAs(selectedPlatforms));
            Assert.That(model.AllPlatforms.Count(), Is.EqualTo(allPlatforms.Count));
            Assert.That(model.AllPlatforms, Has.Member(allPlatforms.First()).And.Member(allPlatforms.Last()));
        }

        [Test]
        public void Index_WhenCalled_ReturnsPartialViewForTagsIndex()
        {
            List<Platform> selectedPlatforms = new List<Platform>();
            List<Platform> allPlatforms = new List<Platform>
            {
                new Platform(),
                new Platform()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(allPlatforms.AsQueryable());

            dbStub.
                Setup(db => db.Platforms).
                Returns(platformDbSetStub.Object);

            PlatformsController controller = new PlatformsController(dbStub.Object);

            var result = controller.Index(selectedPlatforms);

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.Empty.Or.EqualTo("Index"));
        }
    }
}
