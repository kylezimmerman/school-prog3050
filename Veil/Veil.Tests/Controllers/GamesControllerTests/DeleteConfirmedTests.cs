using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
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
    public class DeleteConfirmedTests : GamesControllerTestsBase
    {
        [TestCase(VeilRoles.MEMBER_ROLE)]
        [TestCase(VeilRoles.ADMIN_ROLE)]
        public async void DeleteGameConfirmed_ValidDeleteWithGameProduct(string role)
        {
            Game aGame = new Game
            {
                Id = Id,
                GameSKUs = new List<GameProduct>()
            };

            GameProduct aGameProduct = new PhysicalGameProduct();
            aGameProduct.GameId = aGame.Id;
            aGameProduct.Id = new Guid("44B0752E-968B-477A-AAAD-3ED535BA3559");
            aGameProduct.Game = aGame;

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

            var result = await controller.DeleteGameConfirmed(aGame.Id) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Index"));
        }

        [TestCase(VeilRoles.MEMBER_ROLE)]
        [TestCase(VeilRoles.ADMIN_ROLE)]
        public async void DeleteGameConfirmed_ValidDeleteNoGameProduct(string role)
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

            var result = await controller.DeleteGameConfirmed(aGame.Id) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Index"));
        }

        [TestCase(VeilRoles.MEMBER_ROLE)]
        [TestCase(VeilRoles.ADMIN_ROLE)]
        public void DeleteGameConfirmed_IdNotInDb(string role)
        {
            Game aGame = new Game
            {
                Id = Id,
                GameSKUs = new List<GameProduct>()
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

            Assert.That(async () => await controller.DeleteGameConfirmed(nonMatch), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }
        [TestCase(VeilRoles.MEMBER_ROLE)]
        [TestCase(VeilRoles.ADMIN_ROLE)]
        public async void DeleteGameConfirmed_CatchesOnSaveDelete(string role)
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
            dbStub.Setup(db => db.SaveChangesAsync()).Throws<DbUpdateException>();

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().IsInRole(role);

            GamesController controller = new GamesController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.DeleteGameConfirmed(aGame.Id) as RedirectToRouteResult;

            Assert.That(result != null);
        }
    }
}
