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
using Veil.DataModels.Models;
using Veil.Helpers;
using Veil.Models;

namespace Veil.Tests.Controllers.GameProductsControllerTests
{
    public class RenderPhysicalGameProductPartialTests : GameProductsControllerTestsBase
    {
        [Test]
        public void RenderPhysicalGameProductPartial_NullUser_ReturnsPartialViewWithFalseModelProperties()
        {
            PhysicalGameProduct gameProduct = new PhysicalGameProduct();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Member>> memberDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(new List<Member>().AsQueryable());
            memberDbSetStub.
                Setup(mdb => mdb.Find(It.IsAny<Guid>())).
                Returns<Member>(null);

            dbStub.
                Setup(db => db.Members).
                Returns(memberDbSetStub.Object);

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(Guid.Empty);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.
                Setup(c => c.HttpContext.User.Identity).
                Returns<IIdentity>(null);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = controller.RenderPhysicalGameProductPartial(gameProduct);

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<PhysicalGameProductViewModel>());

            var model = (PhysicalGameProductViewModel) result.Model;

            Assert.That(model.GameProduct, Is.SameAs(gameProduct));
            Assert.That(model.NewIsInCart, Is.False);
            Assert.That(model.UsedIsInCart, Is.False);
            Assert.That(model.ProductIsOnWishlist, Is.False);
        }

        [Test]
        public void RenderPhysicalGameProductPartial_UserWithAllInCart_ReturnsPartialViewWithTrueModelProperties()
        {
            PhysicalGameProduct gameProduct = new PhysicalGameProduct
            {
                Id = GameSKUId
            };

            Member member = new Member
            {
                UserId = new Guid("1901C42C-1094-456C-9EAA-87DDC7AFEEC8"),
                Cart = new Cart
                {
                    Items = new List<CartItem>
                    {
                        new CartItem
                        {
                            ProductId = gameProduct.Id,
                            IsNew = true
                        },
                        new CartItem
                        {
                            ProductId = gameProduct.Id,
                            IsNew = false
                        }
                    }
                },
                Wishlist = new List<Product>
                {
                    gameProduct
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Member>> memberDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.
                Setup(mdb => mdb.Find(member.UserId)).
                Returns(member);

            dbStub.
                Setup(db => db.Members).
                Returns(memberDbSetStub.Object);

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(member.UserId);

            Mock<ControllerContext> contextStub = new Mock<ControllerContext>();
            contextStub.
                Setup(c => c.HttpContext.User.Identity).
                Returns<IIdentity>(null);

            GameProductsController controller = new GameProductsController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = contextStub.Object
            };

            var result = controller.RenderPhysicalGameProductPartial(gameProduct);

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<PhysicalGameProductViewModel>());

            var model = (PhysicalGameProductViewModel)result.Model;

            Assert.That(model.GameProduct, Is.SameAs(gameProduct));
            Assert.That(model.NewIsInCart, Is.True);
            Assert.That(model.UsedIsInCart, Is.True);
            Assert.That(model.ProductIsOnWishlist, Is.True);
        }
    }
}
