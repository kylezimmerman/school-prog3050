/* ManageCreditCardsTests.cs
 *      Drew Matheson, 2015.11.11: Created
 */

using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Helpers;
using Veil.Models;

namespace Veil.Tests.Controllers.ManageControllerTests
{
    public class ManageCreditCardsTests : ManageControllerTestsBase
    {
        [Test]
        public async void ManageCreditCards_WhenCalled_SetsUpViewModel()
        {
            List<Country> countries = GetCountries();
            List<MemberCreditCard> creditCards = new List<MemberCreditCard>
            {
                new MemberCreditCard
                {
                    CardholderName = "Jane Shepard",
                    ExpiryMonth = 4,
                    ExpiryYear = 2154,
                    Last4Digits = "4242",
                    MemberId = memberId
                },
                new MemberCreditCard
                {
                    CardholderName = "John Shepard",
                    ExpiryMonth = 4,
                    ExpiryYear = 2154,
                    Last4Digits = "1881",
                    MemberId = memberId
                }
            };
            List<Member> members = new List<Member>
            {
                new Member { UserId = memberId, CreditCards = creditCards }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Member>> memberDbStub = TestHelpers.GetFakeAsyncDbSet(members.AsQueryable());

            Mock<DbSet<Country>> countryDbSetStub = TestHelpers.GetFakeAsyncDbSet(countries.AsQueryable());
            countryDbSetStub.SetupForInclude();

            dbStub.
                Setup(db => db.Members).
                Returns(memberDbStub.Object);
            dbStub.
                Setup(db => db.Countries).
                Returns(countryDbSetStub.Object);

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object);

            controller.ControllerContext = contextStub.Object;

            var result = await controller.ManageCreditCards() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<BillingInfoViewModel>());

            var model = (BillingInfoViewModel)result.Model;

            Assert.That(model.Countries, Is.Not.Empty);
            Assert.That(model.Countries, Has.Count.EqualTo(countries.Count));
            Assert.That(model.CreditCards, Is.Not.Empty);
            Assert.That(model.CreditCards.Count(), Is.EqualTo(creditCards.Count));
        }

        [Test]
        public async void ManageCreditCards_WhenCalled_IncludesProvinces()
        {
            List<Country> countries = GetCountries();

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member>().AsQueryable());

            Mock<DbSet<Country>> countryDbSetMock = TestHelpers.GetFakeAsyncDbSet(countries.AsQueryable());
            countryDbSetMock.
                Setup(cdb => cdb.Include(It.IsAny<string>())).
                Returns(countryDbSetMock.Object).
                Verifiable();

            dbStub.
                Setup(db => db.Members).
                Returns(memberDbSetStub.Object);
            dbStub.
                Setup(db => db.Countries).
                Returns(countryDbSetMock.Object);

            Mock<IGuidUserIdGetter> idGetterStub = TestHelpers.GetSetupIUserIdGetterFake(memberId);
            Mock<ControllerContext> contextStub = TestHelpers.GetSetupControllerContextFakeWithUserIdentitySetup();

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object, idGetter: idGetterStub.Object);
            controller.ControllerContext = contextStub.Object;

            await controller.ManageCreditCards();

            Assert.That(
                () =>
                    countryDbSetMock.Verify<DbQuery<Country>>(mdb => mdb.Include(It.Is<string>(val => val.Contains(nameof(IVeilDataAccess.Provinces)))),
                    Times.Exactly(1)),
                Throws.Nothing);
        }
    }
}
