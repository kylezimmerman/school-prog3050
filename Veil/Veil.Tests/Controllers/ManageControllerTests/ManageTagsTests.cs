/* ManageTagsTests.cs
 *      Isaac West, 2015.11.29: Created
 */

using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Principal;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Helpers;

namespace Veil.Tests.Controllers.ManageControllerTests
{
    public class ManageTagsTests : ManageControllerTestsBase
    {
        [Test]
        public async void ManageTags_ReturnsMatchingModel()
        {
            Member member = new Member
            {
                UserId = memberId,
                FavoriteTags = new List<Tag>
                {
                    new Tag
                    {
                        Name = "TestTag"
                    }
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            ManageController controller = new ManageController(userManager: null, signInManager: null,
                veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object, stripeService: null)
            {
                ControllerContext = context.Object
            };

            var result = await controller.ManageTags() as ViewResult;

            Assert.That(result != null);

            var model = (List<Tag>)result.Model;

            Assert.That(model.Count, Is.EqualTo(1));
            Assert.That(model[0].Name, Is.EqualTo("TestTag"));
        }

        [Test]
        public async void ManageTags_AddingTags_ReturnsUpdatedModel()
        {
            List<Tag> tags = new List<Tag>
            {
                new Tag
                {
                    Name = "Test Tag"
                },
                new Tag
                {
                    Name = "Second Tag"
                }
            };

            List<string> tagStrings = new List<string>
            {
                "Test Tag",
                "Second Tag"
            };

            Member member = new Member
            {
                UserId = memberId,
                FavoriteTags = new List<Tag>
                {
                    tags[0]
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Tag>> tagDbSetStub = TestHelpers.GetFakeAsyncDbSet(tags.AsQueryable());
            dbStub.Setup(db => db.Tags).Returns(tagDbSetStub.Object);

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            ManageController controller = new ManageController(userManager: null, signInManager: null,
                veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object, stripeService: null)
            {
                ControllerContext = context.Object
            };

            await controller.ManageTags(tagStrings);

            Assert.That(member.FavoriteTags.Count, Is.EqualTo(2));
            Assert.That(member.FavoriteTags.Any(t => t.Name == "Test Tag"));
            Assert.That(member.FavoriteTags.Any(t => t.Name == "Second Tag"));
        }

        [Test]
        public async void ManageTags_RemoveTags_ReturnsUpdatedModel()
        {
            List<Tag> tags = new List<Tag>
            {
                new Tag
                {
                    Name = "Test Tag"
                },
                new Tag
                {
                    Name = "Second Tag"
                }
            };

            List<string> tagStrings = new List<string>
            {
                "Test Tag"
            };

            Member member = new Member
            {
                UserId = memberId,
                FavoriteTags = new List<Tag>
                {
                    tags[0]
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Tag>> tagDbSetStub = TestHelpers.GetFakeAsyncDbSet(tags.AsQueryable());
            dbStub.Setup(db => db.Tags).Returns(tagDbSetStub.Object);

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            ManageController controller = new ManageController(userManager: null, signInManager: null,
                veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object, stripeService: null)
            {
                ControllerContext = context.Object
            };

            await controller.ManageTags(tagStrings);

            Assert.That(member.FavoriteTags.Count, Is.EqualTo(1));
            Assert.That(member.FavoriteTags.Any(t => t.Name == "Test Tag"));
            Assert.That(member.FavoriteTags.All(t => t.Name != "Second Tag"));
        }

        [Test]
        public async void ManageTags_NullClearsTags_ReturnsUpdatedModel()
        {
            List<Tag> tags = new List<Tag>
            {
                new Tag
                {
                    Name = "Test Tag"
                },
                new Tag
                {
                    Name = "Second Tag"
                }
            };

            Member member = new Member
            {
                UserId = memberId,
                FavoriteTags = new List<Tag>
                {
                    tags[0]
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Tag>> tagDbSetStub = TestHelpers.GetFakeAsyncDbSet(tags.AsQueryable());
            dbStub.Setup(db => db.Tags).Returns(tagDbSetStub.Object);

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            ManageController controller = new ManageController(userManager: null, signInManager: null,
                veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object, stripeService: null)
            {
                ControllerContext = context.Object
            };

            await controller.ManageTags(null);

            Assert.That(member.FavoriteTags.Count, Is.EqualTo(0));
        }
    }
}
