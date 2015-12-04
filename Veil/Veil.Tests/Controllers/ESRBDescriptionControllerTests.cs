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
    public class ESRBDescriptionControllerTests
    {
        [Test]
        public void Index_WhenCalls_SetsUpViewModelWithAllContentDescriptorsAndSelectedList()
        {
            List<ESRBContentDescriptor> selectedContentDescriptors = new List<ESRBContentDescriptor>();
            List<ESRBContentDescriptor> allContentDescriptors = new List<ESRBContentDescriptor>
            {
                new ESRBContentDescriptor(),
                new ESRBContentDescriptor()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<ESRBContentDescriptor>> contentDescriptorsDbSetStub = TestHelpers.GetFakeAsyncDbSet(allContentDescriptors.AsQueryable());

            dbStub.
                Setup(db => db.ESRBContentDescriptors).
                Returns(contentDescriptorsDbSetStub.Object);

            ESRBDescriptionController controller = new ESRBDescriptionController(dbStub.Object);

            var result = controller.Index(selectedContentDescriptors);

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<ESRBDescriptionViewModel>());

            var model = (ESRBDescriptionViewModel)result.Model;

            Assert.That(model.Selected, Is.SameAs(selectedContentDescriptors));
            Assert.That(model.All.Count(), Is.EqualTo(allContentDescriptors.Count));
            Assert.That(model.All, Has.Member(allContentDescriptors.First()).And.Member(allContentDescriptors.Last()));
        }

        [Test]
        public void Index_WhenCalled_ReturnsPartialViewForTagsIndex()
        {
            List<ESRBContentDescriptor> selectedContentDescriptors = new List<ESRBContentDescriptor>();
            List<ESRBContentDescriptor> allContentDescriptors = new List<ESRBContentDescriptor>
            {
                new ESRBContentDescriptor(),
                new ESRBContentDescriptor()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<ESRBContentDescriptor>> contentDescriptorsDbSetStub = TestHelpers.GetFakeAsyncDbSet(allContentDescriptors.AsQueryable());

            dbStub.
                Setup(db => db.ESRBContentDescriptors).
                Returns(contentDescriptorsDbSetStub.Object);

            ESRBDescriptionController controller = new ESRBDescriptionController(dbStub.Object);

            var result = controller.Index(selectedContentDescriptors);

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.Empty.Or.EqualTo("Index"));
        }
    }
}
