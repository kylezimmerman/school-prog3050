using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using NUnit.Framework;
using Veil.DataAccess.Tests.Helpers;
using Veil.DataModels.Models;

namespace Veil.DataAccess.Tests
{
    [TestFixture]
    [Rollback]
    class VeilDataContextTests
    {
        private VeilDataContext db;

        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            // Migrate the database to the most current migration
            // NOTE: This still unfortunately still leaves the EnumToLookup tables in the DB
            var configuration = new Veil.DataAccess.Migrations.Configuration();
            var migrator = new DbMigrator(configuration);

            migrator.Update();
        }

        [SetUp]
        public void Setup()
        {
            db = new VeilDataContext();
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
        public void GetNextPhysicalGameProductSku_WhenCalled_ReturnsValueGreaterThanZero()
        {
            long nextSku = db.GetNextPhysicalGameProductSku();

            Assert.That(nextSku, Is.GreaterThan(0));
        }

        [Test]
        public void MarkAsModified_WhenCalled_SetsEntityStateToModified()
        {
            ESRBRating model = new ESRBRating
            {
                RatingId = "F",
                Description = "Fake Rating"
            };

            db.MarkAsModified(model);

            DbEntityEntry<ESRBRating> entry = db.Entry(model);

            Assert.That(entry.State, Is.EqualTo(EntityState.Modified));
        }
    }
}
