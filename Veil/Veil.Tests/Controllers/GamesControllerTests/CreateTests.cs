using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;

namespace Veil.Tests.Controllers.GamesControllerTests
{
    public class CreateTests : GamesControllerTestsBase
    {
        [Test]
        public void Create_GET_CanView()
        {
            var esrbRatings = new List<ESRBRating>
        {
                new ESRBRating { RatingId = "E", Description = "Everyone" }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<ESRBRating>> esrbDbSetStub = TestHelpers.GetFakeAsyncDbSet(esrbRatings.AsQueryable());
            dbStub.Setup(db => db.ESRBRatings).Returns(esrbDbSetStub.Object);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null);

            var result = controller.Create() as ViewResult;

            Assert.That(result != null);
        }

        [Test]
        public async void Create_POST_Valid_RedirectsToDetails()
        {
            var game = new Game { Id = gameId };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Game>> gamesDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game>().AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gamesDbSetStub.Object);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null);

            var result = await controller.Create(game, null, null) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues != null);
            Assert.That(result.RouteValues["Id"], Is.EqualTo(game.Id));
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Details"));
        }

        [Test]
        public async void Create_POST_NoTags_GamesAddCalledOnce()
        {
            var esrbRatings = new List<ESRBRating> { everyoneESRBRating };
            var game = new Game();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Game>> gamesDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game>().AsQueryable());
            gamesDbSetStub.Setup(gdb => gdb.Add(game)).Returns(game).Verifiable();
            dbStub.Setup(db => db.Games).Returns(gamesDbSetStub.Object);

            Mock<DbSet<ESRBRating>> esrbDbSetStub = TestHelpers.GetFakeAsyncDbSet(esrbRatings.AsQueryable());
            dbStub.Setup(db => db.ESRBRatings).Returns(esrbDbSetStub.Object);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null);

            await controller.Create(game, null, null);

            Assert.That(() => gamesDbSetStub.Verify(gdb => gdb.Add(game), Times.Once), Throws.Nothing);
        }

        [Test]
        public async void Create_POST_WithTags_GamesAddCalledOnce()
        {
            var tags = new List<Tag> { tag };
            var tagNames = new List<string> { tag.Name };
            var game = new Game();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Game>> gamesDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game>().AsQueryable());
            gamesDbSetStub.Setup(gdb => gdb.Add(game)).Returns(game).Verifiable();
            dbStub.Setup(db => db.Games).Returns(gamesDbSetStub.Object);

            Mock<DbSet<Tag>> tagDbSetStub = TestHelpers.GetFakeAsyncDbSet(tags.AsQueryable());
            dbStub.Setup(db => db.Tags).Returns(tagDbSetStub.Object);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null);

            await controller.Create(game, tagNames, null);

            Assert.That(() => gamesDbSetStub.Verify(gdb => gdb.Add(game), Times.Once), Throws.Nothing);
        }

        [Test]
        public async void Create_POST_SaveChangesAsyncCalledOnce()
        {
            var game = new Game();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Game>> gamesDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game>().AsQueryable());
            dbStub.Setup(db => db.SaveChangesAsync()).ReturnsAsync(1).Verifiable();
            dbStub.Setup(db => db.Games).Returns(gamesDbSetStub.Object);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null);

            await controller.Create(game, tags: null, contentDescriptors: null);

            Assert.That(() => dbStub.Verify(db => db.SaveChangesAsync(), Times.Once), Throws.Nothing);
        }

        [Test]
        public async void Create_POST_ModelStateIsNotValid()
        {
            var games = new List<Game>();

            var esrbRatings = new List<ESRBRating> { everyoneESRBRating };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Game>> gamesDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            dbStub.Setup(db => db.SaveChangesAsync()).ReturnsAsync(1).Verifiable();
            dbStub.Setup(db => db.Games).Returns(gamesDbSetStub.Object);

            Mock<DbSet<ESRBRating>> esrbDbSetStub = TestHelpers.GetFakeAsyncDbSet(esrbRatings.AsQueryable());
            dbStub.Setup(db => db.ESRBRatings).Returns(esrbDbSetStub.Object);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null);

            controller.ModelState.AddModelError("name", "Name is required");

            var game = new Game();

            var result = await controller.Create(game, null, contentDescriptors: null) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<Game>());
            Assert.That(games, Is.Empty);
            Assert.That(() => dbStub.Verify(db => db.SaveChangesAsync(), Times.Never), Throws.Nothing);
        }
    }
}
