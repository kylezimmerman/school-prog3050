using System;
using System.Collections.Generic;
using NUnit.Framework;
using Veil.DataModels.Models;

namespace Veil.Tests.Controllers.GamesControllerTests
{
    [TestFixture]
    public abstract class GamesControllerTestsBase
    {
        protected PhysicalGameProduct notForSaleSKU;
        protected PhysicalGameProduct availableSKU;
        protected PhysicalGameProduct preOrderSKU;
        protected PhysicalGameProduct discontinuedSKU;

        protected Guid Id;

        protected Tag tag;
        protected ESRBRating everyoneESRBRating;

        protected const string TITLE_FRAGMENT_COMMON_TO_ALL_SEARCH_GAMES = "atch";

        [SetUp]
        public void Setup()
        {
            notForSaleSKU = new PhysicalGameProduct
            {
                ProductAvailabilityStatus = AvailabilityStatus.NotForSale
            };

            availableSKU = new PhysicalGameProduct
            {
                ProductAvailabilityStatus = AvailabilityStatus.Available
            };

            preOrderSKU = new PhysicalGameProduct
            {
                ProductAvailabilityStatus = AvailabilityStatus.PreOrder
            };

            discontinuedSKU = new PhysicalGameProduct
            {
                ProductAvailabilityStatus = AvailabilityStatus.DiscontinuedByManufacturer
            };

            Id = new Guid("44B0752E-998B-466A-AAAD-3ED535BA3559");

            tag = new Tag { Name = "Test" };

            everyoneESRBRating = new ESRBRating { RatingId = "E", Description = "Everyone" };
        }

        protected List<GameProduct> GetGameSKUsListWithAllAvailabilityStatuses()
        {
            return new List<GameProduct>
            {
                notForSaleSKU,
                availableSKU,
                preOrderSKU,
                discontinuedSKU
            };
        }

        protected List<Game> GetGameSearchList()
        {
            return new List<Game>
            {
                new Game
                {
                    GameAvailabilityStatus = AvailabilityStatus.NotForSale,
                    Name = "No Match NotForSale",
                    GameSKUs = GetGameSKUsListWithAllAvailabilityStatuses(),
                    Tags = GetTagList()
                },
                new Game
                {
                    GameAvailabilityStatus = AvailabilityStatus.Available,
                    Name = "Batch Available",
                    GameSKUs = GetGameSKUsListWithAllAvailabilityStatuses(),
                    Tags = new List<Tag>()
                },
                new Game
                {
                    GameAvailabilityStatus = AvailabilityStatus.PreOrder,
                    Name = "Game Match PreOrder",
                    GameSKUs = GetGameSKUsListWithAllAvailabilityStatuses(),
                    Tags = new List<Tag>()
                },
                new Game
                {
                    GameAvailabilityStatus = AvailabilityStatus.DiscontinuedByManufacturer,
                    Name = "Title Patch DiscontinuedByManufacturer",
                    GameSKUs = GetGameSKUsListWithAllAvailabilityStatuses(),
                    Tags = GetTagList()
                }
            };
        }

        protected List<Tag> GetTagList()
        {
            return new List<Tag>
            {
                new Tag { Name = "Shooter" },
                new Tag { Name = "Simulation" },
                new Tag { Name = "RPG" },
                new Tag { Name = "3D" }
            };
        }
        protected List<Platform> GetPlatformList()
        {
            return new List<Platform>
            {
                new Platform { PlatformCode = "XONE", PlatformName = "Xbox One" },
                new Platform { PlatformCode = "PS4", PlatformName = "PlayStation 4" },
                new Platform { PlatformCode = "WIIU", PlatformName = "Wii U" },
                new Platform { PlatformCode = "PC", PlatformName = "PC" }
            };
        }
    }
}
