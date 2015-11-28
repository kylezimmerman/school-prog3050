using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;

namespace Veil.Tests.Controllers.GamesControllerTests
{
    public class EditTests : GamesControllerTestsBase
    {
        [Test]
        public void Edit_GET_Invalid_NullId()
        {
            GamesController controller = new GamesController(veilDataAccess: null, idGetter: null);

            Assert.That(async () => await controller.Edit(id: null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }

        [Test]
        public void Edit_GET_Invalid_NonExistantId()
        {
            var games = new List<Game>
            {
                new Game {Id = Id}
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.Edit(id: Guid.Empty), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }

        [Test]
        public async void Edit_GET_Valid_ViewById()
        {
            var game = new Game { Id = Id };

            var games = new List<Game>
            {
               game
            };

            var esrbRating = new List<ESRBRating>
            {
                new ESRBRating { RatingId = "E", Description = "Everyone" }
            };


            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);
            gameDbSetStub.Setup(gdb => gdb.FindAsync(game.Id)).ReturnsAsync(game);

            Mock<DbSet<ESRBRating>> esrbRatingDbSetStub = TestHelpers.GetFakeAsyncDbSet(esrbRating.AsQueryable());
            dbStub.Setup(db => db.ESRBRatings).Returns(esrbRatingDbSetStub.Object);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null);

            var result = await controller.Edit(game.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<Game>());
            Assert.That(result.Model, Is.EqualTo(game));
        }

        [Test]
        public async void Edit_POST_Valid_NoTags()
        {
            var game = new Game { Id = Id, Tags = new List<Tag>(), ContentDescriptors = new List<ESRBContentDescriptor>() };

            var games = new List<Game>
            {
               game
            };

            var esrbRating = new List<ESRBRating>
            {
                new ESRBRating { RatingId = "E", Description = "Everyone" }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            gameDbSetStub.SetupForInclude();
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<DbSet<ESRBRating>> esrbRatingDbSetStub = TestHelpers.GetFakeAsyncDbSet(esrbRating.AsQueryable());
            dbStub.Setup(db => db.ESRBRatings).Returns(esrbRatingDbSetStub.Object);

            Mock<DbSet<Tag>> tagsDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Tag>().AsQueryable());
            dbStub.Setup(db => db.Tags).Returns(tagsDbSetStub.Object);

            Mock<DbSet<ESRBContentDescriptor>> contentDescriptorStub = TestHelpers.GetFakeAsyncDbSet(new List<ESRBContentDescriptor>().AsQueryable());
            dbStub.Setup(db => db.ESRBContentDescriptors).Returns(contentDescriptorStub.Object);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null);

            var result = await controller.Edit(game, null, contentDescriptors: null) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(game.Tags, Is.Empty);
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Details"));
            Assert.That(result.RouteValues["Id"], Is.EqualTo(game.Id));
        }

        [Test]
        public async void Edit_POST_Valid_ConfirmSaveChangesAsyncCalled()
        {
            var game = new Game { Id = Id, Tags = new List<Tag>(), ContentDescriptors = new List<ESRBContentDescriptor>() };

            var games = new List<Game>
            {
               game
            };

            var esrbRating = new List<ESRBRating>
            {
                new ESRBRating { RatingId = "E", Description = "Everyone" }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            gameDbSetStub.SetupForInclude();
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);
            dbStub.Setup(db => db.SaveChangesAsync()).ReturnsAsync(1).Verifiable();


            Mock<DbSet<ESRBRating>> esrbRatingDbSetStub = TestHelpers.GetFakeAsyncDbSet(esrbRating.AsQueryable());
            dbStub.Setup(db => db.ESRBRatings).Returns(esrbRatingDbSetStub.Object);

            Mock<DbSet<Tag>> tagsDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Tag>().AsQueryable());
            dbStub.Setup(db => db.Tags).Returns(tagsDbSetStub.Object);

            Mock<DbSet<ESRBContentDescriptor>> contentDescriptorStub = TestHelpers.GetFakeAsyncDbSet(new List<ESRBContentDescriptor>().AsQueryable());
            dbStub.Setup(db => db.ESRBContentDescriptors).Returns(contentDescriptorStub.Object);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null);

            await controller.Edit(game, null, contentDescriptors: null);

