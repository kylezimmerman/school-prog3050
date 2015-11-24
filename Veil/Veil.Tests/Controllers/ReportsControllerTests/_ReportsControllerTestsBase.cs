using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Moq;
using NUnit.Framework;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;

namespace Veil.Tests.Controllers.ReportsControllerTests
{
    [TestFixture]
    public abstract class ReportsControllerTestsBase
    {
        protected Guid memberId;
        protected Guid memberId2;
        protected Member member;
        protected User memberUser;
        protected Member member2;
        protected User memberUser2;

        [SetUp]
        public void SetupBase()
        {
            memberId = new Guid("59EF92BE-D71F-49ED-992D-DF15773DAF98");
            memberId2 = new Guid("163EDEE7-FA6A-4220-928E-841CCC8CAAE0");

            member = new Member
            {
                UserId = memberId,
            };

            memberUser = new User
            {
                UserName = "JohnDope",
                FirstName = "John",
                LastName = "Doe",
                Id = memberId,
                PhoneNumber = "800-555-0199",
                Member = member
            };

            member2 = new Member
            {
                UserId = memberId2
            };

            memberUser2 = new User
            {
                UserName = "memberUser2",
                FirstName = "member",
                LastName = "user2",
                Id = memberId2,
                Member = member2
            };

        }

        protected void SetupVeilDataAccessWithUser(Mock<IVeilDataAccess> dbFake, params User[] users)
        {
            Mock<DbSet<User>> userDbSetFake = TestHelpers.GetFakeAsyncDbSet(users.AsQueryable());

            dbFake.
                Setup(db => db.Users).
                Returns(userDbSetFake.Object);
        }

        protected void SetupVeilDataAccessWithWebOrders(Mock<IVeilDataAccess> dbFake, List<WebOrder> webOrders)
        {
            Mock<DbSet<WebOrder>> webOrderDbSetFake = TestHelpers.GetFakeAsyncDbSet(webOrders.AsQueryable());
            webOrderDbSetFake.
                Setup(wdb => wdb.Add(It.IsAny<WebOrder>())).
                Returns<WebOrder>(val => val).
                Callback<WebOrder>(webOrders.Add);

            dbFake.
                Setup(db => db.WebOrders).
                Returns(webOrderDbSetFake.Object);
        }
    }
}
