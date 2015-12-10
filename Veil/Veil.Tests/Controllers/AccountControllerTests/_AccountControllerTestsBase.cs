using System;
using Microsoft.AspNet.Identity;
using Moq;
using NUnit.Framework;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models.Identity;
using Veil.Services.Interfaces;

namespace Veil.Tests.Controllers.AccountControllerTests
{
    [TestFixture]
    public abstract class AccountControllerTestsBase
    {
        // Identity Stubs
        protected Mock<IUserStore<User, Guid>> userStoreStub;
        protected Mock<IStripeService> stripeServiceStub;

        // Db Stub
        protected Mock<IVeilDataAccess> dbStub;

        [SetUp]
        public void SetupBase()
        {
            userStoreStub = new Mock<IUserStore<User, Guid>>();
            stripeServiceStub = new Mock<IStripeService>();

            dbStub = new Mock<IVeilDataAccess>();
            dbStub.Setup(db => db.UserStore).Returns(userStoreStub.Object);
        }

    }
}
