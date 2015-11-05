using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using NUnit.Framework;
using Veil.DataAccess.Tests.Helpers;
using Veil.DataModels.Models;

namespace Veil.DataAccess.Tests
{
    [TestFixture]
    [Rollback]
    public class GetGameAvailabilityStatusTests
    {
        private VeilDataContext db;

        private Game game;

        private Company company = new Company
        {
            Name = "A Publisher/Developer"
        };

        private DateTime releaseDate = new DateTime(635821676576588170L, DateTimeKind.Local);
        private decimal newWebPrice = 1.99m;
        private int internalSkuNumber = 1;

        private PhysicalGameProduct CreateValidPhysicalGameProduct(AvailabilityStatus status)
        {
            string skuNumber = internalSkuNumber++.ToString().PadLeft(12, '0');

            return new PhysicalGameProduct
            {
                ProductAvailabilityStatus = status,
                PlatformCode = "PC",
                Developer = company,
                Publisher = company,
                ReleaseDate = releaseDate,
                NewWebPrice = newWebPrice,
                InternalNewSKU = $"0{skuNumber}",
                InteralUsedSKU = $"1{skuNumber}"
            };
        }

        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            // Migrate the database to the most current migration
            var configuration = new Veil.DataAccess.Migrations.Configuration();
            var migrator = new DbMigrator(configuration);

            migrator.Update();
        }

        [SetUp]
        public void Setup()
        {
            db = new VeilDataContext();
            game = new Game
            {
                Name = "A Game",
                ESRBRatingId = "E",
                MinimumPlayerCount = 1,
                MaximumPlayerCount = 1,
                ShortDescription = "Short Description"
            };
        }

        [TearDown]
        public void TearDown()
        {
            db?.Dispose();
        }

        [TestFixtureTearDown]
        public void FixtureTeardown()
        {
            // Return the database back to pre-first migration state
            var configuration = new Veil.DataAccess.Migrations.Configuration();
            var migrator = new DbMigrator(configuration);

            migrator.Update("0"); // 0 is equivalent to undoing all migrations
        }

        [Test]
        public async void GameAvailabilityStatus_NullGameSKUs_DoesNotThrow()
        {
            db.Games.Add(game);
            await db.SaveChangesAsync();

            await db.Entry(game).ReloadAsync();

            Assert.That(() => game.GameAvailabilityStatus, Throws.Nothing);
        }

        [Test]
        public async void GameAvailabilityStatus_NullGameSKUs_ReturnsNotForSale()
        {
            db.Games.Add(game);
            await db.SaveChangesAsync();

            await db.Entry(game).ReloadAsync();

            Assert.That(game.GameAvailabilityStatus, Is.EqualTo(AvailabilityStatus.NotForSale));
        }

        [Test]
        public async void GameAvailabilityStatus_EmptyGameSKUs_ReturnsNotForSale()
        {
            game.GameSKUs = new List<GameProduct>();

            db.Games.Add(game);
            await db.SaveChangesAsync();

            await db.Entry(game).ReloadAsync();

            Assert.That(game.GameAvailabilityStatus, Is.EqualTo(AvailabilityStatus.NotForSale));
        }

        [Test]
        public async void GameAvailabilityStatus_OnlyDiscontinuedSKUs_ReturnsDiscontinuedByManufacturer()
        {
            game.GameSKUs = new List<GameProduct>
            {
                CreateValidPhysicalGameProduct(AvailabilityStatus.DiscontinuedByManufacturer)
            };

            db.Games.Add(game);
            await db.SaveChangesAsync();

            await db.Entry(game).ReloadAsync();

            Assert.That(game.GameAvailabilityStatus, Is.EqualTo(AvailabilityStatus.DiscontinuedByManufacturer));
        }

        [Test]
        public async void GameAvailabilityStatus_OnlyAvailableSKUs_ReturnsAvailable()
        {
            game.GameSKUs = new List<GameProduct>
            {
                CreateValidPhysicalGameProduct(AvailabilityStatus.Available)
            };

            db.Games.Add(game);
            await db.SaveChangesAsync();

            await db.Entry(game).ReloadAsync();

            Assert.That(game.GameAvailabilityStatus, Is.EqualTo(AvailabilityStatus.Available));
        }

        [Test]
        public async void GameAvailabilityStatus_OnlyPreOrderSKUs_ReturnsPreOrder()
        {
            game.GameSKUs = new List<GameProduct>
            {
                CreateValidPhysicalGameProduct(AvailabilityStatus.PreOrder)
            };

            db.Games.Add(game);
            await db.SaveChangesAsync();

            await db.Entry(game).ReloadAsync();

            Assert.That(game.GameAvailabilityStatus, Is.EqualTo(AvailabilityStatus.PreOrder));
        }

