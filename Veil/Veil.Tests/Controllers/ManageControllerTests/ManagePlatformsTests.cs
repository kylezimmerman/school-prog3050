/* ManagePlatformsTests.cs
 *      Isaac West, 2015.11.29: Created
 */ 

using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Principal;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Helpers;

namespace Veil.Tests.Controllers.ManageControllerTests
{
    public class ManagePlatformsTests : ManageControllerTestsBase
    {
        [Test]
        public async void ManagePlatforms_ReturnsMatchingModel()
        {
            Member member = new Member
            {
                UserId = memberId,
                FavoritePlatforms = new List<Platform>
                {
                    new Platform
                    {
                        PlatformCode = "TPlat",
                        PlatformName = "Test Platform"
                    }
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            ManageController controller = new ManageController(
                userManager: null, signInManager: null,
                veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object, stripeService: null)
            {
                ControllerContext = context.Object
            };

            var result = await controller.ManagePlatforms() as ViewResult;

            Assert.That(result != null);

            var model = (List<Platform>) result.Model;

            Assert.That(model.Count, Is.EqualTo(1));
            Assert.That(model[0].PlatformCode, Is.EqualTo("TPlat"));
        }

        [Test]
        public async void ManagePlatforms_AddingPlatforms_ReturnsUpdatedModel()
        {
            List<Platform> platforms = new List<Platform>
            {
                new Platform
                {
                    PlatformCode = "TPlat",
                    PlatformName = "Test Platform"
                },
                new Platform
                {
                    PlatformCode = "2Plat",
                    PlatformName = "Second Platform"
                }
            };

            List<string> platformStrings = new List<string>
            {
                "TPlat",
                "2Plat"
            };

            Member member = new Member
            {
                UserId = memberId,
                FavoritePlatforms = new List<Platform>
                {
                    platforms[0]
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(
                platforms.AsQueryable());
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            Mock<DbSet<Member>> memberDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            ManageController controller = new ManageController(
                userManager: null, signInManager: null,
                veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object, stripeService: null)
            {
                ControllerContext = context.Object
            };

            await controller.ManagePlatforms(platformStrings);

            Assert.That(member.FavoritePlatforms.Count, Is.EqualTo(2));
            Assert.That(member.FavoritePlatforms.Any(p => p.PlatformCode == "TPlat"));
            Assert.That(member.FavoritePlatforms.Any(p => p.PlatformCode == "2Plat"));
        }

        [Test]
        public async void ManagePlatforms_RemovePlatforms_ReturnsUpdatedModel()
        {
            List<Platform> platforms = new List<Platform>
            {
                new Platform
                {
                    PlatformCode = "TPlat",
                    PlatformName = "Test Platform"
                },
                new Platform
                {
                    PlatformCode = "2Plat",
                    PlatformName = "Second Platform"
                }
            };

            List<string> platformStrings = new List<string>
            {
                "TPlat"
            };

            Member member = new Member
            {
                UserId = memberId,
                FavoritePlatforms = new List<Platform>
                {
                    platforms[0],
                    platforms[1]
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(
                platforms.AsQueryable());
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            Mock<DbSet<Member>> memberDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            ManageController controller = new ManageController(
                userManager: null, signInManager: null,
                veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object, stripeService: null)
            {
                ControllerContext = context.Object
            };

            await controller.ManagePlatforms(platformStrings);

            Assert.That(member.FavoritePlatforms.Count, Is.EqualTo(1));
            Assert.That(member.FavoritePlatforms.Any(p => p.PlatformCode == "TPlat"));
            Assert.That(member.FavoritePlatforms.All(p => p.PlatformCode != "2Plat"));
        }

        [Test]
        public async void ManagePlatforms_NullClearsPlatforms_ReturnsUpdatedModel()
        {
            List<Platform> platforms = new List<Platform>
            {
                new Platform
                {
                    PlatformCode = "TPlat",
                    PlatformName = "Test Platform"
                },
                new Platform
                {
                    PlatformCode = "2Plat",
                    PlatformName = "Second Platform"
                }
            };

            Member member = new Member
            {
                UserId = memberId,
                FavoritePlatforms = platforms
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(
                platforms.AsQueryable());
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            Mock<DbSet<Member>> memberDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            ManageController controller = new ManageController(
                userManager: null, signInManager: null,
                veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object, stripeService: null)
            {
                ControllerContext = context.Object
            };

            await controller.ManagePlatforms(null);

            Assert.That(member.FavoritePlatforms.Count, Is.EqualTo(0));
        }
    }
}