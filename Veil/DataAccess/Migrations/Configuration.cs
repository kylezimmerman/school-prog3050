using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Transactions;
using EfEnumToLookup.LookupGenerator;
using Veil.DataModels;
using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;

[assembly: InternalsVisibleTo("Veil.DataAccess.Tests")]
namespace Veil.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;

    internal sealed class Configuration : DbMigrationsConfiguration<VeilDataContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            ContextKey = "Veil.DataAccess.VeilDataContext";
        }

        protected override void Seed(VeilDataContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data.

            context.ESRBRatings.AddOrUpdate(
                er => er.RatingId,
                new ESRBRating
                {
                    RatingId = "EC",
                    Description = "Early Childhood",
                    ImageURL = "https://esrbstorage.blob.core.windows.net/esrbcontent/images/ratingsymbol_ec.png",
                    MinimumAge = 0
                },
                new ESRBRating
                {
                    RatingId = "E",
                    Description = "Everyone",
                    ImageURL = "https://esrbstorage.blob.core.windows.net/esrbcontent/images/ratingsymbol_e.png",
                    MinimumAge = 0
                },
                new ESRBRating
                {
                    RatingId = "E10+",
                    Description = "Everyone 10+",
                    ImageURL = "https://esrbstorage.blob.core.windows.net/esrbcontent/images/ratingsymbol_e10.png",
                    MinimumAge = 10
                },
                new ESRBRating
                {
                    RatingId = "T",
                    Description = "Teen",
                    ImageURL = "https://esrbstorage.blob.core.windows.net/esrbcontent/images/ratingsymbol_t.png",
                    MinimumAge = 13
                },
                new ESRBRating
                {
                    RatingId = "M",
                    Description = "Mature",
                    ImageURL = "https://esrbstorage.blob.core.windows.net/esrbcontent/images/ratingsymbol_m.png",
                    MinimumAge = 17
                },
                new ESRBRating
                {
                    RatingId = "AO",
                    Description = "Adults Only",
                    ImageURL = "https://esrbstorage.blob.core.windows.net/esrbcontent/images/ratingsymbol_ao.png",
                    MinimumAge = 18
                },
                new ESRBRating
                {
                    RatingId = "RP",
                    Description = "Rating Pending",
                    ImageURL = "https://esrbstorage.blob.core.windows.net/esrbcontent/images/ratingsymbol_rp.png",
                    MinimumAge = 0
                }
            );

            int ecdId = 0;

            context.ESRBContentDescriptors.AddOrUpdate(
                ecd => ecd.Id,
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Alcohol Reference" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Animated Blood" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Blood" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Blood and Gore" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Cartoon Violence" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Comic Mischief" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Crude Humor" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Drug Reference" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Fantasy Violence" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Intense Violence" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Language" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Lyrics" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Mature Humor" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Nudity" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Partial Nudity" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Real Gambling" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Sexual Content" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Sexual Themes" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Sexual Violence" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Simulated Gambling" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Strong Language" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Strong Lyrics" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Strong Sexual Content" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Suggestive Themes" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Tobacco Reference" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Use of Alcohol" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Use of Drugs" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Use of Tobacco" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Violence" },
                new ESRBContentDescriptor { Id = ++ecdId, DescriptorName = "Violent References" }
            );

            context.Countries.AddOrUpdate(
                c => c.CountryCode,
                new Country
                {
                    CountryCode = "CA",
                    CountryName = "Canada",
                    FederalTaxAcronym = "GST",
                    FederalTaxRate = 0.05m
                },
                new Country
                {
                    CountryCode = "US",
                    CountryName = "United States of America",
                    FederalTaxRate = 0
                }
            );

            context.Provinces.AddOrUpdate(
                p => new { p.ProvinceCode, p.CountryCode },
                // Canadian Provinces and Territories
                new Province
                {
                    CountryCode = "CA",
                    ProvinceCode = "AB",
                    Name = "Alberta",
                    ProvincialTaxRate = 0
                },
                new Province
                {
                    CountryCode = "CA",
                    ProvinceCode = "BC",
                    Name = "British Columbia",
                    ProvincialTaxAcronym = "PST",
                    ProvincialTaxRate = 0.07m
                },
                new Province
                {
                    CountryCode = "CA",
                    ProvinceCode = "MB",
                    Name = "Manitoba",
                    ProvincialTaxAcronym = "PST",
                    ProvincialTaxRate = 0.08m
                },
                new Province
                {
                    CountryCode = "CA",
                    ProvinceCode = "NB",
                    Name = "New Brunswick",
                    ProvincialTaxAcronym = "HST",
                    ProvincialTaxRate = 0.08m
                },
                new Province
                {
                    CountryCode = "CA",
                    ProvinceCode = "NL",
                    Name = "Newfoundland and Labrador",
                    ProvincialTaxAcronym = "HST",
                    ProvincialTaxRate = 0.08m
                },
                new Province
                {
                    CountryCode = "CA",
                    ProvinceCode = "NT",
                    Name = "Northwest Territories",
                    ProvincialTaxRate = 0
                },
                new Province
                {
                    CountryCode = "CA",
                    ProvinceCode = "NS",
                    Name = "Nova Scotia",
                    ProvincialTaxAcronym = "HST",
                    ProvincialTaxRate = 0.10m
                },
                new Province
                {
                    CountryCode = "CA",
                    ProvinceCode = "NU",
                    Name = "Nunavut",
                    ProvincialTaxRate = 0
                },
                new Province
                {
                    CountryCode = "CA",
                    ProvinceCode = "ON",
                    Name = "Ontario",
                    ProvincialTaxAcronym = "HST",
                    ProvincialTaxRate = 0.08m
                },
                new Province
                {
                    CountryCode = "CA",
                    ProvinceCode = "PE",
                    Name = "Prince Edward Island",
                    ProvincialTaxAcronym = "HST",
                    ProvincialTaxRate = 0.09m
                },
                new Province
                {
                    CountryCode = "CA",
                    ProvinceCode = "QC",
                    Name = "Quebec",
                    ProvincialTaxAcronym = "PST",
                    ProvincialTaxRate = 0.09975m
                },
                new Province
                {
                    CountryCode = "CA",
                    ProvinceCode = "SK",
                    Name = "Saskatchewan",
                    ProvincialTaxAcronym = "PST",
                    ProvincialTaxRate = 0.05m
                },
                new Province
                {
                    CountryCode = "CA",
                    ProvinceCode = "YT",
                    Name = "Yukon",
                    ProvincialTaxRate = 0
                },

                // US States
                new Province
                {
                    CountryCode = "US",
                    ProvinceCode = "NY",
                    Name = "New York",
                    ProvincialTaxRate = 0
                }
            );

            context.LocationTypes.AddOrUpdate(
                lt => lt.LocationTypeName, 
                new LocationType
                {
                    LocationTypeName = "Office"
                },
                new LocationType
                {
                    LocationTypeName = "Store"
                }
            );

            Location onlineWarehouse = new Location
            {
                City = "Waterloo",
                CountryCode = "CA",
                LocationNumber = 1,
                LocationTypeName = "Office",
                PhoneNumber = "555-555-1199",
                SiteName = Location.ONLINE_WAREHOUSE_NAME,
                PostalCode = "N2L 6R2",
                ProvinceCode = "ON",
                StreetAddress = "123 Fake Way"
            };

            context.Locations.AddOrUpdate(
                l => l.LocationNumber,
                onlineWarehouse
            );

            int deptId = 0;

            context.Departments.AddOrUpdate(
                d => d.Id,
                new Department
                {
                    Id = ++deptId,
                    Name = "Retail Operations"
                },
                new Department
                {
                    Id = ++deptId,
                    Name = "Purchasing"
                },
                new Department
                {
                    Id = ++deptId,
                    Name = "Online Operations"
                }
            );

            Platform ps4 = new Platform
            {
                PlatformCode = "PS4",
                PlatformName = "PlayStation 4"
            };

            context.Platforms.AddOrUpdate(
                p => p.PlatformCode,
                new Platform
                {
                    PlatformCode = "PC",
                    PlatformName = "PC"
                }, 
                ps4, 
                new Platform
                {
                    PlatformCode = "XONE",
                    PlatformName = "Xbox One"
                },
                new Platform
                {
                    PlatformCode = "WIIU",
                    PlatformName = "Wii U"
                },
                new Platform
                {
                    PlatformCode = "PS3",
                    PlatformName = "PlayStation 3"
                }, 
                new Platform
                {
                    PlatformCode = "X360",
                    PlatformName = "Xbox 360"
                }
            );

            context.Companies.AddOrUpdate(
                c => c.Name,
                new Company { Name = "Activision Blizzard" },
                new Company { Name = "Electronic Arts" },
                new Company { Name = "Ubisoft" },
                new Company { Name = "Take-Two" },
                new Company { Name = "2K Games" },
                new Company { Name = "Blizzard Entertainment" },
                new Company { Name = "EA DICE" },
                new Company { Name = "Rockstar Games" },
                new Company { Name = "Nintendo" },
                new Company { Name = "Sony Computer Entertainment" },
                new Company { Name = "Microsoft Studios" },
                new Company { Name = "Bungie" },
                new Company { Name = "Treyarch" },
                new Company { Name = "Square Enix" },
                new Company { Name = "Konami" },
                new Company { Name = "Sega" },
                new Company { Name = "Bandai Namco" },
                new Company { Name = "Atari" },
                new Company { Name = "Capcom" },
                new Company { Name = "Codemasters" },
                new Company { Name = "D3" },
                new Company { Name = "1C" },
                new Company { Name = "Atlus" },
                new Company { Name = "Eidos" },
                new Company { Name = "WB Games" },
                new Company { Name = "Koei Tecmo" },
                new Company { Name = "Paradox" },
                new Company { Name = "SNK" },
                new Company { Name = "Rare" },
                new Company { Name = "Supergiant Games" },
                new Company { Name = "Gaijin Games" },
                new Company { Name = "Gearbox" },
                new Company { Name = "Criterion Games" },
                new Company { Name = "Valve" },
                new Company { Name = "Hello Games" },
                new Company { Name = "Klei Entertainment" },
                new Company { Name = "Squad" },
                new Company { Name = "Introversion" },
                new Company { Name = "Double Fine" },
                new Company { Name = "GSC Game World" },
                new Company { Name = "CD PROJEKT RED" },
                new Company { Name = "Croteam" },
                new Company { Name = "Nadeo" },
                new Company { Name = "Haemimont Games" },
                new Company { Name = "PopCap" },
                new Company { Name = "TellTale Games" },
                new Company { Name = "Crytek" },
                new Company { Name = "Robot Entertainment" },
                new Company { Name = "Firaxis Games" },
                new Company { Name = "Epic Games" },
                new Company { Name = "id Software" },
                new Company { Name = "Vlambeer" },
                new Company { Name = "3909" },
                new Company { Name = "Alexander Bruce" },
                new Company { Name = "Blendo Games" },
                new Company { Name = "Hidden Path Entertainment" },
                new Company { Name = "Bethesda Softworks" },
                new Company { Name = "Polytron Corporation" },
                new Company { Name = "The Fullbright Company" },
                new Company { Name = "IO Interactive" },
                new Company { Name = "Avalanche Studios" },
                new Company { Name = "Volition" },
                new Company { Name = "Mike Bithell" },
                new Company { Name = "Runic Games" },
                new Company { Name = "Frozen Byte" },
                new Company { Name = "ACE Team" },
                new Company { Name = "Zachtronics" },
                new Company { Name = "Subset Games" },
                new Company { Name = "Psyonix" },
                new Company { Name = "Monolith Productions" },
                new Company { Name = "Bioware" },
                new Company { Name = "Facepunch Studios" },
                new Company { Name = "Turn 10 Studios" },
                new Company { Name = "Lionhead Studios" },
                new Company { Name = "Fatshark" }
            );

            context.Tags.AddOrUpdate(
                t => t.Name,
                new Tag { Name = "First-Person" },
                new Tag { Name = "Third-Person" },
                new Tag { Name = "Shooter" },
                new Tag { Name = "Simulation" },
                new Tag { Name = "RTS" },
                new Tag { Name = "Driving/Racing" },
                new Tag { Name = "RPG" },
                new Tag { Name = "MMO" },
                new Tag { Name = "Action" },
                new Tag { Name = "Adventure" },
                new Tag { Name = "Side Scroller" },
                new Tag { Name = "2D" },
                new Tag { Name = "3D" },
                new Tag { Name = "Turn-Based" },
                new Tag { Name = "Roguelike" },
                new Tag { Name = "Open World" },
                new Tag { Name = "Baseball" },
                new Tag { Name = "Basketball" },
                new Tag { Name = "Billiards" },
                new Tag { Name = "Block-Breaking" },
                new Tag { Name = "Bowling" },
                new Tag { Name = "Boxing" },
                new Tag { Name = "Brawler" },
                new Tag { Name = "Card Game" },
                new Tag { Name = "Compilation" },
                new Tag { Name = "Cricket" },
                new Tag { Name = "Dual-Joystick Shooter" },
                new Tag { Name = "Educational" },
                new Tag { Name = "Fighting" },
                new Tag { Name = "Fishing" },
                new Tag { Name = "Fitness" },
                new Tag { Name = "Flight Simulator" },
                new Tag { Name = "Football" },
                new Tag { Name = "Gambling" },
                new Tag { Name = "Golf" },
                new Tag { Name = "Hockey" },
                new Tag { Name = "Light-Gun Shooter" },
                new Tag { Name = "Minigame Collection" },
                new Tag { Name = "MOBA" },
                new Tag { Name = "Music/Rhythm" },
                new Tag { Name = "Pinball" },
                new Tag { Name = "Platformer" },
                new Tag { Name = "Puzzle" },
                new Tag { Name = "Shoot 'Em Up" },
                new Tag { Name = "Skateboarding" },
                new Tag { Name = "Snowboarding/Skiing" },
                new Tag { Name = "Soccer" },
                new Tag { Name = "Sports" },
                new Tag { Name = "Strategy" },
                new Tag { Name = "Surfing" },
                new Tag { Name = "Tennis" },
                new Tag { Name = "Text Adventure" },
                new Tag { Name = "Track & Field" },
                new Tag { Name = "Trivia/Board Game" },
                new Tag { Name = "Vehicular Combat" },
                new Tag { Name = "Wrestling" }
            );

            context.Roles.AddOrUpdate(
                r => r.Id,
                new GuidIdentityRole
                {
                    Id = Guid.ParseExact("455b072e-de7d-e511-80df-001cd8b71da6", "D"),
                    Name = VeilRoles.ADMIN_ROLE
                },
                new GuidIdentityRole
                {
                    Id = Guid.ParseExact("465b072e-de7d-e511-80df-001cd8b71da6", "D"),
                    Name = VeilRoles.EMPLOYEE_ROLE
                },
                new GuidIdentityRole
                {
                    Id = Guid.ParseExact("475b072e-de7d-e511-80df-001cd8b71da6", "D"),
                    Name = VeilRoles.MEMBER_ROLE
                }
            );

    #region Debug Only Seed Values
            /* TODO: Remove this when we are done testing */
            Game halo5 = new Game
            {
                Name = "Halo 5: Guardians",
                ESRBRatingId = "T",
                ShortDescription = "An unstoppable force threatens the galaxy, and the Master Chief is missing.",
                LongDescription = "A mysterious and unstoppable force threatens the galaxy, the Master Chief is missing and his loyalty questioned. Experience the most dramatic Halo story to date in a 4-player cooperative epic that spans three worlds. Challenge friends and rivals in new multiplayer modes: Warzone, massive 24-player battles, and Arena, pure 4-vs-4 competitive combat.",
                MinimumPlayerCount = 1,
                MaximumPlayerCount = 24,
                PrimaryImageURL = "http://edge.alluremedia.com.au/m/k/2015/10/halo-1980x1080.jpg",
                TrailerURL = "https://www.youtube.com/embed/Rh_NXwqFvHc"
            };

            Game vermintide = new Game
            {
                Name = "Warhammer: End Times - Vermintide",
                ESRBRatingId = "M",
                LongDescription = "Vermintide takes place in and around Ubersreik, a city overrun by Skaven. You will assume the role of one of five heroes, each featuring different play-styles, abilities, gear and personality. Working cooperatively, you must use their individual attributes to survive an apocalyptic invasion from the hordes of relentless rat-men, known as the Skaven. Battles will take place across a range of environments stretching from the top of the Magnus Tower to the bowels of the Under Empire.",
                ShortDescription = "Vermintide is an epic co-operative action combat adventure set in the End Times of the iconic Warhammer Fantasy world.",
                MinimumPlayerCount = 1,
                MaximumPlayerCount = 4,
                PrimaryImageURL = "http://cdn.akamai.steamstatic.com/steam/apps/235540/header.jpg?t=1446475925",
                TrailerURL = "https://www.youtube.com/embed/KxTaQmhztVQ"
            };

            Game fallout4 = new Game
            {
                Name = "Fallout 4",
                ESRBRatingId = "M",
                LongDescription = "Bethesda Game Studios, the award-winning creators of Fallout 3 and The Elder Scrolls V: Skyrim, welcome you to the world of Fallout 4 – their most ambitious game ever, and the next generation of open-world gaming. As the sole survivor of Vault 111, you enter a world destroyed by nuclear war.Every second is a fight for survival, and every choice is yours.Only you can rebuild and determine the fate of the Wasteland.Welcome home.",
                ShortDescription = "Bethesda Game Studios welcome you to the world of Fallout 4 – their most ambitious game ever, and the next generation of open-world gaming.",
                MinimumPlayerCount = 1,
                MaximumPlayerCount = 1,
                PrimaryImageURL = "http://cdn.akamai.steamstatic.com/steam/apps/377160/header.jpg?t=1446248342",
                TrailerURL = "https://www.youtube.com/embed/k3IlHBBGCIw"
            };

            context.Games.AddOrUpdate(
                g => g.Name,
                halo5,
                vermintide,
                fallout4
            );

            context.SaveChanges();

            Tag shooterTag = context.Tags.Find("Shooter");
            Tag firstPerson = context.Tags.Find("First-Person");
            Tag thirdPerson = context.Tags.Find("Third-Person");

            halo5.Tags = halo5.Tags ?? new List<Tag>();
            vermintide.Tags = vermintide.Tags ?? new List<Tag>();
            fallout4.Tags = fallout4.Tags ?? new List<Tag>();

            halo5.Tags.Add(firstPerson);
            halo5.Tags.Add(shooterTag);
            vermintide.Tags.Add(shooterTag);
            vermintide.Tags.Add(firstPerson);
            fallout4.Tags.Add(shooterTag);
            fallout4.Tags.Add(thirdPerson);

            Company bungie = context.Companies.FirstOrDefault(c => c.Name == "Bungie");
            Company fatshark = context.Companies.FirstOrDefault(c => c.Name == "Fatshark");
            Company bethesda = context.Companies.FirstOrDefault(c => c.Name == "Bethesda Softworks");

            halo5 = context.Games.FirstOrDefault(g => g.Name == halo5.Name);
            vermintide = context.Games.FirstOrDefault(g => g.Name == vermintide.Name);
            fallout4 = context.Games.FirstOrDefault(g => g.Name == fallout4.Name);

            PhysicalGameProduct halo5SKU = new PhysicalGameProduct
            {
                DeveloperId = bungie.Id,
                PublisherId = bungie.Id,
                PlatformCode = "XONE",
                GameId = halo5.Id,
                InternalNewSKU = "0000000000001",
                InteralUsedSKU = "1000000000001",
                NewWebPrice = 59.99m,
                ReleaseDate = new DateTime(2015, 11, 01),
                ProductAvailabilityStatus = AvailabilityStatus.Available,
                WillBuyBackUsedCopy = true,
                UsedWebPrice = 50.00m,
                SKUNameSuffix = "Launch Edition",
                BoxArtImageURL = "http://compass.xbox.com/assets/fd/27/fd27fe56-fb0d-48af-aadf-b06c9a9786db.jpg?n=Halo-guardians_digital-boxshot-standard-ed_307x421.jpg"
            };

            PhysicalGameProduct vermintideSKU = new PhysicalGameProduct
            {
                DeveloperId = fatshark.Id,
                PublisherId = fatshark.Id,
                PlatformCode = ps4.PlatformCode,
                GameId = vermintide.Id,
                InternalNewSKU = "0000000000002",
                InteralUsedSKU = "1000000000002",
                NewWebPrice = 59.99m,
                ReleaseDate = new DateTime(2015, 11, 01),
                ProductAvailabilityStatus = AvailabilityStatus.Available,
                WillBuyBackUsedCopy = true,
                UsedWebPrice = 50.00m,
                SKUNameSuffix = "Launch Edition",
                BoxArtImageURL = "http://static.giantbomb.com/uploads/scale_super/2/23724/2790382-vermintide_art_big.jpg"
            };

            PhysicalGameProduct fallout4SKU = new PhysicalGameProduct
            {
                DeveloperId = bethesda.Id,
                PublisherId = bethesda.Id,
                PlatformCode = ps4.PlatformCode,
                GameId = fallout4.Id,
                InternalNewSKU = "0000000000003",
                InteralUsedSKU = "1000000000003",
                NewWebPrice = 59.99m,
                ReleaseDate = new DateTime(2015, 11, 01),
                ProductAvailabilityStatus = AvailabilityStatus.Available,
                WillBuyBackUsedCopy = true,
                UsedWebPrice = 50.00m,
                SKUNameSuffix = "Launch Edition",
                BoxArtImageURL = "http://ecx.images-amazon.com/images/I/81aoDmHE7hL._SL1500_.jpg"
            };

            using (TransactionScope scope = new TransactionScope())
            {
                context.PhysicalGameProducts.AddOrUpdate(
                    pgp => pgp.InternalNewSKU,
                    halo5SKU,
                    vermintideSKU,
                    fallout4SKU);

                context.SaveChanges();

                halo5SKU = context.PhysicalGameProducts.First(pgp => pgp.InternalNewSKU == halo5SKU.InternalNewSKU);
                vermintideSKU = context.PhysicalGameProducts.First(pgp => pgp.InternalNewSKU == vermintideSKU.InternalNewSKU);
                fallout4SKU = context.PhysicalGameProducts.First(pgp => pgp.InternalNewSKU == fallout4SKU.InternalNewSKU);

                context.ProductLocationInventories.AddOrUpdate(
                    pli => new
                    {
                        pli.LocationId,
                        pli.ProductId
                    },
                    new ProductLocationInventory
                    {
                        LocationId = onlineWarehouse.Id,
                        ProductId = halo5SKU.Id,
                        NewOnHand = 100,
                        NewOnOrder = 0,
                        UsedOnHand = 10
                    },
                    new ProductLocationInventory
                    {
                        LocationId = onlineWarehouse.Id,
                        ProductId = vermintideSKU.Id,
                        NewOnHand = 100,
                        NewOnOrder = 0,
                        UsedOnHand = 20
                    },
                    new ProductLocationInventory
                    {
                        LocationId = onlineWarehouse.Id,
                        ProductId = fallout4SKU.Id,
                        NewOnHand = 200,
                        NewOnOrder = 0,
                        UsedOnHand = 10
                    });

                scope.Complete();
            }
            #endregion Debug Only Seed Values

            /* Note: Uncomment this to regenerate the EnumToLookup script used in AddEnumToLookupMigration
                     After running Update-Database, copy the SQL Script from the exception message
            */
            /*EnumToLookup enumToLookup = new EnumToLookup
            {
                TableNamePrefix = "",
                TableNameSuffix = "_Lookup",
                NameFieldLength = 64,
                UseTransaction = true
            };

            var migrationSQL = enumToLookup.GenerateMigrationSql(context);
            
            throw new Exception(migrationSQL);*/
        }
    }
}
