using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Models;

namespace Veil.Tests.Controllers
{
    [TestFixture]
    public class HomeControllerTests
    {
        private Game game1;
        private Game game2;
        private Game game3;
        private Game game4;
        private Game game5;

        [SetUp]
        public void Setup()
        {
            GameProduct futureGameProduct = new PhysicalGameProduct()
            {
                ReleaseDate = DateTime.MaxValue
            };

            List<GameProduct> futureGames = new List<GameProduct>()
            {
                futureGameProduct
            };

            GameProduct pastGameProduct = new PhysicalGameProduct()
            {
                ReleaseDate = DateTime.MinValue
            };

            List<GameProduct> pastGames = new List<GameProduct>()
            {
                pastGameProduct
            };

            game1 = new Game()
            {
                GameSKUs = futureGames
            };

            game2 = new Game()
            {
                GameSKUs = futureGames
            };

            game3 = new Game()
            {
                GameSKUs = pastGames
            };

            game4 = new Game()
            {
                GameSKUs = pastGames
            };

            game5 = new Game()
            {
                GameSKUs = pastGames
            };
        }

        [Test]
        public async void Index_WhenCalled_ReturnsViewResult()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gamesDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> {game1, game2, game3, game4, game5}.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gamesDbSetStub.Object);

            HomeController controller = new HomeController(dbStub.Object);

            var result = await controller.Index() as ViewResult;

            Assert.That(result.Model != null);

            var model = (HomePageViewModel)result.Model;

            Assert.That(model.ComingSoon.Select(g => g.GameSKUs), Has.All.Matches<List<GameProduct>>(gs => gs.Any(lgp => lgp.ReleaseDate > DateTime.Now)));
            Assert.That(model.NewReleases.Select(g => g.GameSKUs), Has.All.Matches<List<GameProduct>>(gs => gs.Any(lgp => lgp.ReleaseDate < DateTime.Now)));
        }
    }
}
