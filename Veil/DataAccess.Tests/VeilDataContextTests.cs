using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Linq;
using NUnit.Framework;
using Veil.DataAccess.Tests.Helpers;
using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;

namespace Veil.DataAccess.Tests
{
    [TestFixture]
    [Rollback]
    class VeilDataContextTests
    {
        private VeilDataContext db;
        protected readonly Guid UserGuid = Guid.ParseExact("854cb2ff-587e-e511-80df-001cd8b71da6", "D");
        protected readonly Guid OtherGuid = Guid.ParseExact("864cb2ff-587e-e511-80df-001cd8b71da6", "D");

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
        public void GetNextPhysicalGameProductSku_WhenCalled_Returns12DigitString()
        {
            string nextSku = db.GetNextPhysicalGameProductSku();

            Assert.That(nextSku, Is.StringMatching(@"^\d{12}$"));
        }

        [Test]
        public void GetNextPhysicalGameProductSku_WhenCalled_ReturnsGreaterThanZeroValue()
        {
            string nextSku = db.GetNextPhysicalGameProductSku();

            long value = long.Parse(nextSku);

            Assert.That(value, Is.GreaterThan(0));
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

        [Test]
        public async void DeleteUser_WithRole_CascadeDeletesTheUsersRoles()
        {
            User user = new User
            {
                Id = UserGuid,
                Email = "fake@example.com",
                FirstName = "Fake",
                LastName = "User",
                UserName = "fake@example.com",
                PasswordHash = "gibberish"
            };

            GuidIdentityRole role = new GuidIdentityRole
            {
                Id = OtherGuid,
                Name = "TestRole"
            };

            db.Users.Add(user);
            db.Roles.Add(role);

            user.Roles.Add(new GuidIdentityUserRole
            {
                RoleId = role.Id,
                UserId = user.Id
            });

            await db.SaveChangesAsync();

            ICollection<GuidIdentityUserRole> usersInRole = await db.Roles.Where(r => r.Id == role.Id).Select(r => r.Users).FirstOrDefaultAsync();

            Assert.That(usersInRole, Is.Not.Empty, "The user role wasn't created");
            Assert.That(usersInRole, Has.Count.EqualTo(1), "There should only be one user role for this test");

            db.Users.Remove(user);

            await db.SaveChangesAsync();

            usersInRole = await db.Roles.Where(r => r.Id == role.Id).Select(r => r.Users).FirstOrDefaultAsync();

            Assert.That(usersInRole, Is.Empty);
        }
    }
}
