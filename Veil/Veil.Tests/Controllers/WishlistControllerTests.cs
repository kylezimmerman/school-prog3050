using System;
using System.Collections.Generic;
using System.Data.Entity;
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
using Veil.Models;

namespace Veil.Tests.Controllers
{
    [TestFixture]
    class WishlistControllerTests
    {
        private Guid Id;
        private Guid UserId;

        [SetUp]
        public void Setup()
        {
            Id = new Guid("45B0752E-998B-466A-AAAD-3ED535BA3559");
            UserId = new Guid("09EABF21-D5AC-4A5D-ADF8-27180E6D889B");
        }

        [Test]
        public void Index_AnonymousUser_PrivateWishlist_Throws404Exception()
        {
            Member member = new Member
            {
                UserAccount = new DataModels.Models.Identity.User
                {
                    UserName = "TestUser"
                },
                WishListVisibility = WishListVisibility.Private
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.Find(member.UserId)).Returns(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(false);

            WishlistController controller = new WishlistController(dbStub.Object, idGetter: null)
            {
                ControllerContext = context.Object
            };

            Assert.That(async () => await controller.Index(member.UserAccount.UserName), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void Index_AnonymousUser_PublicWishlist_ReturnsMatchingModel()
        {
            Member member = new Member
            {
                UserAccount = new DataModels.Models.Identity.User
                {
                    UserName = "TestUser"
                },
                UserId = UserId,
                WishListVisibility = WishListVisibility.Public,
                Wishlist = new List<Product>()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(false);

            WishlistController controller = new WishlistController(dbStub.Object, idGetter: null)
            {
                ControllerContext = context.Object
            };

            var result = await controller.Index(member.UserAccount.UserName) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<Member>());

            var model = (Member)result.Model;

            Assert.That(model.Wishlist, Is.EqualTo(member.Wishlist));
        }

        [Test]
        public async void Index_CurrentMemberWishlist_ReturnsMatchingModel()
        {
            Member member = new Member
            {
                UserId = UserId
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            WishlistController controller = new WishlistController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.Index(null) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<Member>());

            var model = (Member)result.Model;

            Assert.That(model.UserId, Is.EqualTo(UserId));
        }

        [Test]
        public void Index_WishlistOwnerNotFound_Throws404Exception()
        {
            Member member = new Member
            {
                UserAccount = new DataModels.Models.Identity.User
                {
                    UserName = "TestUser"
                },
                WishListVisibility = WishListVisibility.Private
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.Find(member.UserId)).Returns(member);
            memberDbSetStub.Setup(db => db.FindAsync(Id)).ReturnsAsync(null);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            WishlistController controller = new WishlistController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            Assert.That(async () => await controller.Index("NotTestUser"), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void Index_WishlistOwnerFound_Public_ReturnsMatchingModel()
        {
            Member wishlistOwner = new Member
            {
                UserAccount = new DataModels.Models.Identity.User
                {
                    UserName = "TestUser"
                },
                UserId = Id,
                WishListVisibility = WishListVisibility.Public
            };

            Member currentMember = new Member
            {
                UserId = UserId
            };

            List<Member> members = new List<Member>
            {
                wishlistOwner,
                currentMember
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(members.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(currentMember.UserId)).ReturnsAsync(currentMember);
            memberDbSetStub.Setup(db => db.FindAsync(wishlistOwner.UserId)).ReturnsAsync(wishlistOwner);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(currentMember.UserId);

            WishlistController controller = new WishlistController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.Index(wishlistOwner.UserAccount.UserName) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<Member>());

            var model = (Member)result.Model;

            Assert.That(model.UserId, Is.EqualTo(wishlistOwner.UserId));
        }

        [Test]
        public void Index_WishlistOwnerFound_Private_Throws404Exception()
        {
            Member wishlistOwner = new Member
            {
                UserAccount = new DataModels.Models.Identity.User
                {
                    UserName = "TestUser"
                },
                UserId = Id,
                WishListVisibility = WishListVisibility.Private
            };

            Member currentMember = new Member
            {
                UserId = UserId
            };

            List<Member> members = new List<Member>
            {
                wishlistOwner,
                currentMember
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(members.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(currentMember.UserId)).ReturnsAsync(currentMember);
            memberDbSetStub.Setup(db => db.FindAsync(wishlistOwner.UserId)).ReturnsAsync(wishlistOwner);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(currentMember.UserId);

            WishlistController controller = new WishlistController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            Assert.That(async () => await controller.Index(wishlistOwner.UserAccount.UserName), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void Index_WishlistOwnerFound_FriendsOnly_NotFriends_Throws404Exception()
        {
            Member wishlistOwner = new Member
            {
                UserAccount = new DataModels.Models.Identity.User
                {
                    UserName = "TestUser"
                },
                UserId = Id,
                WishListVisibility = WishListVisibility.FriendsOnly,
                RequestedFriendships = new List<Friendship>(),
                ReceivedFriendships = new List<Friendship>(),
            };

            Member currentMember = new Member
            {
                UserId = UserId
            };

            List<Member> members = new List<Member>
            {
                wishlistOwner,
                currentMember
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(members.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(currentMember.UserId)).ReturnsAsync(currentMember);
            memberDbSetStub.Setup(db => db.FindAsync(wishlistOwner.UserId)).ReturnsAsync(wishlistOwner);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(currentMember.UserId);

            WishlistController controller = new WishlistController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.Index(wishlistOwner.UserAccount.UserName) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["controller"], Is.EqualTo("FriendList"));
            Assert.That(result.RouteValues["action"], Is.EqualTo("Index"));
        }

        [Test]
        public void RenderPhysicalGameProduct_CurrentUserIsMember()
        {
            Guid gameProductId = new Guid("976ACE77-D87C-4EBE-83A0-46F911F6490E");
            PhysicalGameProduct gameProduct = new PhysicalGameProduct()
            {
                Id = gameProductId
            };

            Member wishlistOwner = new Member
            {
                UserId = Id,
                WishListVisibility = WishListVisibility.FriendsOnly,
                RequestedFriendships = new List<Friendship>(),
                ReceivedFriendships = new List<Friendship>(),
                UserAccount = new DataModels.Models.Identity.User()
                {
                    UserName = "WishlistOwnerName"
                }
            };

            Member currentMember = new Member
            {
                UserId = UserId,
                Wishlist = new List<Product>
                {
                    gameProduct
                },
                Cart = new Cart
                {
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
                            Quantity = 1
                        }
                    }
                }
            };

            List<Member> members = new List<Member>
            {
                wishlistOwner,
                currentMember
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(members.AsQueryable());
            memberDbSetStub.Setup(db => db.Find(currentMember.UserId)).Returns(currentMember);
            memberDbSetStub.Setup(db => db.Find(wishlistOwner.UserId)).Returns(wishlistOwner);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(currentMember.UserId);

            WishlistController controller = new WishlistController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            var result = controller.RenderPhysicalGameProduct(gameProduct, wishlistOwner.UserId) as PartialViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<WishlistPhysicalGameProductViewModel>());

            var model = (WishlistPhysicalGameProductViewModel)result.Model;

            Assert.That(model.GameProduct.Id, Is.EqualTo(gameProduct.Id));
            Assert.That(model.NewIsInCart);
            Assert.That(model.UsedIsInCart);
            Assert.That(model.ProductIsOnWishlist);
            Assert.That(!model.MemberIsCurrentUser);
        }

        [Test]
        public void Add_NullId_Throws404Exception()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Product>> productDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Product>().AsQueryable());

            dbStub.Setup(db => db.Products).Returns(productDbSetStub.Object);

            WishlistController controller = new WishlistController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.Add(null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public void Add_IdNotInDb_Throws404Exception()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Product>> productDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Product>().AsQueryable());

            dbStub.Setup(db => db.Products).Returns(productDbSetStub.Object);

            WishlistController controller = new WishlistController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.Add(Id), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async void Add_IdInDb_ReturnsViewWithMemberAsModel(bool itemAlreadyInWishlist)
        {
            Guid gameProductId = new Guid("976ACE77-D87C-4EBE-83A0-46F911F6490E");
            PhysicalGameProduct gameProduct = new PhysicalGameProduct()
            {
                Game = new Game()
                {
                    Name = "TestGame"
                },
                SKUNameSuffix = "TestGameProductName",
                Id = gameProductId
            };

            Member currentMember = new Member
            {
                UserId = UserId,
                Wishlist = new List<Product>()
            };

            if (itemAlreadyInWishlist)
            {
                currentMember.Wishlist.Add(gameProduct);
            }

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Product>> productDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Product>().AsQueryable());
            productDbSetStub.Setup(db => db.FindAsync(gameProduct.Id)).ReturnsAsync(gameProduct);
            dbStub.Setup(db => db.Products).Returns(productDbSetStub.Object);

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { currentMember }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(currentMember.UserId)).ReturnsAsync(currentMember);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(currentMember.UserId);

            WishlistController controller = new WishlistController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.Add(gameProduct.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<Member>());

            var model = (Member)result.Model;

            Assert.That(model.UserId, Is.EqualTo(currentMember.UserId));
            Assert.That(model.Wishlist.Contains(gameProduct));
        }

        [Test]
        public void Remove_NullId_Throws404Exception()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Product>> productDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Product>().AsQueryable());

            dbStub.Setup(db => db.Products).Returns(productDbSetStub.Object);

            WishlistController controller = new WishlistController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.Remove(null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public void Remove_IdNotInDb_Throws404Exception()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Product>> productDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Product>().AsQueryable());

            dbStub.Setup(db => db.Products).Returns(productDbSetStub.Object);

            WishlistController controller = new WishlistController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.Remove(Id), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async void Remove_IdInDb_ReturnsViewWithMemberAsModel(bool itemAlreadyInWishlist)
        {
            Guid gameProductId = new Guid("976ACE77-D87C-4EBE-83A0-46F911F6490E");
            PhysicalGameProduct gameProduct = new PhysicalGameProduct()
            {
                Game = new Game()
                {
                    Name = "TestGame"
                },
                SKUNameSuffix = "TestGameProductName",
                Id = gameProductId
            };

            Member currentMember = new Member
            {
                UserId = UserId,
                Wishlist = new List<Product>()
            };

            if (itemAlreadyInWishlist)
            {
                currentMember.Wishlist.Add(gameProduct);
            }

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Product>> productDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Product>().AsQueryable());
            productDbSetStub.Setup(db => db.FindAsync(gameProduct.Id)).ReturnsAsync(gameProduct);
            dbStub.Setup(db => db.Products).Returns(productDbSetStub.Object);

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { currentMember }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(currentMember.UserId)).ReturnsAsync(currentMember);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(currentMember.UserId);

            WishlistController controller = new WishlistController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.Remove(gameProduct.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<Member>());

            var model = (Member)result.Model;

            Assert.That(model.UserId, Is.EqualTo(currentMember.UserId));
            Assert.That(model.Wishlist.Contains(gameProduct), Is.False);
        }
    }
}
