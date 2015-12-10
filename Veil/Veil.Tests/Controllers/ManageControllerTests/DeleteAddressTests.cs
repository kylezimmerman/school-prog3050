/* DeleteAddressTests.cs
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
    public class DeleteAddressTests : ManageControllerTestsBase
    {
        [Test]
        public void DeleteAddress_IdNotInDb_Throws404Exception()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.FindAsync(It.IsAny<Guid>())).
                ReturnsAsync(null);

            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object);

            Assert.That(async () => await controller.DeleteAddress(addressId), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void DeleteAddress_IdInDb_RemovesReturnedAddress()
        {
            MemberAddress addressToDelete = new MemberAddress
            {
                Id = addressId
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetMock = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetMock.
                Setup(adb => adb.FindAsync(It.IsAny<Guid>())).
                ReturnsAsync(addressToDelete);

            addressDbSetMock.
                Setup(adb => adb.Remove(It.IsAny<MemberAddress>())).
                Returns<MemberAddress>(val => val).
                Verifiable();

            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetMock.Object);

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object);

            await controller.DeleteAddress(addressId);

            Assert.That(
                () =>
                    addressDbSetMock.Verify(adb => adb.Remove(addressToDelete),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void DeleteAddress_IdInDb_CallsSaveChangesAsync()
        {
            MemberAddress addressToDelete = new MemberAddress
            {
                Id = addressId
            };

            Mock<IVeilDataAccess> dbMock = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.FindAsync(It.IsAny<Guid>())).
                ReturnsAsync(addressToDelete);
            addressDbSetStub.
                Setup(adb => adb.Remove(It.IsAny<MemberAddress>())).
                Returns<MemberAddress>(val => val);

            dbMock.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);
            dbMock.
                Setup(db => db.SaveChangesAsync()).
                ReturnsAsync(1).
                Verifiable();

            ManageController controller = CreateManageController(veilDataAccess: dbMock.Object);

            await controller.DeleteAddress(addressId);

            Assert.That(
                () =>
                    dbMock.Verify(db => db.SaveChangesAsync(),
                    Times.Once),
                Throws.Nothing);
        }

        [Test]
        public async void DeleteAddress_IdInDb_ReturnsRedirectionToManageAddresses()
        {
            MemberAddress addressToDelete = new MemberAddress
            {
                Id = addressId
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<MemberAddress>> addressDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<MemberAddress>().AsQueryable());
            addressDbSetStub.
                Setup(adb => adb.FindAsync(It.IsAny<Guid>())).
                ReturnsAsync(addressToDelete);

            dbStub.
                Setup(db => db.MemberAddresses).
                Returns(addressDbSetStub.Object);

            ManageController controller = CreateManageController(veilDataAccess: dbStub.Object);

            var result = await controller.DeleteAddress(addressId) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo(nameof(ManageController.ManageAddresses)));
            Assert.That(result.RouteValues["Controllers"], Is.Null.Or.EqualTo("Manage"));
        }
    }
}
