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
using Veil.DataModels.Models;
using Veil.Models.Reports;

namespace Veil.Tests.Controllers.ReportsControllerTests
{
    public class WishlistTests : ReportsControllerTestsBase
    {
        [Test]
        public async void Wishlist_GameWithNoProducts_IsNotInModel()
        {
            var game = new Game
            {
                Id = new Guid("09BD7D30-5268-4A02-A400-1F302040B6AA"),
                GameSKUs = new List<GameProduct>()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { game }.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Platform>().AsQueryable());
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member>().AsQueryable());
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.Wishlist() as ViewResult;

            Assert.That(result != null);

            var model = (WishlistViewModel)result.Model;

            Assert.That(model.Games.Count(), Is.EqualTo(0));
            Assert.That(model.WishlistCount, Is.EqualTo(0));
        }

        [Test]
        public async void Wishlist_GameWithProducts_NotWishlisted_IsNotInModel()
        {
            var game = new Game
            {
                Id = new Guid("09BD7D30-5268-4A02-A400-1F302040B6AA"),
                GameSKUs = new List<GameProduct>
                {
                    new PhysicalGameProduct
                    {
                        Id = new Guid("D9825FE1-F575-4C36-ACAA-98BA71FACFA8")
                    }
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { game }.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Platform>().AsQueryable());
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member>().AsQueryable());
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.Wishlist() as ViewResult;

            Assert.That(result != null);

            var model = (WishlistViewModel)result.Model;

            Assert.That(model.Games.Count(), Is.EqualTo(0));
            Assert.That(model.WishlistCount, Is.EqualTo(0));
        }

