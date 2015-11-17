using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Helpers;

namespace Veil.Tests.Controllers
{
    [TestFixture]
    class CartControllerTests
    {
        private Guid Id;
        private Guid UserId;
        private Guid GameId;

        [SetUp]
        public void Setup()
        {
            Id = new Guid("45B0752E-998B-466A-AAAD-3ED535BA3559");
            UserId = new Guid("09EABF21-D5AC-4A5D-ADF8-27180E6D889B");
            GameId = new Guid("EFBCB640-388B-E511-80DF-001CD8B71DA6");
        }

        [Test]
        public async void Index_EmptyCart_ReturnsMatchingModel()
        {
            Cart cart = new Cart
            {
                MemberId = UserId,
                Items = new List<CartItem>()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Cart>> cartDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Cart> { cart }.AsQueryable());
            cartDbSetStub.Setup(db => db.FindAsync(cart.MemberId)).ReturnsAsync(cart);
            dbStub.Setup(db => db.Carts).Returns(cartDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(UserId);

            CartController controller = new CartController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.Index() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<Cart>());

            var model = (Cart)result.Model;

            Assert.That(model.Items.Count, Is.EqualTo(0));
        }

        [Test]
        public async void Index_ItemsInCart_ReturnsMatchingModel()
        {
            PhysicalGameProduct gameProduct = new PhysicalGameProduct()
            {
                Id = Id,
                BoxArtImageURL = "boxart",
                NewWebPrice = 12m,
                UsedWebPrice = 8m,
                Platform = new Platform
                {
                    PlatformName = "XBAX",
                }
            };

            Cart cart = new Cart
            {
                MemberId = UserId,
                Items = new List<CartItem>
                {
                    new CartItem
                    {
                        Product = gameProduct,
                        ProductId = gameProduct.Id,
                        IsNew = true,
                        MemberId = UserId,
                        Quantity = 1
                    },
                    new CartItem
                    {
                        Product = gameProduct,
                        ProductId = gameProduct.Id,
                        IsNew = false,
                        MemberId = UserId,
                        Quantity = 2
                    }
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Cart>> cartDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Cart> { cart }.AsQueryable());
            cartDbSetStub.Setup(db => db.FindAsync(cart.MemberId)).ReturnsAsync(cart);
            dbStub.Setup(db => db.Carts).Returns(cartDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(UserId);

            CartController controller = new CartController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.Index() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<Cart>());

            var model = (Cart)result.Model;

            Assert.That(model.Items.Count, Is.EqualTo(2));
            Assert.That(model.Items.FirstOrDefault(i => i.IsNew).ProductId, Is.EqualTo(Id));
            Assert.That(model.Items.FirstOrDefault(i => !i.IsNew).ProductId, Is.EqualTo(Id));
            Assert.That(model.Items.FirstOrDefault(i => i.IsNew).Quantity, Is.EqualTo(1));
            Assert.That(model.Items.FirstOrDefault(i => !i.IsNew).Quantity, Is.EqualTo(2));
        }

        [Test]
        public async void UpdateQuantity_ProductIdIsNull_RedirectsToIndex()
        {
            CartController controller = new CartController(veilDataAccess: null, idGetter: null);

            var result = await controller.UpdateQuantity(null, true, 1) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Index"));
        }

        [Test]
        public async void UpdateQuantity_IsNewIsNull_RedirectsToIndex()
        {
            CartController controller = new CartController(veilDataAccess: null, idGetter: null);

            var result = await controller.UpdateQuantity(Id, null, 1) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Index"));
        }

        [TestCase(null)]
        [TestCase(-1)]
        public async void UpdateQuantity_InvalidQuantity_RedirectsToIndex(int? quantity)
        {
            CartController controller = new CartController(veilDataAccess: null, idGetter: null);

            var result = await controller.UpdateQuantity(Id, true, quantity) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Index"));
        }

        [Test]
        public async void UpdateQuantity_0Quantity_RemovesFromCart()
        {
            CartController controller = new CartController(veilDataAccess: null, idGetter: null);

            var result = await controller.UpdateQuantity(Id, true, 0) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("RemoveItem"));
        }