        [Test]
        public async void GameAvailabilityStatus_OnlyNotForSaleSKUs_ReturnsNotForSale()
        {
            game.GameSKUs = new List<GameProduct>
            {
                CreateValidPhysicalGameProduct(AvailabilityStatus.NotForSale)
            };

            db.Games.Add(game);
            await db.SaveChangesAsync();

            await db.Entry(game).ReloadAsync();

            Assert.That(game.GameAvailabilityStatus, Is.EqualTo(AvailabilityStatus.NotForSale));
        }

        [Test]
        public async void GameAvailabilityStatus_PreOrderAndAvailableSKUs_ReturnsPreOrder()
        {
            game.GameSKUs = new List<GameProduct>
            {
                CreateValidPhysicalGameProduct(AvailabilityStatus.Available),
                CreateValidPhysicalGameProduct(AvailabilityStatus.PreOrder)
            };

            db.Games.Add(game);
            await db.SaveChangesAsync();

            await db.Entry(game).ReloadAsync();

            Assert.That(game.GameAvailabilityStatus, Is.EqualTo(AvailabilityStatus.PreOrder));
        }

        [Test]
        public async void GameAvailabilityStatus_PreOrderAvailableDiscontinuedByManufacturerSKUs_ReturnsPreOrder()
        {
            game.GameSKUs = new List<GameProduct>
            {
                CreateValidPhysicalGameProduct(AvailabilityStatus.Available),
                CreateValidPhysicalGameProduct(AvailabilityStatus.DiscontinuedByManufacturer),
                CreateValidPhysicalGameProduct(AvailabilityStatus.PreOrder)
            };

            db.Games.Add(game);
            await db.SaveChangesAsync();

            await db.Entry(game).ReloadAsync();

            Assert.That(game.GameAvailabilityStatus, Is.EqualTo(AvailabilityStatus.PreOrder));
        }

        [Test]
        public async void GameAvailabilityStatus_OneOfEachStatus_ReturnsPreOrder()
        {
            game.GameSKUs = new List<GameProduct>
            {
                CreateValidPhysicalGameProduct(AvailabilityStatus.Available),
                CreateValidPhysicalGameProduct(AvailabilityStatus.DiscontinuedByManufacturer),
                CreateValidPhysicalGameProduct(AvailabilityStatus.NotForSale),
                CreateValidPhysicalGameProduct(AvailabilityStatus.PreOrder)
            };

            db.Games.Add(game);
            await db.SaveChangesAsync();

            await db.Entry(game).ReloadAsync();

            Assert.That(game.GameAvailabilityStatus, Is.EqualTo(AvailabilityStatus.PreOrder));
        }

        [Test]
        public async void GameAvailabilityStatus_AvailableAndDiscontinuedByManufacturerSKUs_ReturnsAvailable()
        {
            game.GameSKUs = new List<GameProduct>
            {
                CreateValidPhysicalGameProduct(AvailabilityStatus.Available),
                CreateValidPhysicalGameProduct(AvailabilityStatus.DiscontinuedByManufacturer)
            };

            db.Games.Add(game);
            await db.SaveChangesAsync();

            await db.Entry(game).ReloadAsync();

            Assert.That(game.GameAvailabilityStatus, Is.EqualTo(AvailabilityStatus.Available));
        }

        [Test]
        public async void GameAvailabilityStatus_AvailableDiscontinuedByManufacturerAndNotForSaleSKUs_ReturnsAvailable()
        {
            game.GameSKUs = new List<GameProduct>
            {
                CreateValidPhysicalGameProduct(AvailabilityStatus.Available),
                CreateValidPhysicalGameProduct(AvailabilityStatus.DiscontinuedByManufacturer),
                CreateValidPhysicalGameProduct(AvailabilityStatus.NotForSale)
            };

            db.Games.Add(game);
            await db.SaveChangesAsync();

            await db.Entry(game).ReloadAsync();

            Assert.That(game.GameAvailabilityStatus, Is.EqualTo(AvailabilityStatus.Available));
        }

        [Test]
        public async void GameAvailabilityStatus_DiscontinuedByManufacturerAndNotForSaleSKUs_ReturnsDiscontinuedByManufacturer()
        {
            game.GameSKUs = new List<GameProduct>
            {
                CreateValidPhysicalGameProduct(AvailabilityStatus.DiscontinuedByManufacturer),
                CreateValidPhysicalGameProduct(AvailabilityStatus.NotForSale)
            };

            db.Games.Add(game);
            await db.SaveChangesAsync();

            await db.Entry(game).ReloadAsync();

            Assert.That(game.GameAvailabilityStatus, Is.EqualTo(AvailabilityStatus.DiscontinuedByManufacturer));
        }
    }
}
