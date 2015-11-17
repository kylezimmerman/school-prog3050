using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;
using Veil.Helpers;
using Veil.Models;

namespace Veil.Tests.Controllers
{
    [TestFixture]
    class FriendListControllerTests
    {
        private Guid Id1;
        private Guid Id2;
        private Guid Id3;
        private Guid UserId;
        private Member member;
        private Member otherMember1;
        private Member otherMember2;
        private Member otherMember3;


        [SetUp]
        public void Setup()
        {
            Id1 = new Guid("45B0752E-998B-466A-AAAD-3ED535BA3559");
            Id2 = new Guid("59421EF4-4585-4593-BBA0-52CA80A9E774");
            Id3 = new Guid("D714C3D5-E3BB-4D28-A5CD-029706773DD7");
            UserId = new Guid("09EABF21-D5AC-4A5D-ADF8-27180E6D889B");

            member = new Member
            {
                UserId = UserId,
                RequestedFriendships = new List<Friendship>(),
                ReceivedFriendships = new List<Friendship>(),
                UserAccount = new User()
                {
                    UserName = "Isaac"
                }
            };

            otherMember1 = new Member()
            {
                UserId = Id1,
                ReceivedFriendships = new List<Friendship>(),
                RequestedFriendships = new List<Friendship>(),
                UserAccount = new User()
                {
                    UserName = "Drew"
                }
            };

            otherMember2 = new Member()
            {
                UserId = Id2,
                ReceivedFriendships = new List<Friendship>(),
                RequestedFriendships = new List<Friendship>(),
                UserAccount = new User()
                {
                    UserName = "Kyle"
                }
            };

            otherMember3 = new Member()
            {
                UserId = Id3,
                ReceivedFriendships = new List<Friendship>(),
                RequestedFriendships = new List<Friendship>(),
                UserAccount = new User()
                {
                    UserName = "Justin"
                }
            };
        }

        [Test]
        public async void Index_MemberViewsFriendList_NoFreindRequestsNoFriends_ReturnsCorrectViewModel()
        {
            Member member = new Member
            {
                UserId = UserId,
                RequestedFriendships = new List<Friendship>(),
                ReceivedFriendships = new List<Friendship>()  
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);
            memberDbSetStub.SetupForInclude();

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            FriendListController controller = new FriendListController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.Index() as ViewResult;

            Assert.That(result.Model != null);

            var model = (FriendsListViewModel)result.Model;

            Assert.That(model.PendingReceivedFriendships.Count == 0);
            Assert.That(model.PendingSentFriendships.Count == 0);
            Assert.That(model.ConfirmedFriends.Count == 0);
        }

        [Test]
        public async void Index_MemberViewsFriendList_OneRequestedOneReceivedOneFriend_ReturnsCorrectViewModel()
        {
            Friendship requested = new Friendship()
            {
                ReceiverId = otherMember1.UserId,
                RequesterId = member.UserId,
                RequestStatus = FriendshipRequestStatus.Pending
            };

            Friendship received = new Friendship()
            {
                ReceiverId = member.UserId,
                RequesterId = otherMember2.UserId,
                RequestStatus = FriendshipRequestStatus.Pending
            };

            Friendship friend = new Friendship()
            {
                ReceiverId = member.UserId,
                RequesterId = otherMember3.UserId,
                RequestStatus = FriendshipRequestStatus.Accepted
            };

            member.RequestedFriendships.Add(requested);
            member.ReceivedFriendships.Add(received);
            member.ReceivedFriendships.Add(friend);

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            memberDbSetStub.SetupForInclude();

            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            FriendListController controller = new FriendListController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.Index() as ViewResult;

            Assert.That(result.Model != null);

            var model = (FriendsListViewModel)result.Model;

            Assert.That(model.PendingSentFriendships.Count == 1);
            Assert.That(model.PendingReceivedFriendships.Count == 1);
            Assert.That(model.ConfirmedFriends.Count == 1);
        }

        [Test]
        public async void AddFriendRequest_AddFriendIncorrectUsername_DoesntAddFriendship()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);