        [Test]
        public void UpdateQuantity_ItemIsNotInCart_Throws404Exception()
        {
            Guid gameProductId = new Guid("976ACE77-D87C-4EBE-83A0-46F911F6490E");

            PhysicalGameProduct gameProduct = new PhysicalGameProduct()
            {
                Id = Id,
                BoxArtImageURL = "boxart",
                NewWebPrice = 12m,
                UsedWebPrice = 8m,
                Platform = new Platform
                {
                    PlatformName = "XBAX",
                }
            };

            Cart cart = new Cart
            {
                MemberId = UserId,
                Items = new List<CartItem>
                {
                    new CartItem
                    {
                        Product = gameProduct,
                        ProductId = gameProduct.Id,
                        IsNew = true,
                        MemberId = UserId,
                        Quantity = 1
                    }
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Cart>> cartDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Cart> { cart }.AsQueryable());
            cartDbSetStub.Setup(db => db.FindAsync(cart.MemberId)).ReturnsAsync(cart);
            dbStub.Setup(db => db.Carts).Returns(cartDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(UserId);

            CartController controller = new CartController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            Assert.That(async () => await controller.UpdateQuantity(gameProductId, true, 2), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void UpdateQuantity_ItemIsInCart_EnoughInventory_ReturnsUpdatedModel()
        {
            PhysicalGameProduct gameProduct = new PhysicalGameProduct()
            {
                Id = Id,
                BoxArtImageURL = "boxart",
                SKUNameSuffix = "GameProductName",
                Game = new Game
                {
                    Name = "GameName"
                },
                NewWebPrice = 12m,
                UsedWebPrice = 8m,
                Platform = new Platform
                {
                    PlatformName = "XBAX",
                },
                LocationInventories = new List<ProductLocationInventory>
                {
                    new ProductLocationInventory
                    {
                        NewOnHand = 5,
                        UsedOnHand = 5
                    }
                }
            };

            Cart cart = new Cart
            {
                MemberId = UserId,
                Items = new List<CartItem>
                {
                    new CartItem
                    {
                        Product = gameProduct,
                        ProductId = gameProduct.Id,
                        IsNew = true,
                        MemberId = UserId,
                        Quantity = 1
                    }
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Cart>> cartDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Cart> { cart }.AsQueryable());
            cartDbSetStub.Setup(db => db.FindAsync(cart.MemberId)).ReturnsAsync(cart);
            dbStub.Setup(db => db.Carts).Returns(cartDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(UserId);

            CartController controller = new CartController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.UpdateQuantity(gameProduct.Id, true, 4) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<Cart>());

            var model = (Cart)result.Model;

            Assert.That(model.Items.Count, Is.EqualTo(1));
            Assert.That(model.Items.FirstOrDefault().Quantity, Is.EqualTo(4));
        }

        [Test]
        public async void UpdateQuantity_ItemIsInCart_NotEnoughNewInventory_ReturnsUpdatedModel()
        {
            PhysicalGameProduct gameProduct = new PhysicalGameProduct()
            {
                Id = Id,
                BoxArtImageURL = "boxart",
                SKUNameSuffix = "GameProductName",
                Game = new Game
                {
                    Name = "GameName"
                },
                NewWebPrice = 12m,
                UsedWebPrice = 8m,
                Platform = new Platform
                {
                    PlatformName = "XBAX",
                },
                LocationInventories = new List<ProductLocationInventory>
                {
                    new ProductLocationInventory
                    {
                        NewOnHand = 5,
                        UsedOnHand = 2
                    }
                }
            };

            Cart cart = new Cart
            {
                MemberId = UserId,
                Items = new List<CartItem>
                {
                    new CartItem
                    {
                        Product = gameProduct,
                        ProductId = gameProduct.Id,
                        IsNew = true,
                        MemberId = UserId,
                        Quantity = 1
                    }
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Cart>> cartDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Cart> { cart }.AsQueryable());
            cartDbSetStub.Setup(db => db.FindAsync(cart.MemberId)).ReturnsAsync(cart);
            dbStub.Setup(db => db.Carts).Returns(cartDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(UserId);

            CartController controller = new CartController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.UpdateQuantity(gameProduct.Id, true, 8) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<Cart>());

            var model = (Cart)result.Model;

            Assert.That(model.Items.Count, Is.EqualTo(1));
            Assert.That(model.Items.FirstOrDefault().Quantity, Is.EqualTo(8));
        }

        [Test]
        public async void UpdateQuantity_ItemIsInCart_NotEnoughUsedInventory_ReturnsUpdatedModel()
        {
            PhysicalGameProduct gameProduct = new PhysicalGameProduct()
            {
                Id = Id,
                BoxArtImageURL = "boxart",
                SKUNameSuffix = "GameProductName",
                Game = new Game
                {
                    Name = "GameName"
                },
                NewWebPrice = 12m,
                UsedWebPrice = 8m,
                Platform = new Platform
                {
                    PlatformName = "XBAX",
                },
                LocationInventories = new List<ProductLocationInventory>
                {
                    new ProductLocationInventory
                    {
                        NewOnHand = 5,
                        UsedOnHand = 2
                    }
                }
            };

            Cart cart = new Cart
            {
                MemberId = UserId,
                Items = new List<CartItem>
                {
                    new CartItem
                    {
                        Product = gameProduct,
                        ProductId = gameProduct.Id,
                        IsNew = false,
                        MemberId = UserId,
                        Quantity = 1
                    }
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Cart>> cartDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Cart> { cart }.AsQueryable());
            cartDbSetStub.Setup(db => db.FindAsync(cart.MemberId)).ReturnsAsync(cart);
            dbStub.Setup(db => db.Carts).Returns(cartDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(UserId);

            CartController controller = new CartController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.UpdateQuantity(gameProduct.Id, false, 4) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<Cart>());

            var model = (Cart)result.Model;

            Assert.That(model.Items.Count, Is.EqualTo(1));
            Assert.That(model.Items.FirstOrDefault().Quantity, Is.EqualTo(2));
        }

        [Test]
        public async void AddItem_ValidAdd()
        {
            Game game = new Game()
            {
                Id = GameId,
                Name = "game"
            };

            GameProduct gameProduct = new PhysicalGameProduct()
            {
                Id = Id,
                BoxArtImageURL = "boxart",
                NewWebPrice = 79.99m,
                UsedWebPrice = 44.99m,
                Platform = new Platform
                {
                    PlatformName = "PS4",
                },
                Game = game
            };

            Cart cart = new Cart
            {
                MemberId = UserId,
                Items = new List<CartItem>(),
            };

            Member member = new Member()
            {
                UserId = UserId,
                Cart = cart
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Cart>> cartDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Cart> { cart }.AsQueryable());
            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> {member}.AsQueryable());
            Mock<DbSet<GameProduct>> gameProductDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> {gameProduct}.AsQueryable());

            cartDbSetStub.Setup(db => db.FindAsync(cart.MemberId)).ReturnsAsync(cart);
            gameProductDbSetStub.Setup(db => db.FindAsync(gameProduct.Id)).ReturnsAsync(gameProduct);
            memberDbSetStub.Setup(db => db.Find(member.UserId)).Returns(member);
            gameProductDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Carts).Returns(cartDbSetStub.Object);
            dbStub.Setup(db => db.GameProducts).Returns(gameProductDbSetStub.Object);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<HttpSessionStateBase> session = new Mock<HttpSessionStateBase>();
            context.Setup(s => s.HttpContext.Session).Returns(session.Object);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(UserId);

            CartController controller = new CartController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.AddItem(gameProduct.Id, true) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));
        }

        [Test]
        public void AddItem_NullId()
        {
            CartController controller = new CartController(null, null);

            Assert.That(async () => await controller.AddItem(null, true), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public void Additem_IdNotInDb()
        {
            Game game = new Game()
            {
                Id = GameId,
                Name = "game"
            };

            GameProduct gameProduct = new PhysicalGameProduct()
            {
                Id = Id,
                BoxArtImageURL = "boxart",
                NewWebPrice = 79.99m,
                UsedWebPrice = 44.99m,
                Platform = new Platform
                {
                    PlatformName = "PS4",
                },
                Game = game,
                GameId = game.Id
            };

            Cart cart = new Cart
            {
                MemberId = UserId,
                Items = new List<CartItem>(),
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Cart>> cartDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Cart> { cart }.AsQueryable());
            Mock<DbSet<GameProduct>> gameProductDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> { gameProduct }.AsQueryable());

            cartDbSetStub.Setup(db => db.FindAsync(cart.MemberId)).ReturnsAsync(cart);
            gameProductDbSetStub.Setup(db => db.FindAsync(gameProduct.Id)).ReturnsAsync(gameProduct);
            gameProductDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Carts).Returns(cartDbSetStub.Object);
            dbStub.Setup(db => db.GameProducts).Returns(gameProductDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<HttpSessionStateBase> session = new Mock<HttpSessionStateBase>();
            context.Setup(s => s.HttpContext.Session).Returns(session.Object);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(UserId);

            CartController controller = new CartController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            Guid nonMatch = new Guid("45B0752E-998B-477A-AAAD-3ED535BA3559");

            Assert.That(async () => await controller.AddItem(nonMatch, true), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public void AddItem_NullCart()
        {
            Game game = new Game()
            {
                Id = GameId,
                Name = "game"
            };

            GameProduct gameProduct = new PhysicalGameProduct()
            {
                Id = Id,
                BoxArtImageURL = "boxart",
                NewWebPrice = 79.99m,
                UsedWebPrice = 44.99m,
                Platform = new Platform
                {
                    PlatformName = "PS4",
                },
                Game = game
            };

            Cart cart = new Cart
            {
                MemberId = new Guid("45B0752E-998B-477A-AAAD-3ED535BA3559"),
                Items = new List<CartItem>(),
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Cart>> cartDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Cart> { cart }.AsQueryable());
            Mock<DbSet<GameProduct>> gameProductDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> { gameProduct }.AsQueryable());

            cartDbSetStub.Setup(db => db.FindAsync(cart.MemberId)).ReturnsAsync(cart);
            gameProductDbSetStub.Setup(db => db.FindAsync(gameProduct.Id)).ReturnsAsync(gameProduct);
            gameProductDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Carts).Returns(cartDbSetStub.Object);
            dbStub.Setup(db => db.GameProducts).Returns(gameProductDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(UserId);

            CartController controller = new CartController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            Assert.That(async () => await controller.AddItem(gameProduct.Id, true), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void AddItem_CatchesOnSave()
        {
            Game game = new Game()
            {
                Id = GameId,
                Name = "game"
            };

            GameProduct gameProduct = new PhysicalGameProduct()
            {
                Id = Id,
                BoxArtImageURL = "boxart",
                NewWebPrice = 79.99m,
                UsedWebPrice = 44.99m,
                Platform = new Platform
                {
                    PlatformName = "PS4",
                },
                Game = game
            };

            Cart cart = new Cart
            {
                MemberId = UserId,
                Items = new List<CartItem>(),
            };

            Member member = new Member()
            {
                UserId = UserId,
                Cart = cart
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Cart>> cartDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Cart> { cart }.AsQueryable());
            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            Mock<DbSet<GameProduct>> gameProductDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> { gameProduct }.AsQueryable());

            cartDbSetStub.Setup(db => db.FindAsync(cart.MemberId)).ReturnsAsync(cart);
            gameProductDbSetStub.Setup(db => db.FindAsync(gameProduct.Id)).ReturnsAsync(gameProduct);
            memberDbSetStub.Setup(db => db.Find(member.UserId)).Returns(member);
            gameProductDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Carts).Returns(cartDbSetStub.Object);
            dbStub.Setup(db => db.GameProducts).Returns(gameProductDbSetStub.Object);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);
            dbStub.Setup(db => db.SaveChangesAsync()).Throws<DbUpdateException>();

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(UserId);

            CartController controller = new CartController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.AddItem(gameProduct.Id, true) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));
        }

        [Test]
        public async void RemoveItem_ValidRemove()
        {
            Game game = new Game()
            {
                Id = GameId,
                Name = "game"
            };

            GameProduct gameProduct = new PhysicalGameProduct()
            {
                Id = Id,
                BoxArtImageURL = "boxart",
                NewWebPrice = 79.99m,
                UsedWebPrice = 44.99m,
                Platform = new Platform
                {
                    PlatformName = "PS4",
                },
                Game = game
            };

            CartItem cartItem = new CartItem()
            {
                ProductId = gameProduct.Id,
                IsNew = true
            };

            Cart cart = new Cart
            {
                MemberId = UserId,
                Items = new List<CartItem>()
                {
                    cartItem        
                }
            };

            Member member = new Member()
            {
                UserId = UserId,
                Cart = cart
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Cart>> cartDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Cart> { cart }.AsQueryable());
            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            Mock<DbSet<GameProduct>> gameProductDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> { gameProduct }.AsQueryable());

            cartDbSetStub.Setup(db => db.FindAsync(cart.MemberId)).ReturnsAsync(cart);
            gameProductDbSetStub.Setup(db => db.FindAsync(gameProduct.Id)).ReturnsAsync(gameProduct);
            memberDbSetStub.Setup(db => db.Find(member.UserId)).Returns(member);
            gameProductDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Carts).Returns(cartDbSetStub.Object);
            dbStub.Setup(db => db.GameProducts).Returns(gameProductDbSetStub.Object);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<HttpSessionStateBase> session = new Mock<HttpSessionStateBase>();
            context.Setup(s => s.HttpContext.Session).Returns(session.Object);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(UserId);