        [Test]
        public async void Wishlist_GameWithProducts_Wishlisted_IsInModel_SubtotalsMatch()
        {
            var platforms = new List<Platform>
            {
                new Platform
                {
                    PlatformCode = "TPlat",
                    PlatformName = "Test Platform"
                },
                new Platform
                {
                    PlatformCode = "OTPlat",
                    PlatformName = "Other Test Platform"
                },
            };

            List<GameProduct> products = new List<GameProduct>
            {
                new PhysicalGameProduct
                {
                    Id = new Guid("D9825FE1-F575-4C36-ACAA-98BA71FACFA8"),
                    Platform = platforms[0]
                },
                new PhysicalGameProduct
                {
                    Id = new Guid("F546AD43-58C8-44FE-9799-ACC7F0B6368E"),
                    Platform = platforms[1]
                }
            };

            var game = new Game
            {
                Id = new Guid("09BD7D30-5268-4A02-A400-1F302040B6AA"),
                GameSKUs = products
            };
            
            platforms[0].GameProducts = new List<GameProduct>
            {
                products[0]
            };
            platforms[1].GameProducts = new List<GameProduct>
            {
                products[1]
            };

            var members = new List<Member>
            {
                new Member
                {
                    UserId = new Guid("1369019C-8F25-427A-A8F1-00287247D4AE"),
                    Wishlist = new List<Product>
                    {
                        products[0]
                    }
                },
                new Member
                {
                    UserId = new Guid("D814D26E-AD33-48C5-9518-EA43B75AE5AC"),
                    Wishlist = new List<Product>
                    {
                        products[0],
                        products[1]
                    }
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { game }.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(platforms.AsQueryable());
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(members.AsQueryable());
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.Wishlist() as ViewResult;

            Assert.That(result != null);

            var model = (WishlistViewModel)result.Model;

            Assert.That(model.Games.Count(), Is.EqualTo(1));
            Assert.That(model.Games.First().WishlistCount, Is.EqualTo(3));

            Assert.That(model.Platforms.First(p =>
                p.Platform.PlatformCode == "TPlat").WishlistCount, Is.EqualTo(2));
            Assert.That(model.Platforms.First(p =>
                p.Platform.PlatformCode == "OTPlat").WishlistCount, Is.EqualTo(1));

            Assert.That(model.WishlistCount, Is.EqualTo(3));
        }

        [Test]
        public void WishlistDetail_GameIdIsNull_Throws404Exception()
        {
            var controller = new ReportsController(null);

            Assert.That(async () => await controller.WishlistDetail(null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public void WishlistDetail_GameIsNotFound_Throws404Exception()
        {
            var platforms = new List<Platform>
            {
                new Platform
                {
                    PlatformCode = "TPlat",
                    PlatformName = "Test Platform"
                },
                new Platform
                {
                    PlatformCode = "OTPlat",
                    PlatformName = "Other Test Platform"
                },
            };

            List<GameProduct> products = new List<GameProduct>
            {
                new PhysicalGameProduct
                {
                    Id = new Guid("D9825FE1-F575-4C36-ACAA-98BA71FACFA8"),
                    Platform = platforms[0]
                },
                new PhysicalGameProduct
                {
                    Id = new Guid("F546AD43-58C8-44FE-9799-ACC7F0B6368E"),
                    Platform = platforms[1]
                }
            };

            var game = new Game
            {
                Id = new Guid("09BD7D30-5268-4A02-A400-1F302040B6AA"),
                GameSKUs = products
            };

            platforms[0].GameProducts = new List<GameProduct>
            {
                products[0]
            };
            platforms[1].GameProducts = new List<GameProduct>
            {
                products[1]
            };

            var members = new List<Member>
            {
                new Member
                {
                    UserId = new Guid("1369019C-8F25-427A-A8F1-00287247D4AE"),
                    Wishlist = new List<Product>
                    {
                        products[0]
                    }
                },
                new Member
                {
                    UserId = new Guid("D814D26E-AD33-48C5-9518-EA43B75AE5AC"),
                    Wishlist = new List<Product>
                    {
                        products[0],
                        products[1]
                    }
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { game }.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(platforms.AsQueryable());
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(members.AsQueryable());
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            ReportsController controller = new ReportsController(dbStub.Object);

            Assert.That(async () => await controller.WishlistDetail(new Guid("9E073E71-1A1E-4B51-A4CA-AE820BEBEA91")), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void WishlistDetail_GameWithProducts_Wishlisted_IsInModel()
        {
            var platforms = new List<Platform>
            {
                new Platform
                {
                    PlatformCode = "TPlat",
                    PlatformName = "Test Platform"
                },
                new Platform
                {
                    PlatformCode = "OTPlat",
                    PlatformName = "Other Test Platform"
                },
            };

            List<GameProduct> products = new List<GameProduct>
            {
                new PhysicalGameProduct
                {
                    Id = new Guid("D9825FE1-F575-4C36-ACAA-98BA71FACFA8"),
                    Platform = platforms[0]
                },
                new PhysicalGameProduct
                {
                    Id = new Guid("F546AD43-58C8-44FE-9799-ACC7F0B6368E"),
                    Platform = platforms[1]
                }
            };

            var game = new Game
            {
                Id = new Guid("09BD7D30-5268-4A02-A400-1F302040B6AA"),
                GameSKUs = products
            };

            platforms[0].GameProducts = new List<GameProduct>
            {
                products[0]
            };
            platforms[1].GameProducts = new List<GameProduct>
            {
                products[1]
            };

            var members = new List<Member>
            {
                new Member
                {
                    UserId = new Guid("1369019C-8F25-427A-A8F1-00287247D4AE"),
                    Wishlist = new List<Product>
                    {
                        products[0]
                    }
                },
                new Member
                {
                    UserId = new Guid("D814D26E-AD33-48C5-9518-EA43B75AE5AC"),
                    Wishlist = new List<Product>
                    {
                        products[0],
                        products[1]
                    }
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Game>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Game> { game }.AsQueryable());
            dbStub.Setup(db => db.Games).Returns(gameDbSetStub.Object);

            Mock<DbSet<Platform>> platformDbSetStub = TestHelpers.GetFakeAsyncDbSet(platforms.AsQueryable());
            dbStub.Setup(db => db.Platforms).Returns(platformDbSetStub.Object);

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(members.AsQueryable());
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            ReportsController controller = new ReportsController(dbStub.Object);

            var result = await controller.WishlistDetail(game.Id) as ViewResult;

            Assert.That(result != null);

            var model = (WishlistDetailGameViewModel)result.Model;

            Assert.That(model.GameProducts.Count(), Is.EqualTo(2));
            Assert.That(model.GameProducts.First().WishlistCount, Is.EqualTo(2));
            Assert.That(model.GameProducts.Last().WishlistCount, Is.EqualTo(1));
            Assert.That(model.WishlistCount, Is.EqualTo(3));
        }
    }
}