/* DeleteCreditCardTests.cs
 *      Drew Matheson, 2015.12.01: Created
 */

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;

namespace Veil.Tests.Controllers.ManageControllerTests
{
    public class DeleteCreditCardTests : ManageControllerTestsBase
    {
        [Test]
        public void DeleteCreditCard_IdNotInDb_Throws404Exception()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberCreditCard>> creditCardDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberCreditCard>().AsQueryable());
            creditCardDbSetStub.
                Setup(adb => adb.FindAsync(It.IsAny<Guid>())).
                ReturnsAsync(null);

            dbStub.
                Setup(db => db.MemberCreditCards).
                Returns(creditCardDbSetStub.Object);

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object);

            Assert.That(async () => await controller.DeleteCreditCard(creditCardId), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void DeleteCreditCard_IdInDb_RemovesReturnedAddress()
        {
            MemberCreditCard creditCardToDelete = new MemberCreditCard
            {
                Id = creditCardId
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberCreditCard>> creditCardDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberCreditCard>().AsQueryable());
            creditCardDbSetStub.
                Setup(adb => adb.FindAsync(It.IsAny<Guid>())).
                ReturnsAsync(creditCardToDelete);

            creditCardDbSetStub.
                Setup(adb => adb.Remove(It.IsAny<MemberCreditCard>())).
                Returns<MemberCreditCard>(val => val).
                Verifiable();

            dbStub.
                Setup(db => db.MemberCreditCards).
                Returns(creditCardDbSetStub.Object);

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object);

            await controller.DeleteCreditCard(creditCardId);

            Assert.That(
                () =>
                    creditCardDbSetStub.Verify(adb => adb.Remove(creditCardToDelete),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void DeleteCreditCard_IdInDb_CallsSaveChangesAsync()
        {
            MemberCreditCard creditCardToDelete = new MemberCreditCard
            {
                Id = creditCardId
            };

            Mock<IVeilDataAccess> dbMock = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberCreditCard>> creditCardDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberCreditCard>().AsQueryable());
            creditCardDbSetStub.
                Setup(adb => adb.FindAsync(It.IsAny<Guid>())).
                ReturnsAsync(creditCardToDelete);

            creditCardDbSetStub.
                Setup(adb => adb.Remove(It.IsAny<MemberCreditCard>())).
                Returns<MemberCreditCard>(val => val);

            dbMock.
                Setup(db => db.MemberCreditCards).
                Returns(creditCardDbSetStub.Object);
            dbMock.
                Setup(db => db.SaveChangesAsync()).
                ReturnsAsync(1).
                Verifiable();

            ManageController controller = CreateManageController(veilDataAccess: dbMock.Object);

            await controller.DeleteCreditCard(creditCardId);

            Assert.That(
                () =>
                    dbMock.Verify(db => db.SaveChangesAsync(),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void DeleteCreditCard_IdInDb_ReturnsRedirectionToManageAddresses()
        {
            MemberCreditCard creditCardToDelete = new MemberCreditCard
            {
                Id = creditCardId
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberCreditCard>> creditCardDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberCreditCard>().AsQueryable());
            creditCardDbSetStub.
                Setup(adb => adb.FindAsync(It.IsAny<Guid>())).
                ReturnsAsync(creditCardToDelete);

            dbStub.
                Setup(db => db.MemberCreditCards).
                Returns(creditCardDbSetStub.Object);

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object);

            var result = await controller.DeleteCreditCard(creditCardId) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(ManageController.ManageCreditCards)));
            Assert.That(result.RouteValues["Controllers"], Is.Null.Or.EqualTo("Manage"));
        }
    }
}
