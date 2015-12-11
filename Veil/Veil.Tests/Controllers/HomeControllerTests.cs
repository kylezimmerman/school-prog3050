using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels;
using Veil.DataModels.Models;
using Veil.Models;

namespace Veil.Tests.Controllers
{
    [TestFixture]
    public class HomeControllerTests
    {
        private Game futureGame1;
        private Game futureGame2;
        private Game pastGame1;
        private Game pastGame2;
        private Game pastGame3;

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

            futureGame1 = new Game()
            {
                GameSKUs = futureGames
            };

            futureGame2 = new Game()
            {
                GameSKUs = futureGames
            };

            pastGame1 = new Game()
            {
                GameSKUs = pastGames
            };

            pastGame2 = new Game()
            {
                GameSKUs = pastGames
            };

            pastGame3 = new Game()
            {
                GameSKUs = pastGames
            };
        }

        [Test]
        public async void Index_WhenCalled_ReturnsViewResult()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gamesDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> {futureGame1, futureGame2, pastGame1, pastGame2, pastGame3}.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gamesDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().InAllRoles();

            HomeController controller = new HomeController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Index() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<HomePageViewModel>());

            var model = (HomePageViewModel)result.Model;

            Assert.That(model.ComingSoon.Select(g => g.GameSKUs), Has.All.Matches<List<GameProduct>>(gs => gs.Any(lgp => lgp.ReleaseDate > DateTime.Now)));
            Assert.That(model.NewReleases.Select(g => g.GameSKUs), Has.All.Matches<List<GameProduct>>(gs => gs.Any(lgp => lgp.ReleaseDate < DateTime.Now)));
        }

        [TestCase(null)] //Unauthenticated
        [TestCase(VeilRoles.MEMBER_ROLE)]
        public async void Index_Unprivileged_ExcludesNotForSaleGame(string role)
        {
            futureGame1.GameAvailabilityStatus = AvailabilityStatus.NotForSale;
            futureGame2.GameAvailabilityStatus = AvailabilityStatus.PreOrder;
            pastGame1.GameAvailabilityStatus = AvailabilityStatus.NotForSale;
            pastGame2.GameAvailabilityStatus = AvailabilityStatus.Available;
            pastGame3.GameAvailabilityStatus = AvailabilityStatus.DiscontinuedByManufacturer;

            List<Game> games = new List<Game>
            {
                futureGame1, futureGame2, pastGame1, pastGame2, pastGame3
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().IsInRole(role);

            HomeController controller = new HomeController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Index() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<HomePageViewModel>());

            var model = (HomePageViewModel)result.Model;

            Assert.That(model.ComingSoon, Has.Count.EqualTo(1));
            Assert.That(model.ComingSoon, Has.Member(futureGame2));

            Assert.That(model.NewReleases, Has.Count.EqualTo(2));
            Assert.That(model.NewReleases, Has.Member(pastGame2));
            Assert.That(model.NewReleases, Has.Member(pastGame3));
        }

        [TestCase(VeilRoles.EMPLOYEE_ROLE)]
        [TestCase(VeilRoles.ADMIN_ROLE)]
        public async void Index_Privileged_IncludesNotForSaleGame(string role)
        {
            futureGame1.GameAvailabilityStatus = AvailabilityStatus.NotForSale;
            futureGame2.GameAvailabilityStatus = AvailabilityStatus.PreOrder;
            pastGame1.GameAvailabilityStatus = AvailabilityStatus.NotForSale;
            pastGame2.GameAvailabilityStatus = AvailabilityStatus.Available;
            pastGame3.GameAvailabilityStatus = AvailabilityStatus.DiscontinuedByManufacturer;

            List<Game> games = new List<Game>
            {
                futureGame1, futureGame2, pastGame1, pastGame2, pastGame3
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(games.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().IsInRole(role);

            HomeController controller = new HomeController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Index() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<HomePageViewModel>());

            var model = (HomePageViewModel)result.Model;

            Assert.That(model.ComingSoon, Has.Count.EqualTo(2));
            Assert.That(model.ComingSoon, Has.Member(futureGame1));
            Assert.That(model.ComingSoon, Has.Member(futureGame2));

            Assert.That(model.NewReleases, Has.Count.EqualTo(3));
            Assert.That(model.NewReleases, Has.Member(pastGame1));
            Assert.That(model.NewReleases, Has.Member(pastGame2));
            Assert.That(model.NewReleases, Has.Member(pastGame3));
        }

        [Test]
        public void Contact_WhenCalled_ReturnsView()
        {
            HomeController controller = new HomeController(null);

            var result = controller.Contact() as ViewResult;

            Assert.That(result != null);
        }

        [Test]
        public void About_WhenCalled_ReturnsView()
        {
            HomeController controller = new HomeController(null);

            var result = controller.About() as ViewResult;

            Assert.That(result != null);
        }
    }
}
