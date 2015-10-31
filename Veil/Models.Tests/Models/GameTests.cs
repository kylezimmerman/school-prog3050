using System.Collections.Generic;
using NUnit.Framework;
using Veil.DataModels.Models;

namespace Veil.DataModels.Tests.Models
{
    [TestFixture]
    public class GameTests
    {
        [Test]
        public void GameAvailabilityStatus_NullGameSKUs_DoesNotThrow()
        {
            var game = new Game();

            Assert.That(() => game.GameAvailabilityStatus, Throws.Nothing);
            // Act
        }

        [Test]
        public void GameAvailabilityStatus_NullGameSKUs_ReturnsNotForSale()
        {
            var game = new Game();

            Assert.That(game.GameAvailabilityStatus, Is.EqualTo(AvailabilityStatus.NotForSale));
        }

        [Test]
        public void GameAvailabilityStatus_EmptyGameSKUs_ReturnsNotForSale()
        {
            var game = new Game
            {
                GameSKUs = new List<GameProduct>()
            };

            Assert.That(game.GameAvailabilityStatus, Is.EqualTo(AvailabilityStatus.NotForSale));
        }

        [Test]
        public void GameAvailabilityStatus_OnlyDiscontinuedSKUs_ReturnsDiscontinuedByManufacturer()
        {
            var game = new Game
            {
                GameSKUs = new List<GameProduct>
                {
                    new PhysicalGameProduct
                    {
                        ProductAvailabilityStatus = AvailabilityStatus.DiscontinuedByManufacturer
                    }
                }
            };

            Assert.That(game.GameAvailabilityStatus, Is.EqualTo(AvailabilityStatus.DiscontinuedByManufacturer));
        }

        [Test]
        public void GameAvailabilityStatus_OnlyAvailableSKUs_ReturnsAvailable()
        {
            var game = new Game
            {
                GameSKUs = new List<GameProduct>
                {
                    new PhysicalGameProduct
                    {
                        ProductAvailabilityStatus = AvailabilityStatus.Available
                    }
                }
            };

            Assert.That(game.GameAvailabilityStatus, Is.EqualTo(AvailabilityStatus.Available));
        }

        [Test]
        public void GameAvailabilityStatus_OnlyPreOrderSKUs_ReturnsPreOrder()
        {
            var game = new Game
            {
                GameSKUs = new List<GameProduct>
                {
                    new PhysicalGameProduct
                    {
                        ProductAvailabilityStatus = AvailabilityStatus.PreOrder
                    }
                }
            };

            Assert.That(game.GameAvailabilityStatus, Is.EqualTo(AvailabilityStatus.PreOrder));
        }

        [Test]
        public void GameAvailabilityStatus_OnlyNotForSaleSKUs_ReturnsNotForSale()
        {
            var game = new Game
            {
                GameSKUs = new List<GameProduct>
                {
                    new PhysicalGameProduct
                    {
                        ProductAvailabilityStatus = AvailabilityStatus.NotForSale
                    }
                }
            };

            Assert.That(game.GameAvailabilityStatus, Is.EqualTo(AvailabilityStatus.NotForSale));
        }

        [Test]
        public void GameAvailabilityStatus_PreOrderAndAvailableSKUs_ReturnsPreOrder()
        {
            var game = new Game
            {
                GameSKUs = new List<GameProduct>
                {
                    new PhysicalGameProduct
                    {
                        ProductAvailabilityStatus = AvailabilityStatus.Available
                    },
                    new PhysicalGameProduct
                    {
                        ProductAvailabilityStatus = AvailabilityStatus.PreOrder
                    }
                }
            };

            Assert.That(game.GameAvailabilityStatus, Is.EqualTo(AvailabilityStatus.PreOrder));
        }

        [Test]
        public void GameAvailabilityStatus_PreOrderAvailableDiscontinuedByManufacturerSKUs_ReturnsPreOrder()
        {
            var game = new Game
            {
                GameSKUs = new List<GameProduct>
                {
                    new PhysicalGameProduct
                    {
                        ProductAvailabilityStatus = AvailabilityStatus.Available
                    },
                    new PhysicalGameProduct
                    {
                        ProductAvailabilityStatus = AvailabilityStatus.DiscontinuedByManufacturer
                    },
                    new PhysicalGameProduct
                    {
                        ProductAvailabilityStatus = AvailabilityStatus.PreOrder
                    }
                }
            };

            Assert.That(game.GameAvailabilityStatus, Is.EqualTo(AvailabilityStatus.PreOrder));
        }

        [Test]
        public void GameAvailabilityStatus_OneOfEachStatus_ReturnsPreOrder()
        {
            var game = new Game
            {
                GameSKUs = new List<GameProduct>
                {
                    new PhysicalGameProduct
                    {
                        ProductAvailabilityStatus = AvailabilityStatus.Available
                    },
                    new PhysicalGameProduct
                    {
                        ProductAvailabilityStatus = AvailabilityStatus.DiscontinuedByManufacturer
                    },
                    new PhysicalGameProduct
                    {
                        ProductAvailabilityStatus = AvailabilityStatus.NotForSale
                    },
                    new PhysicalGameProduct
                    {
                        ProductAvailabilityStatus = AvailabilityStatus.PreOrder
                    }
                }
            };

            Assert.That(game.GameAvailabilityStatus, Is.EqualTo(AvailabilityStatus.PreOrder));
        }

        [Test]
        public void GameAvailabilityStatus_AvailableAndDiscontinuedByManufacturerSKUs_ReturnsAvailable()
        {
            var game = new Game
            {
                GameSKUs = new List<GameProduct>
                {
                    new PhysicalGameProduct
                    {
                        ProductAvailabilityStatus = AvailabilityStatus.Available
                    },
                    new PhysicalGameProduct
                    {
                        ProductAvailabilityStatus = AvailabilityStatus.DiscontinuedByManufacturer
                    }
                }
            };

            Assert.That(game.GameAvailabilityStatus, Is.EqualTo(AvailabilityStatus.Available));
        }

        [Test]
        public void GameAvailabilityStatus_AvailableDiscontinuedByManufacturerAndNotForSaleSKUs_ReturnsAvailable()
        {
            var game = new Game
            {
                GameSKUs = new List<GameProduct>
                {
                    new PhysicalGameProduct
                    {
                        ProductAvailabilityStatus = AvailabilityStatus.Available
                    },
                    new PhysicalGameProduct
                    {
                        ProductAvailabilityStatus = AvailabilityStatus.DiscontinuedByManufacturer
                    },
                    new PhysicalGameProduct
                    {
                        ProductAvailabilityStatus = AvailabilityStatus.NotForSale
                    }
                }
            };

            Assert.That(game.GameAvailabilityStatus, Is.EqualTo(AvailabilityStatus.Available));
        }

        [Test]
        public void GameAvailabilityStatus_DiscontinuedByManufacturerAndNotForSaleSKUs_ReturnsDiscontinuedByManufacturer()
        {
            var game = new Game
            {
                GameSKUs = new List<GameProduct>
                {
                    new PhysicalGameProduct
                    {
                        ProductAvailabilityStatus = AvailabilityStatus.DiscontinuedByManufacturer
                    },
                    new PhysicalGameProduct
                    {
                        ProductAvailabilityStatus = AvailabilityStatus.NotForSale
                    }
                }
            };

            Assert.That(game.GameAvailabilityStatus, Is.EqualTo(AvailabilityStatus.DiscontinuedByManufacturer));
        }
    }
}
