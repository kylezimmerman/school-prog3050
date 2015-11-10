using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels;
using Veil.DataModels.Models;

namespace Veil.Tests.Controllers
{
    [TestFixture]
    class GameProductsControllerTests
    {
        private Guid Id;


        [SetUp]
        public void Setup()
        {
            Id = new Guid("44B0752E-998B-466A-AAAD-3ED535BA3559");
        }

        [TestCase(VeilRoles.MEMBER_ROLE)]
        [TestCase(VeilRoles.ADMIN_ROLE)]
        public async void DeletePhysicalGameProduct_ValidDelete(string role)
        {
            GameProduct gameSku = new PhysicalGameProduct();
            gameSku.GameId = Id;
            gameSku.Id = Id;

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<GameProduct>> gameProductDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> { gameSku }.AsQueryable());
            gameProductDbSetStub.SetupForInclude();

            gameProductDbSetStub.Setup(gp => gp.FindAsync(Id)).ReturnsAsync(gameSku);
            dbStub.Setup(db => db.GameProducts).Returns(gameProductDbSetStub.Object);


            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().IsInRole(role);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetter: null)
            {
                ControllerContext = contextStub.Object
            };

            var result = await controller.Delete(gameSku.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.Not.Null);
            Assert.That(result.Model, Is.InstanceOf<GameProduct>());
        }

        [TestCase(VeilRoles.MEMBER_ROLE)]
        [TestCase(VeilRoles.ADMIN_ROLE)]
        public async void DeletePhysicalGameProduct_InvalidID(string role)
        {
            GameProduct gameSku = new PhysicalGameProduct();
            gameSku.GameId = Id;
            gameSku.Id = Id;

            Guid nonMatch = new Guid("44B0752E-998B-477A-AAAD-3ED535BA3559");

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<GameProduct>> gameProductDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> { gameSku }.AsQueryable());
            gameProductDbSetStub.SetupForInclude();

            gameProductDbSetStub.Setup(gp => gp.FindAsync(Id)).ReturnsAsync(gameSku);
            dbStub.Setup(db => db.GameProducts).Returns(gameProductDbSetStub.Object);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.SetupUser().IsInRole(role);

            GamesController controller = new GamesController(dbStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            Assert.That(async () => await controller.Delete(nonMatch), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }
    }
}
