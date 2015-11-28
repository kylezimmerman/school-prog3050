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
using Veil.DataModels;
using Veil.DataModels.Models;

namespace Veil.Tests.Controllers.GamesControllerTests
{
    public class DeleteTests : GamesControllerTestsBase
    {
        [TestCase(VeilRoles.MEMBER_ROLE)]
        [TestCase(VeilRoles.ADMIN_ROLE)]
        public async void DeleteGame_ValidDeleteWithGameProduct(string role)
        {
            Game aGame = new Game
            {
                Id = Id
            };

            GameProduct aGameProduct = new PhysicalGameProduct();
            aGameProduct.GameId = aGame.Id;
            aGameProduct.Id = new Guid("44B0752E-968B-477A-AAAD-3ED535BA3559");

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { aGame }.AsQueryable());
            Mock<DbSet<GameProduct>> gameProductsDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> { aGameProduct }.AsQueryable());

            gameDbSetStub.Setup(g => g.FindAsync(aGame.Id)).ReturnsAsync(aGame);
            gameProductsDbSetStub.Setup(gp => gp.FindAsync(aGameProduct.Id)).ReturnsAsync(aGameProduct);

            dbStub.Setup(db => db.GameProducts).Returns(gameProductsDbSetStub.Object);
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().IsInRole(role);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Delete(aGame.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model != null);
            Assert.That(result.Model, Is.InstanceOf<Game>());
        }

        [TestCase(VeilRoles.MEMBER_ROLE)]
        [TestCase(VeilRoles.ADMIN_ROLE)]
        public async void DeleteGame_ValidDeleteNoGameProduct(string role)
        {
            Game aGame = new Game
            {
                Id = Id,
                GameSKUs = new List<GameProduct>()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { aGame }.AsQueryable());

            gameDbSetStub.Setup(g => g.FindAsync(aGame.Id)).ReturnsAsync(aGame);
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().IsInRole(role);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Delete(aGame.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model != null);
            Assert.That(result.Model, Is.InstanceOf<Game>());
        }

        [TestCase(VeilRoles.MEMBER_ROLE)]
        [TestCase(VeilRoles.ADMIN_ROLE)]
        public void DeleteGame_NullId(string role)
        {
            GamesController controller = new GamesController(veilDataAccess: null, idGetter: null);

            Assert.That(async () => await controller.Delete(null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }

        [TestCase(VeilRoles.MEMBER_ROLE)]
        [TestCase(VeilRoles.ADMIN_ROLE)]
        public void DeleteGame_IdNotInDb(string role)
        {
            Game aGame = new Game
            {
                Id = Id
            };

            Guid nonMatch = new Guid("44B0752E-968B-477A-AAAD-3ED535BA3559");

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { aGame }.AsQueryable());

            gameDbSetStub.Setup(g => g.FindAsync(aGame.Id)).ReturnsAsync(aGame);
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().IsInRole(role);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            Assert.That(async () => await controller.Delete(nonMatch), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }
    }
}
