using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using JetBrains.Annotations;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;
using Veil.Helpers;
using Veil.Models;
using Veil.Services;
using Veil.Services.Interfaces;

namespace Veil.Tests.Controllers.CheckoutControllerTests
{
    [TestFixture]
    public abstract class CheckoutControllerTestsBase
    {
        protected Guid memberId;
        protected Guid addressId;
        protected Guid creditCardId;
        protected Guid cartProduct1Id;
        protected Guid cartProduct2Id;
        protected AddressViewModel validAddressViewModel;
        protected GameProduct cartProduct1;
        protected GameProduct cartProduct2;
        protected Game game;
        protected Platform platform;
        protected CartItem newProduct1CartItem;
        protected CartItem usedProduct1CartItem;
        protected CartItem newProduct2CartItem;
        protected Cart cartWithNewAndUsed;
        protected MemberCreditCard memberCreditCard;
        protected MemberAddress memberAddress;
        protected Member member;
        protected User memberUser;
        protected WebOrderCheckoutDetails validNotSavedShippingDetails;
        protected WebOrderCheckoutDetails validNotSavedShippingBillingDetails;
        protected WebOrderCheckoutDetails validSavedShippingBillingDetails;
        
        [SetUp]
        public void SetupBase()
        {
            memberId = new Guid("59EF92BE-D71F-49ED-992D-DF15773DAF98");
            addressId = new Guid("53BE47E4-0C74-4D49-97BB-7246A7880B39");
            creditCardId = new Guid("D9A69026-E3DA-4748-816B-293D9BE3E43F");
            cartProduct1Id = new Guid("3882D242-A62A-4E99-BA11-D6EF340C2EE8");
            cartProduct2Id = new Guid("7413D131-7337-42DC-A7E4-1155EB91E8C9");

            memberAddress = new MemberAddress
            {
                Address = new Address
                {
                    City = "Waterloo",
                    PostalCode = "N2L 6R2",
                    StreetAddress = "445 Wes Graham Way"
                },
                CountryCode = "CA",
                Country = new Country { CountryCode = "CA", CountryName = "Canada", FederalTaxRate = 0.05m },
                ProvinceCode = "ON",
                Province = new Province { CountryCode = "CA", ProvinceCode = "ON", ProvincialTaxRate = 0.08m },
                MemberId = memberId,
                Id = addressId
            };

            game = new Game
            {
                Name = "A game"
            };

            platform = new Platform
            {
                PlatformCode = "XONE",
                PlatformName = "Xbox One"
            };

            cartProduct1 = new PhysicalGameProduct
            {
                Id = cartProduct1Id,
                NewWebPrice = 60.00m,
                ProductAvailabilityStatus = AvailabilityStatus.Available,
                ReleaseDate = new DateTime(635835582902643008L, DateTimeKind.Local),
                UsedWebPrice = 10.00m,
                Game = game,
                Platform = platform
            };

            cartProduct2 = new PhysicalGameProduct
            {
                Id = cartProduct2Id,
                NewWebPrice = 59.99m,
                ProductAvailabilityStatus = AvailabilityStatus.Available,
                ReleaseDate = new DateTime(635837213100050176L, DateTimeKind.Local),
                Game = game,
                Platform = platform
            };

            newProduct1CartItem = new CartItem
            {
                IsNew = true,
                MemberId = memberId,
                Product = cartProduct1,
                ProductId = cartProduct1.Id,
                Quantity = 1
            };

            usedProduct1CartItem = new CartItem
            {
                IsNew = false,
                MemberId = memberId,
                Product = cartProduct1,
                ProductId = cartProduct1.Id,
                Quantity = 1
            };

            newProduct2CartItem = new CartItem
            {
                IsNew = true,
                MemberId = memberId,
                Product = cartProduct2,
                ProductId = cartProduct2.Id,
                Quantity = 1
            };

            validNotSavedShippingDetails = new WebOrderCheckoutDetails
            {
                Address = new Address
                {
                    City = "Waterloo",
                    PostalCode = "N2L 6R2",
                    POBoxNumber = "123",
                    StreetAddress = "445 Wes Graham Way"
                },
                ProvinceCode = "ON",
                CountryCode = "CA"
            };

            validAddressViewModel = new AddressViewModel
            {
                City = "Waterloo",
                CountryCode = "CA",
                ProvinceCode = "ON",
                POBoxNumber = "1234",
                PostalCode = "N2L 6R2",
                StreetAddress = "445 Wes Graham Way"
            };

            memberCreditCard = new MemberCreditCard
            {
                Id = creditCardId,
                CardholderName = "John Doe",
                ExpiryMonth = 11,
                ExpiryYear = 2015,
                Last4Digits = "4242",
                Member = member,
                MemberId = memberId,
                StripeCardId = "cardToken"
            };

            member = new Member
            {
                UserId = memberId,
                CreditCards = new List<MemberCreditCard>
                {
                    memberCreditCard
                }
            };

            memberUser = new User
            {
                FirstName = "John",
                LastName = "Doe",
                Id = memberId,
                PhoneNumber = "800-555-0199",
            };
            
            validNotSavedShippingBillingDetails = new WebOrderCheckoutDetails
            {
                Address = new Address
                {
                    City = "Waterloo",
                    PostalCode = "N2L 6R2",
                    POBoxNumber = "123",
                    StreetAddress = "445 Wes Graham Way"
                },
                ProvinceCode = "ON",
                CountryCode = "CA",
                StripeCardToken = "card_token"
            };

            validSavedShippingBillingDetails = new WebOrderCheckoutDetails
            {
                MemberCreditCardId = creditCardId,
                MemberAddressId = addressId
            };

            cartWithNewAndUsed = new Cart
            {
                Items = new List<CartItem>
                {
                    newProduct1CartItem,
                    usedProduct1CartItem
                },
                Member = member,
                MemberId = memberId
            };
        }