            CartController controller = new CartController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.RemoveItem(gameProduct.Id, cartItem.IsNew) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Index"));
        }

        [Test]
        public void RemoveItem_NullId()
        {
            CartController controller = new CartController(null, null);

            Assert.That(async () => await controller.RemoveItem(null, true), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public void RemoveItem_NullCart()
        {
            Cart cart = new Cart
            {
                MemberId = new Guid("45B0752E-998B-477A-AAAD-3ED535BA3559"),
                Items = new List<CartItem>(),
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Cart>> cartDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Cart> { cart }.AsQueryable());

            cartDbSetStub.Setup(db => db.FindAsync(cart.MemberId)).ReturnsAsync(cart);

            dbStub.Setup(db => db.Carts).Returns(cartDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(UserId);

            CartController controller = new CartController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            Assert.That(async () => await controller.RemoveItem(Id, true), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void RemoveItem_CatchesOnSave()
        {
            Game game = new Game()
            {
                Id = GameId,
                Name = "game"
            };

            GameProduct gameProduct = new PhysicalGameProduct()
            {
                Id = Id,
                BoxArtImageURL = "boxart",
                NewWebPrice = 79.99m,
                UsedWebPrice = 44.99m,
                Platform = new Platform
                {
                    PlatformName = "PS4",
                },
                Game = game
            };

            CartItem cartItem = new CartItem()
            {
                ProductId = gameProduct.Id
            };

            Cart cart = new Cart
            {
                MemberId = UserId,
                Items = new List<CartItem>()
                {
                    cartItem
                }
            };

            Member member = new Member()
            {
                UserId = UserId,
                Cart = cart
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Cart>> cartDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Cart> { cart }.AsQueryable());
            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            Mock<DbSet<GameProduct>> gameProductDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(new List<GameProduct> { gameProduct }.AsQueryable());

            cartDbSetStub.Setup(db => db.FindAsync(cart.MemberId)).ReturnsAsync(cart);
            gameProductDbSetStub.Setup(db => db.FindAsync(gameProduct.Id)).ReturnsAsync(gameProduct);
            memberDbSetStub.Setup(db => db.Find(member.UserId)).Returns(member);
            gameProductDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Carts).Returns(cartDbSetStub.Object);
            dbStub.Setup(db => db.GameProducts).Returns(gameProductDbSetStub.Object);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);
            dbStub.Setup(db => db.SaveChangesAsync()).Throws<DbUpdateException>();

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<HttpSessionStateBase> session = new Mock<HttpSessionStateBase>();
            context.Setup(s => s.HttpContext.Session).Returns(session.Object);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(UserId);

            CartController controller = new CartController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.RemoveItem(gameProduct.Id, cartItem.IsNew) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["action"], Is.EqualTo("Index"));
        }
    }
}