            //Note: this is called exactly 2 times instead of once due to the Tag saving workaround.
            Assert.That(() => dbStub.Verify(db => db.SaveChangesAsync(), Times.Exactly(2)), Throws.Nothing);
        }

        [Test]
        public async void Edit_POST_Valid_WithTags()
        {
            var game = new Game { Id = Id, Tags = new List<Tag>(), ContentDescriptors = new List<ESRBContentDescriptor>() };
            var tagNames = new List<string> { tag.Name };

            var games = new List<Game>
            {
               game
            };

            var esrbRating = new List<ESRBRating>
            {
                new ESRBRating { RatingId = "E", Description = "Everyone" }
            };

            var tags = new List<Tag> { tag };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            gameDbSetStub.SetupForInclude();
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<DbSet<ESRBRating>> esrbRatingDbSetStub = TestHelpers.GetFakeAsyncDbSet(esrbRating.AsQueryable());
            dbStub.Setup(db => db.ESRBRatings).Returns(esrbRatingDbSetStub.Object);

            Mock<DbSet<Tag>> tagsDbSetStub = TestHelpers.GetFakeAsyncDbSet(tags.AsQueryable());
            dbStub.Setup(db => db.Tags).Returns(tagsDbSetStub.Object);

            Mock<DbSet<ESRBContentDescriptor>> contentDescriptorStub = TestHelpers.GetFakeAsyncDbSet(new List<ESRBContentDescriptor>().AsQueryable());
            dbStub.Setup(db => db.ESRBContentDescriptors).Returns(contentDescriptorStub.Object);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null);

            var result = await controller.Edit(game, tagNames, contentDescriptors: null) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(game.Tags != null);
            Assert.That(game.Tags.Count, Is.EqualTo(1));
            Assert.That(game.Tags, Contains.Item(tag));
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Details"));
            Assert.That(result.RouteValues["Id"], Is.EqualTo(game.Id));
        }

        [Test]
        public async void Edit_POST_Valid_WithContentDescriptors()
        {
            var contentDescriptor = new ESRBContentDescriptor { Id = 1, DescriptorName = "Test Descriptor" };
            var contentDescriptors = new List<ESRBContentDescriptor> { contentDescriptor };

            var game = new Game { Id = Id, Tags = new List<Tag>(), ContentDescriptors = new List<ESRBContentDescriptor>() };

            var contentDescriptorIds = new List<int> { contentDescriptor.Id };

            var games = new List<Game>
            {
                game,
            };

            var esrbRating = new List<ESRBRating>
            {
                new ESRBRating { RatingId = "E", Description = "Everyone" }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            gameDbSetStub.SetupForInclude();
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<DbSet<ESRBRating>> esrbRatingDbSetStub = TestHelpers.GetFakeAsyncDbSet(esrbRating.AsQueryable());
            dbStub.Setup(db => db.ESRBRatings).Returns(esrbRatingDbSetStub.Object);

            Mock<DbSet<Tag>> tagsDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Tag>().AsQueryable());
            dbStub.Setup(db => db.Tags).Returns(tagsDbSetStub.Object);

            Mock<DbSet<ESRBContentDescriptor>> contentDescriptorStub = TestHelpers.GetFakeAsyncDbSet(contentDescriptors.AsQueryable());
            dbStub.Setup(db => db.ESRBContentDescriptors).Returns(contentDescriptorStub.Object);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null);

            var result = await controller.Edit(game, tags: null, contentDescriptors: contentDescriptorIds) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(game.ContentDescriptors != null);
            Assert.That(game.ContentDescriptors.Count, Is.EqualTo(1));
            Assert.That(game.ContentDescriptors, Contains.Item(contentDescriptor));
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Details"));
            Assert.That(result.RouteValues["Id"], Is.EqualTo(game.Id));
        }

        [Test]
        public async void Edit_POST_Invalid_ModelState_IsValid_False()
        {
            var game = new Game();
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<ESRBRating>> esrbRatingDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<ESRBRating>().AsQueryable());
            dbStub.Setup(db => db.ESRBRatings).Returns(esrbRatingDbSetStub.Object);


            GamesController controller = new GamesController(dbStub.Object, idGetter: null);
            controller.ModelState.AddModelError("id", "id");

            var result = await controller.Edit(game, tags: null, contentDescriptors: null) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model != null);
            Assert.That(result.Model, Is.InstanceOf<Game>());
            Assert.That(result.Model, Is.EqualTo(game));
        }
    }
}