        protected CheckoutController CreateCheckoutController(
            IVeilDataAccess veilDataAccess = null, IGuidUserIdGetter idGetter = null,
            IStripeService stripeService = null, IShippingCostService shippingCostService = null,
            VeilUserManager userManager = null, ControllerContext context = null)
        {

            idGetter = idGetter ?? TestHelpers.GetSetupIUserIdGetterFake(memberId).Object;
            context = context ?? TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup().Object;

            var controller = new CheckoutController(
                veilDataAccess, idGetter, stripeService, shippingCostService, userManager)
            {
                ControllerContext = context
            };

            return controller;
        }

        protected void SetupVeilDataAccessWithAddresses(Mock<IVeilDataAccess> dbFake, [NotNull] IEnumerable<MemberAddress> addresses)
        {
            Mock<DbSet<MemberAddress>> addressDbSetFake =
                TestHelpers.GetFakeAsyncDbSet(addresses.AsQueryable());
            addressDbSetFake.SetupForInclude();

            dbFake.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetFake.Object);
        }

        protected void SetupVeilDataAccessWithCountriesSetupForInclude(
            Mock<IVeilDataAccess> dbFake, [NotNull]IEnumerable<Country> addresses)
        {
            Mock<DbSet<Country>> countriesDbSetFake =
                TestHelpers.GetFakeAsyncDbSet(addresses.AsQueryable());
            countriesDbSetFake.SetupForInclude();

            dbFake.
                Setup(db => db.Countries).
                Returns(countriesDbSetFake.Object);
        }

        protected void SetupVeilDataAccessWithCarts(Mock<IVeilDataAccess> dbFake, [NotNull]IEnumerable<Cart> carts)
        {
            Mock<DbSet<Cart>> cartDbSetFake = TestHelpers.GetFakeAsyncDbSet(carts.AsQueryable());
            cartDbSetFake.SetupForInclude();

            dbFake.
                Setup(db => db.Carts).
                Returns(cartDbSetFake.Object);
        }

        protected void SetupVeilDataAccessWithProvincesSetupForInclude(Mock<IVeilDataAccess> dbFake, IEnumerable<Province> provinces)
        {
            Mock<DbSet<Province>> provinceDbSetFake = TestHelpers.GetFakeAsyncDbSet(
                provinces.AsQueryable());
            provinceDbSetFake.SetupForInclude();

            dbFake.
                Setup(db => db.Provinces).
                Returns(provinceDbSetFake.Object);
        }

        protected void SetupVeilDataAccessWithMember(Mock<IVeilDataAccess> dbFake, Member theMember)
        {
            Mock<DbSet<Member>> memberDbSetFake =
                TestHelpers.GetFakeAsyncDbSet(new List<Member> { theMember }.AsQueryable());

            if (theMember != null)
            {
                memberDbSetFake.
                    Setup(mdb => mdb.FindAsync(theMember.UserId)).
                    ReturnsAsync(theMember);
            }
            
            dbFake.
                Setup(db => db.Members).
                Returns(memberDbSetFake.Object);
        }

        protected void SetupVeilDataAccessWithUser(Mock<IVeilDataAccess> dbFake, User user)
        {
            Mock<DbSet<User>> userDbSetFake = TestHelpers.GetFakeAsyncDbSet(new List<User> { user }.AsQueryable());

            dbFake.
                Setup(db => db.Users).
                Returns(userDbSetFake.Object);
        }
        
        protected Mock<ControllerContext> GetControllerContextWithSessionSetupToReturn(WebOrderCheckoutDetails returnValue)
        {
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();
            contextStub.
                SetupGet(c => c.HttpContext.Session[CheckoutController.OrderCheckoutDetailsKey]).
                Returns(returnValue);

            return contextStub;
        }

        protected List<MemberAddress> GetMemberAddresses()
        {
            return new List<MemberAddress>
            {
                memberAddress
            };
        }

        protected List<Country> GetCountries()
        {
            return new List<Country>
            {
                new Country { CountryCode = "CA", CountryName = "Canada"},
                new Country { CountryCode = "US", CountryName = "United States"}
            };
        }

        protected List<Cart> GetCartsListContainingCartWithNewAndUsed()
        {
            return new List<Cart>
            {
                cartWithNewAndUsed
            };
        }

        protected List<Province> GetProvinceList(WebOrderCheckoutDetails details)
        {
            List<Province> provinces = new List<Province>
            {
                new Province
                {
                    ProvinceCode = details.ProvinceCode,
                    CountryCode = details.CountryCode,
                    ProvincialTaxRate = 0.08m,
                    Country = new Country { CountryCode = "CA", CountryName = "Canada", FederalTaxRate = 0.05m }
                }
            };

            return provinces;
        }
    }
}
