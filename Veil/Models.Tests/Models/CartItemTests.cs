using System;
using NUnit.Framework;
using Veil.DataModels.Models;

namespace Veil.DataModels.Tests.Models
{
    [TestFixture]
    public class CartItemTests
    {
        [Test]
        public void Comparer_SameInstance_ReturnsTrue()
        {
            CartItem item = new CartItem();

            bool result = CartItem.CartItemComparer.Equals(item, item);

            Assert.That(result, Is.True);
        }

        [Test]
        public void Comparer_OneNullOneNot_ReturnsFalse()
        {
            CartItem item = new CartItem();

            bool result = CartItem.CartItemComparer.Equals(item, null);

            Assert.That(result, Is.False);

            result = CartItem.CartItemComparer.Equals(null, item);

            Assert.That(result, Is.False);
        }

        [Test]
        public void Comparer_DifferentInstancesWithSameValues_ReturnsTrue()
        {
            Guid memberId = new Guid("3E522B6E-9F75-4E9A-BB11-52CE00827977");
            Guid productId = new Guid("6C7133F7-143A-4CF1-90C5-8661A4AC6B87");
            int quantity = 4;
            bool isNew = true;

            CartItem item1 = new CartItem
            {
                IsNew = isNew,
                MemberId = memberId,
                ProductId = productId,
                Quantity = quantity
            };
            CartItem item2 = new CartItem
            {
                IsNew = isNew,
                MemberId = memberId,
                ProductId = productId,
                Quantity = quantity
            };

            bool result = CartItem.CartItemComparer.Equals(item1, item2);

            Assert.That(result, Is.True);
        }

        [Test]
        public void GetHashCode_SameInstance_ReturnsSameValue()
        {
            Guid memberId = new Guid("3E522B6E-9F75-4E9A-BB11-52CE00827977");
            Guid productId = new Guid("6C7133F7-143A-4CF1-90C5-8661A4AC6B87");
            int quantity = 4;
            bool isNew = true;

            CartItem item = new CartItem
            {
                IsNew = isNew,
                MemberId = memberId,
                ProductId = productId,
                Quantity = quantity
            };

            int result1 = CartItem.CartItemComparer.GetHashCode(item);
            int result2 = CartItem.CartItemComparer.GetHashCode(item);

            Assert.That(result1, Is.EqualTo(result2));
        }

        [Test]
        public void GetHashCode_DifferentInstancesWithSameValues_ReturnsSameValueForBoth()
        {
            Guid memberId = new Guid("3E522B6E-9F75-4E9A-BB11-52CE00827977");
            Guid productId = new Guid("6C7133F7-143A-4CF1-90C5-8661A4AC6B87");
            int quantity = 4;
            bool isNew = false;

            CartItem item1 = new CartItem
            {
                IsNew = isNew,
                MemberId = memberId,
                ProductId = productId,
                Quantity = quantity
            };
            CartItem item2 = new CartItem
            {
                IsNew = isNew,
                MemberId = memberId,
                ProductId = productId,
                Quantity = quantity
            };

            int result1 = CartItem.CartItemComparer.GetHashCode(item1);
            int result2 = CartItem.CartItemComparer.GetHashCode(item2);

            Assert.That(result1, Is.EqualTo(result2));
        }
    }
}
