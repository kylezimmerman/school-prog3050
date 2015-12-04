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
    public class TagsControllerTests
    {
        [Test]
        public void Index_WhenCalled_SetsUpViewModelWithAllTagsAndSelectedList()
        {
            List<Tag> selectTags = new List<Tag>();
            List<Tag> allTags = new List<Tag>
            {
                new Tag(),
                new Tag()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Tag>> tagDbSetStub = TestHelpers.GetFakeAsyncDbSet(allTags.AsQueryable());

            dbStub.
                Setup(db => db.Tags).
                Returns(tagDbSetStub.Object);

            TagsController controller = new TagsController(dbStub.Object);

            var result = controller.Index(selectTags);

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<TagViewModel>());

            var model = (TagViewModel) result.Model;

            Assert.That(model.Selected, Is.SameAs(selectTags));
            Assert.That(model.AllTags.Count(), Is.EqualTo(allTags.Count));
            Assert.That(model.AllTags, Has.Member(allTags.First()).And.Member(allTags.Last()));
        }

        [Test]
        public void Index_WhenCalled_ReturnsPartialViewForTagsIndex()
        {
            List<Tag> selectTags = new List<Tag>();
            List<Tag> allTags = new List<Tag>();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Tag>> tagDbSetStub = TestHelpers.GetFakeAsyncDbSet(allTags.AsQueryable());

            dbStub.
                Setup(db => db.Tags).
                Returns(tagDbSetStub.Object);

            TagsController controller = new TagsController(dbStub.Object);

            var result = controller.Index(selectTags);

            Assert.That(result != null);
            Assert.That(result.ViewName, Is.Empty.Or.EqualTo("Index"));
        }
    }
}
