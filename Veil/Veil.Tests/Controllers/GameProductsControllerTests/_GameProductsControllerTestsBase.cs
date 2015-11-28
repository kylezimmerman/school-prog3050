using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Moq;
using NUnit.Framework;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;

namespace Veil.Tests.Controllers.GameProductsControllerTests
{
    [TestFixture]
    public abstract class GameProductsControllerTestsBase
    {
        protected Guid gameId;
        protected Guid GameSKUId;

        protected Game game;

        protected Platform ps4Platform;

        protected Company veilCompany;

        [SetUp]
        public void Setup()
        {
            gameId = new Guid("44B0752E-998B-466A-AAAD-3ED535BA3559");
            GameSKUId = new Guid("3FE5BFCF-0A01-4EC2-A662-ADA08A2C34D2");

            game = new Game
            {
                Id = gameId,
                Name = "A game"
            };

            ps4Platform = new Platform
            {
                PlatformCode = "PS4",
                PlatformName = "Playstation 4"
            };

            veilCompany = new Company()
            {
                Id = new Guid("B4FDA176-1EA6-469A-BB02-75125D811ED4"),
                Name = "Veil"
            };
        }

        protected Mock<DbSet<Game>> MockGames(Mock<IVeilDataAccess> dbStub, IQueryable<Game> games = null)
        {
            games = games ?? new List<Game> { game }.AsQueryable();

            var stub = TestHelpers.GetFakeAsyncDbSet(games);
            dbStub.Setup(db => db.Games).Returns(stub.Object);

            return stub;
        }

        protected Mock<DbSet<Platform>> MockPlatforms(Mock<IVeilDataAccess> dbStub, IQueryable<Platform> platforms = null)
        {
            platforms = platforms ?? new List<Platform> { ps4Platform }.AsQueryable();

            var stub = TestHelpers.GetFakeAsyncDbSet(platforms);
            dbStub.Setup(db => db.Platforms).Returns(stub.Object);

            return stub;
        }

        protected Mock<DbSet<Company>> MockCompanies(Mock<IVeilDataAccess> dbStub, IQueryable<Company> companies = null)
        {
            companies = companies ?? new List<Company> { veilCompany }.AsQueryable();

            var stub = TestHelpers.GetFakeAsyncDbSet(companies);
            dbStub.Setup(db => db.Companies).Returns(stub.Object);

            return stub;
        }

        protected Mock<DbSet<GameProduct>> MockEmptyGameProducts(Mock<IVeilDataAccess> dbStub)
        {
            var stub = TestHelpers.GetFakeAsyncDbSet(new List<GameProduct>().AsQueryable());
            dbStub.Setup(db => db.GameProducts).Returns(stub.Object);

            return stub;
        }

        protected void StubLocationWithOnlineWarehouse(Mock<IVeilDataAccess> dbFake)
        {
            Mock<DbSet<Location>> locationDbSetStub = TestHelpers.GetLocationDbSetWithOnlineWarehouse();

            dbFake.Setup(db => db.Locations).Returns(locationDbSetStub.Object);
        }

        protected void StubEmptyProductLocationInventories(Mock<IVeilDataAccess> dbFake)
        {
            Mock<DbSet<ProductLocationInventory>> productInventoryStub =
                TestHelpers.GetFakeAsyncDbSet(new List<ProductLocationInventory>().AsQueryable());

            dbFake.Setup(db => db.ProductLocationInventories).Returns(productInventoryStub.Object);
        }
    }
}