            Mock<DbSet<Friendship>> friendshipDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Friendship>().AsQueryable());

            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);
            dbStub.Setup(db => db.Friendships).Returns(friendshipDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            FriendListController controller = new FriendListController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };


            Friendship friend = new Friendship()
            {
                ReceiverId = member.UserId,
                RequesterId = otherMember1.UserId,
                RequestStatus = FriendshipRequestStatus.Pending
            };

            dbStub.Setup(db => db.Friendships.Add(friend)).Verifiable();

            //otherMember1 isn't in the mocked db
            await controller.AddFriendRequest(otherMember1.UserAccount.UserName);

            Assert.That(
                () =>
                    dbStub.Verify(db => db.Friendships.Add(friend),
                    Times.Never),
                Throws.Nothing);
        }

        [Test]
        public async void AddFriendRequest_AddFriend_AddsFriendship()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member, otherMember1 }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);

            Mock<DbSet<Friendship>> friendshipDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Friendship>().AsQueryable());
            friendshipDbSetStub.Setup(db => db.Add(It.IsAny<Friendship>())).Returns<Friendship>(i => i).Verifiable();

            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);
            dbStub.Setup(db => db.Friendships).Returns(friendshipDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            FriendListController controller = new FriendListController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            await controller.AddFriendRequest(otherMember1.UserAccount.UserName);

            Assert.That(
                () =>
                    friendshipDbSetStub.Verify(db => db.Add(It.IsAny<Friendship>()),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void AddFriendRequest_AddFriendWhoAlreadyRequestedUser_ChangesFriendshipToAccepted()
        {
            Friendship received = new Friendship
            {
                ReceiverId = member.UserId,
                Receiver = member,
                RequesterId = otherMember1.UserId,
                Requester = otherMember1,
                RequestStatus = FriendshipRequestStatus.Pending
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member, otherMember1 }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);

            Mock<DbSet<Friendship>> friendshipDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Friendship> { received }.AsQueryable());
            friendshipDbSetStub.Setup(db => db.Add(It.IsAny<Friendship>())).Returns<Friendship>(i => i).Verifiable();

            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);
            dbStub.Setup(db => db.Friendships).Returns(friendshipDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            FriendListController controller = new FriendListController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            await controller.AddFriendRequest(otherMember1.UserAccount.UserName);

            Assert.That(received.RequestStatus == FriendshipRequestStatus.Accepted);
            Assert.That(
                () =>
                    dbStub.Verify(db => db.SaveChangesAsync(),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void AddFriendRequest_RequestsOnesSelf_NothingChanged()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member, otherMember1 }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);

            Mock<DbSet<Friendship>> friendshipDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Friendship>().AsQueryable());
            friendshipDbSetStub.Setup(db => db.Add(It.IsAny<Friendship>())).Returns<Friendship>(i => i).Verifiable();

            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);
            dbStub.Setup(db => db.Friendships).Returns(friendshipDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            FriendListController controller = new FriendListController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            await controller.AddFriendRequest(member.UserAccount.UserName);

            Assert.That(
                () =>
                    friendshipDbSetStub.Verify(db => db.Add(It.IsAny<Friendship>()),
                    Times.Never),
                Throws.Nothing);
            Assert.That(
                () =>
                    dbStub.Verify(db => db.SaveChangesAsync(),
                    Times.Never),
                Throws.Nothing);
        }

        [Test]
        public async void AddFriendRequest_AddFriendAlreadyFriends_NothingChanged()
        {
            Friendship friendship = new Friendship
            {
                ReceiverId = member.UserId,
                Receiver = member,
                RequesterId = otherMember1.UserId,
                Requester = otherMember1,
                RequestStatus = FriendshipRequestStatus.Accepted
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member, otherMember1 }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);

            Mock<DbSet<Friendship>> friendshipDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Friendship> { friendship }.AsQueryable());
            friendshipDbSetStub.Setup(db => db.Add(It.IsAny<Friendship>())).Returns<Friendship>(i => i).Verifiable();

            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);
            dbStub.Setup(db => db.Friendships).Returns(friendshipDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            FriendListController controller = new FriendListController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            await controller.AddFriendRequest(otherMember1.UserAccount.UserName);

            Assert.That(
                () =>
                    friendshipDbSetStub.Verify(db => db.Add(It.IsAny<Friendship>()),
                    Times.Never),
                Throws.Nothing);
            Assert.That(
                () =>
                    dbStub.Verify(db => db.SaveChangesAsync(),
                    Times.Never),
                Throws.Nothing);
        }

        [Test]
        public async void AddFriendRequest_AddRequestedNotAcceptedYet_NothingChanged()
        {
            Friendship friendship = new Friendship
            {
                RequesterId = member.UserId,
                Requester = member,
                ReceiverId = otherMember1.UserId,
                Receiver = otherMember1,
                RequestStatus = FriendshipRequestStatus.Pending
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member, otherMember1 }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);

            Mock<DbSet<Friendship>> friendshipDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Friendship> { friendship }.AsQueryable());
            friendshipDbSetStub.Setup(db => db.Add(It.IsAny<Friendship>())).Returns<Friendship>(i => i).Verifiable();

            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);
            dbStub.Setup(db => db.Friendships).Returns(friendshipDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            FriendListController controller = new FriendListController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            await controller.AddFriendRequest(otherMember1.UserAccount.UserName);

            Assert.That(
                () =>
                    friendshipDbSetStub.Verify(db => db.Add(It.IsAny<Friendship>()),
                    Times.Never),
                Throws.Nothing);
            Assert.That(
                () =>
                    dbStub.Verify(db => db.SaveChangesAsync(),
                    Times.Never),
                Throws.Nothing);
        }

        [Test]
        public async void Approve_UserApprovesRequest_ChangesFriendshipToAccepted()
        {
            Friendship received = new Friendship
            {
                ReceiverId = member.UserId,
                Receiver = member,
                RequesterId = otherMember1.UserId,
                Requester = otherMember1,
                RequestStatus = FriendshipRequestStatus.Pending
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member, otherMember1 }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);

            Mock<DbSet<Friendship>> friendshipDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Friendship> { received }.AsQueryable());
            friendshipDbSetStub.Setup(db => db.Add(It.IsAny<Friendship>())).Returns<Friendship>(i => i).Verifiable();

            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);
            dbStub.Setup(db => db.Friendships).Returns(friendshipDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            FriendListController controller = new FriendListController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            await controller.Approve(otherMember1.UserId);

            Assert.That(received.RequestStatus == FriendshipRequestStatus.Accepted);
            Assert.That(
                () =>
                    dbStub.Verify(db => db.SaveChangesAsync(),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void Approve_ApproveNullFriendhsip_NothingHappens()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member, otherMember1 }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);

            Mock<DbSet<Friendship>> friendshipDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Friendship>().AsQueryable());
            friendshipDbSetStub.Setup(db => db.Add(It.IsAny<Friendship>())).Returns<Friendship>(i => i).Verifiable();

            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);
            dbStub.Setup(db => db.Friendships).Returns(friendshipDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            FriendListController controller = new FriendListController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            await controller.Approve(otherMember1.UserId);

            Assert.That(
                () =>
                    friendshipDbSetStub.Verify(db => db.Add(It.IsAny<Friendship>()),
                    Times.Never),
                Throws.Nothing);
            Assert.That(
                () =>
                    dbStub.Verify(db => db.SaveChangesAsync(),
                    Times.Never),
                Throws.Nothing);
        }

        [Test]
        public async void Decline_UserDeclinesRequest_DeletesFriendship()
        {
            Friendship received = new Friendship
            {
                ReceiverId = member.UserId,
                Receiver = member,
                RequesterId = otherMember1.UserId,
                Requester = otherMember1,
                RequestStatus = FriendshipRequestStatus.Pending
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member, otherMember1 }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);

            Mock<DbSet<Friendship>> friendshipDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Friendship> { received }.AsQueryable());
            friendshipDbSetStub.Setup(db => db.Remove(It.IsAny<Friendship>())).Verifiable();

            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);
            dbStub.Setup(db => db.Friendships).Returns(friendshipDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            FriendListController controller = new FriendListController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            await controller.Decline(otherMember1.UserId);

            Assert.That(
                () =>
                    friendshipDbSetStub.Verify(db => db.Remove(It.IsAny<Friendship>()),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void Decline_DeclineNullFriendship_NothingHappens()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member, otherMember1 }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);

            Mock<DbSet<Friendship>> friendshipDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Friendship>().AsQueryable());
            friendshipDbSetStub.Setup(db => db.Remove(It.IsAny<Friendship>())).Verifiable();

            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);
            dbStub.Setup(db => db.Friendships).Returns(friendshipDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            FriendListController controller = new FriendListController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            await controller.Decline(otherMember1.UserId);

            Assert.That(
                () =>
                    friendshipDbSetStub.Verify(db => db.Remove(It.IsAny<Friendship>()),
                    Times.Never),
                Throws.Nothing);
        }

        [Test]
        public async void Remove_UserRemovesFriend_DeletesFriendship()
        {
            Friendship received = new Friendship
            {
                ReceiverId = member.UserId,
                Receiver = member,
                RequesterId = otherMember1.UserId,
                Requester = otherMember1,
                RequestStatus = FriendshipRequestStatus.Accepted
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member, otherMember1 }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);

            Mock<DbSet<Friendship>> friendshipDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Friendship> { received }.AsQueryable());
            friendshipDbSetStub.Setup(db => db.Remove(It.IsAny<Friendship>())).Verifiable();

            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);
            dbStub.Setup(db => db.Friendships).Returns(friendshipDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            FriendListController controller = new FriendListController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            await controller.Remove(otherMember1.UserId);

            Assert.That(
                () =>
                    friendshipDbSetStub.Verify(db => db.Remove(It.IsAny<Friendship>()),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void Cancel_UserCancelsRequest_DeletesFriendship()
        {
            Friendship received = new Friendship
            {
                ReceiverId = member.UserId,
                Receiver = member,
                RequesterId = otherMember1.UserId,
                Requester = otherMember1,
                RequestStatus = FriendshipRequestStatus.Pending
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member, otherMember1 }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);

            Mock<DbSet<Friendship>> friendshipDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Friendship> { received }.AsQueryable());
            friendshipDbSetStub.Setup(db => db.Remove(It.IsAny<Friendship>())).Verifiable();

            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);
            dbStub.Setup(db => db.Friendships).Returns(friendshipDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);
            context.Setup(c => c.HttpContext.User.Identity.IsAuthenticated).Returns(true);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            FriendListController controller = new FriendListController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            await controller.Cancel(otherMember1.UserId);

            Assert.That(
                () =>
                    friendshipDbSetStub.Verify(db => db.Remove(It.IsAny<Friendship>()),
                    Times.Exactly(1)),
                Throws.Nothing);
        }
    }
}
