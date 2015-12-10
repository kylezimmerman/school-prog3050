using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;
using Veil.Helpers;
using Veil.Services;
using Veil.Services.Interfaces;

namespace Veil.Tests.Controllers.ManageControllerTests
{
    [TestFixture]
    public abstract class ManageControllerTestsBase
    {
        protected Guid memberId;
        protected Guid addressId;
        protected Guid creditCardId;

        protected User userWithMember;

        [SetUp]
        public void SetupBase()
        {
            memberId = new Guid("59EF92BE-D71F-49ED-992D-DF15773DAF98");
            addressId = new Guid("53BE47E4-0C74-4D49-97BB-7246A7880B39");
            creditCardId = new Guid("9E77DA3D-F27B-4390-9088-95D157070D06");

            userWithMember = new User
            {
                Id = memberId,
                Member = new Member
                {
                    UserId = memberId,
                    FavoritePlatforms = new List<Platform>(),
                    FavoriteTags = new List<Tag>()
                }
            };
        }

        protected ManageController CreateManageController(
            VeilUserManager userManager = null, VeilSignInManager signInManager = null,
            IVeilDataAccess veilDataAccess = null, IGuidUserIdGetter idGetter = null,
            IStripeService stripeService = null)
        {
            return new ManageController(userManager, signInManager, veilDataAccess, idGetter, stripeService);
        }

        protected List<MemberAddress> GetMemberAddresses()
        {
            return new List<MemberAddress>
            {
                new MemberAddress
                {
                    Address = new Address
                    {
                        City = "A city"
                    },
                    CountryCode = "CA",
                    MemberId = memberId
                },
                new MemberAddress
                {
                    Address = new Address
                    {
                        City = "Waterloo",
                        PostalCode = "N2L 6R2",
                        StreetAddress = "445 Wes Graham Way"
                    },
                    CountryCode = "CA",
                    ProvinceCode = "ON",
                    MemberId = memberId
                }
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

        protected Mock<IVeilDataAccess> SetupVeilDataAccessFakeWithCountriesAndAddresses(List<Country> countries = null, List<MemberAddress> addresses = null)
        {
            addresses = addresses ?? new List<MemberAddress>();

            Mock<IVeilDataAccess> dbStub = SetupVeilDataAccessFakeWithCountries(countries);
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(addresses.AsQueryable());

            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);

            return dbStub;
        }

        protected Mock<IVeilDataAccess> SetupVeilDataAccessFakeWithCountries(List<Country> countries = null)
        {
            countries = countries ?? new List<Country>();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Country>> countryDbSetStub = TestHelpers.GetFakeAsyncDbSet(countries.AsQueryable());
            countryDbSetStub.SetupForInclude();

            dbStub.
                Setup(db => db.Countries).
                Returns(countryDbSetStub.Object);

            return dbStub;
        }
    }
}
