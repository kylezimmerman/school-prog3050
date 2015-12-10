/* ConfirmNewEmailTests.cs
 *      Sean Coombes, 2015.12.08: Created
 */

using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models.Identity;
using Veil.Services;

namespace Veil.Tests.Controllers.ManageControllerTests
{
    public class ConfirmNewEmailTests : ManageControllerTestsBase
    {
        [Test]
        public async void ConfirmNewEmail_SuccessfulConfirmation()
        {
            User user = new User()
            {
                Id = memberId,
                Email = "person@email.com"
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/,
                null /*dataProtectionProvider*/);

            userManagerStub.Setup(um => um.FindByIdAsync(memberId)).ReturnsAsync(user);
            userManagerStub.Setup(um => um.ConfirmEmailAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            userManagerStub.Setup(um => um.UpdateSecurityStampAsync(It.IsAny<Guid>())).ReturnsAsync(IdentityResult.Success);
            dbStub.Setup(db => db.UserStore).Returns(userStoreStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            ManageController controller = new ManageController(userManagerStub.Object, null, dbStub.Object, null, null)
            {
                ControllerContext = context.Object
            };

            var result = await controller.ConfirmNewEmail(user.Id, "string");

            Assert.That(result != null);
        }

        [Test]
        public async void ConfirmNewEmail_EmptyGuid()
        {
            ManageController controller = new ManageController(null, null, null, null, null);

            var result = await controller.ConfirmNewEmail(Guid.Empty, "string");

            Assert.That(result != null);
        }

        [Test]
        public async void ConfirmNewEmail_IdentityResultFails()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/,
                null /*dataProtectionProvider*/);

            userManagerStub.Setup(um => um.ConfirmEmailAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Failed());
            dbStub.Setup(db => db.UserStore).Returns(userStoreStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            ManageController controller = new ManageController(userManagerStub.Object, null, dbStub.Object, null, null)
            {
                ControllerContext = context.Object
            };

            var result = await controller.ConfirmNewEmail(memberId, "string");

            Assert.That(result != null);
        }

        [Test]
        public async void ConfirmNewEmail_ConfirmEmailReturnsFailedIdentityResult_AddsErrorsToModelErrors()
        {
            string[] identityErrors =
            {
                "Error 1",
                "Error 2",
                "Error 3"
            };
            IdentityResult failedResult = IdentityResult.Failed(identityErrors);

            User user = new User()
            {
                Id = memberId,
                Email = "person@email.com"
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/,
                null /*dataProtectionProvider*/);

            userManagerStub.
                Setup(um => um.ConfirmEmailAsync(It.IsAny<Guid>(), It.IsAny<string>())).
                ReturnsAsync(failedResult);

            dbStub.
                Setup(db => db.UserStore).
                Returns(userStoreStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            ManageController controller = new ManageController(userManagerStub.Object, null, dbStub.Object, null, null)
            {
                ControllerContext = context.Object
            };

            await controller.ConfirmNewEmail(user.Id, "string");

            Assert.That(controller.ModelState.Count, Is.EqualTo(1));
            Assert.That(controller.ModelState.First().Value.Errors.Count, Is.EqualTo(identityErrors.Length));
        }

        [Test]
        public void ConfirmNewEmail_UserIsNullThrows()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/,
                null /*dataProtectionProvider*/);

            userManagerStub.Setup(um => um.FindByIdAsync(memberId)).ReturnsAsync(null);
            userManagerStub.Setup(um => um.ConfirmEmailAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            userManagerStub.Setup(um => um.UpdateSecurityStampAsync(It.IsAny<Guid>())).ReturnsAsync(IdentityResult.Success);
            dbStub.Setup(db => db.UserStore).Returns(userStoreStub.Object);

            ManageController controller = new ManageController(userManagerStub.Object, null, dbStub.Object, null, null);

            Assert.That(async () => await controller.ConfirmNewEmail(memberId, "string"), Throws.InvalidOperationException);
        }

        [Test]
        public async void ConfirmNewEmail_ThrowsOnSave()
        {
            User user = new User()
            {
                Id = memberId,
                Email = "person@email.com"
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<IUserStore<User, Guid>> userStoreStub = new Mock<IUserStore<User, Guid>>();
            Mock<VeilUserManager> userManagerStub = new Mock<VeilUserManager>(dbStub.Object, null /*messageService*/,
                null /*dataProtectionProvider*/);

            userManagerStub.Setup(um => um.FindByIdAsync(memberId)).ReturnsAsync(user);
            userManagerStub.Setup(um => um.ConfirmEmailAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            userManagerStub.Setup(um => um.UpdateSecurityStampAsync(It.IsAny<Guid>())).ReturnsAsync(IdentityResult.Success);
            dbStub.Setup(db => db.SaveChangesAsync()).Throws<DbUpdateException>();
            dbStub.Setup(db => db.UserStore).Returns(userStoreStub.Object);

            ManageController controller = new ManageController(userManagerStub.Object, null, dbStub.Object, null, null);

            var result = await controller.ConfirmNewEmail(user.Id, "string");

            Assert.That(result != null);
        }
    }
}
