using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Principal;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels;
using Veil.DataModels.Models;
using Veil.Helpers;
using Veil.Models;
using Veil.Tests.Controllers.GameProductsControllerTests;

namespace Veil.Tests.Controllers.GamesControllerTests
{
    public class RecommendedTests : GameProductsControllerTestsBase
    {
        [Test]
        public async void Recommended_NoFavorites_ReturnsMatchingModel()
        {
            Member member = new Member
            {
                UserId = new Guid("09EABF21-D5AC-4A5D-ADF8-27180E6D889B"),
                FavoriteTags = new List<Tag>(),
                FavoritePlatforms = new List<Platform>()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            GamesController controller = new GamesController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.Recommended() as RedirectToRouteResult;

            Assert.That(result.RouteValues["action"], Is.EqualTo("Index"));
        }

        [Test]
        public async void Recommended_NoResults_ReturnsMatchingModel()
        {
            List<Game> games = new List<Game>
            {
                new Game
                {
                    Name = "FirstGame",
                    GameSKUs = new List<GameProduct>
                    {
                        new PhysicalGameProduct
                        {
                            PlatformCode = "PS4"
                        }
                    },
                    GameAvailabilityStatus = AvailabilityStatus.Available
                }
            };

            games[0].GameSKUs.First().Game = games[0];

            List<Tag> tags = new List<Tag>
            {
                new Tag
                {
                    Name = "Shooter",
                    TaggedGames = new List<Game>
                    {
                        games[0]
                    }
                },
                new Tag
                {
                    Name = "3D",
                    TaggedGames = new List<Game>
                    {
                        games[0]
                    }
                },
                new Tag
                {
                    Name = "2D",
                    TaggedGames = new List<Game>()
                }
            };

            games[0].Tags = new List<Tag>
            {
                tags[0],
                tags[1]
            };

            List<Platform> platforms = new List<Platform>
            {
                new Platform
                {
                    PlatformCode = "PS4",
                    PlatformName = "Playstation 4",
                    GameProducts = new List<GameProduct>
                    {
                        games[0].GameSKUs.First()
                    }
                }
            };

            Member member = new Member
            {
                UserId = new Guid("09EABF21-D5AC-4A5D-ADF8-27180E6D889B"),
                FavoriteTags = new List<Tag>
                {
                    tags[2]
                },
                FavoritePlatforms = new List<Platform>(),
                WebOrders = new List<WebOrder>()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            GamesController controller = new GamesController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.Recommended() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            Assert.That(model.Games.Count, Is.EqualTo(0));
        }

        [Test]
        public async void Recommended_ReturnsMatchingModel()
        {
            List<Game> games = new List<Game>
            {
                new Game
                {
                    Name = "FirstGame",
                    GameSKUs = new List<GameProduct>
                    {
                        new PhysicalGameProduct
                        {
                            PlatformCode = "PS4"
                        }
                    },
                    GameAvailabilityStatus = AvailabilityStatus.Available
                },
                new Game
                {
                    Name = "SecondGame",
                    GameSKUs = new List<GameProduct>
                    {
                        new PhysicalGameProduct
                        {
                            PlatformCode = "PC"
                        }
                    },
                    GameAvailabilityStatus = AvailabilityStatus.Available
                },
                new Game
                {
                    Name = "ThirdGame",
                    GameSKUs = new List<GameProduct>
                    {
                        new PhysicalGameProduct
                        {
                            PlatformCode = "XONE",
                            ReleaseDate = DateTime.Today.AddDays(-1)
                        }
                    },
                    GameAvailabilityStatus = AvailabilityStatus.Available
                },
                new Game
                {
                    Name = "FourthGame",
                    GameSKUs = new List<GameProduct>
                    {
                        new PhysicalGameProduct
                        {
                            PlatformCode = "XONE",
                            ReleaseDate = DateTime.Today.AddDays(-2)
                        }
                    },
                    GameAvailabilityStatus = AvailabilityStatus.Available
                },
                new Game
                {
                    Name = "FifthGame",
                    GameSKUs = new List<GameProduct>
                    {
                        new PhysicalGameProduct
                        {
                            PlatformCode = "PS4"
                        }
                    },
                    GameAvailabilityStatus = AvailabilityStatus.Available
                },
                new Game
                {
                    Name = "PurchasedGame",
                    GameSKUs = new List<GameProduct>
                    {
                        new PhysicalGameProduct
                        {
                            PlatformCode = "PS4"
                        }
                    },
                    GameAvailabilityStatus = AvailabilityStatus.Available
                },
                new Game
                {
                    Name = "NotForSaleGame",
                    GameSKUs = new List<GameProduct>
                    {
                        new PhysicalGameProduct
                        {
                            PlatformCode = "PS4"
                        }
                    },
                    GameAvailabilityStatus = AvailabilityStatus.NotForSale
                }
            };

            games[0].GameSKUs.First().Game = games[0];
            games[1].GameSKUs.First().Game = games[1];
            games[2].GameSKUs.First().Game = games[2];
            games[3].GameSKUs.First().Game = games[3];
            games[4].GameSKUs.First().Game = games[4];

            List<Tag> tags = new List<Tag>
            {
                new Tag
                {
                    Name = "Shooter",
                    TaggedGames = new List<Game>
                    {
                        games[0],
                        games[2],
                        games[3]
                    }
                },
                new Tag
                {
                    Name = "3D",
                    TaggedGames = new List<Game>
                    {
                        games[0],
                        games[1],
                        games[2],
                        games[3]
                    }
                },
                new Tag
                {
                    Name = "2D",
                    TaggedGames = new List<Game>
                    {
                        games[1],
                        games[3],
                        games[4]
                    }
                }
            };

            games[0].Tags = new List<Tag>
            {
                tags[0],
                tags[1]
            };
            games[1].Tags = new List<Tag>
            {
                tags[1],
                tags[2]
            };
            games[2].Tags = new List<Tag>
            {
                tags[0],
                tags[1],
                tags[2]
            };
            games[3].Tags = new List<Tag>
            {
                tags[0],
                tags[1],
                tags[2]
            };
            games[4].Tags = new List<Tag>
            {
                tags[2]
            };

            List<Platform> platforms = new List<Platform>
            {
                new Platform
                {
                    PlatformCode = "XONE",
                    PlatformName = "Xbox ONE",
                    GameProducts = new List<GameProduct>
                    {
                        games[2].GameSKUs.First(),
                        games[3].GameSKUs.First()
                    }
                },
                new Platform
                {
                    PlatformCode = "PC",
                    PlatformName = "PC",
                    GameProducts = new List<GameProduct>
                    {
                        games[0].GameSKUs.First(),
                        games[4].GameSKUs.First()
                    }
                },
                new Platform
                {
                    PlatformCode = "PS4",
                    PlatformName = "Playstation 4",
                    GameProducts = new List<GameProduct>
                    {
                        games[1].GameSKUs.First()
                    }
                }
            };

            Member member = new Member
            {
                UserId = new Guid("09EABF21-D5AC-4A5D-ADF8-27180E6D889B"),
                FavoriteTags = new List<Tag>
                {
                    tags[0],
                    tags[1]
                },
                FavoritePlatforms = new List<Platform>
                {
                    platforms[1],
                    platforms[2]
                },
                WebOrders = new List<WebOrder>
                {
                    new WebOrder
                    {
                        OrderItems = new List<OrderItem>
                        {
                            new OrderItem
                            {
                                Product = games[6].GameSKUs.First()
                            }
                        }
                    }
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            GamesController controller = new GamesController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.Recommended() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<GameListViewModel>());

            var model = (GameListViewModel)result.Model;

            var modelGames = model.Games.ToList();

            Assert.That(modelGames.Count, Is.EqualTo(5));
            Assert.That(modelGames[0].Name, Is.EqualTo("FirstGame"));
            Assert.That(modelGames[1].Name, Is.EqualTo("SecondGame"));
            Assert.That(modelGames[2].Name, Is.EqualTo("ThirdGame"));
            Assert.That(modelGames[3].Name, Is.EqualTo("FourthGame"));
            Assert.That(modelGames[4].Name, Is.EqualTo("FifthGame"));
            Assert.That(modelGames.All(g => g.Name != "PurchasedGame"));
            Assert.That(modelGames.All(g => g.Name != "NotForSaleGame"));
        }
    }
}
