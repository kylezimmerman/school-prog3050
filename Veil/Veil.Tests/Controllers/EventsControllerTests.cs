using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Models;

namespace Veil.Tests.Controllers
{
    [TestFixture]
    public class EventsControllerTests
    {
        private Guid Id;

        [SetUp]
        public void Setup()
        {
            Id = new Guid("45B0752E-998B-466A-AAAD-3ED535BA3559");
        }

        [Test]
        public void Details_NullId_Throws404Exception()
        {
            EventsController controller = new EventsController(null);

            Assert.That(async () => await controller.Details(null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }

        [Test]
        public void Details_IdNotInDb_Throws404Exception()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Event>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Event>().AsQueryable());
            
            dbStub.Setup(db => db.Events).Returns(gameDbSetStub.Object);

            EventsController controller = new EventsController(dbStub.Object);

            Assert.That(async () => await controller.Details(Id), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async void Details_IdInDb_ReturnsViewWithModel(bool memberRegistered)
        {
            Event eventItem = new Event
            {
                Id = Id,
                RegisteredMembers = new List<Member>()
            };

            Member member = new Member
            {
                UserId = Id,
                RegisteredEvents = new List<Event>()
            };

            if (memberRegistered)
            {
                eventItem.RegisteredMembers.Add(member);
                member.RegisteredEvents.Add(eventItem);
            }

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Event>> eventDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Event> { eventItem }.AsQueryable());
            eventDbSetStub.Setup(db => db.FindAsync(eventItem.Id)).ReturnsAsync(eventItem);
            dbStub.Setup(db => db.Events).Returns(eventDbSetStub.Object);

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(eventItem.Id)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            var claim = new Claim("CurrentUserTest", Id.ToString());
            var mockIdentity = Mock.Of<ClaimsIdentity>(ci => ci.FindFirst(It.IsAny<string>()) == claim);
            var context = Mock.Of<ControllerContext>(c => c.HttpContext.User.Identity == mockIdentity);

            EventsController controller = new EventsController(dbStub.Object)
            {
                ControllerContext = context
            };
            
            var result = await controller.Details(Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model != null);
            Assert.That(result.Model, Is.InstanceOf<EventDetailsViewModel>());

            var model = (EventDetailsViewModel)result.Model;

            Assert.That(model.Event, Is.EqualTo(eventItem));
            Assert.That(model.CurrentMemberIsRegistered == memberRegistered);
        }

        [Test]
        public void Register_NullId_Throws404Exception()
        {
            EventsController controller = new EventsController(veilDataAccess: null);

            Assert.That(async () => await controller.Register(null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public void Register_IdNotInDb_Throws404Exception()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Event>> eventDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Event>().AsQueryable());

            dbStub.Setup(db => db.Events).Returns(eventDbSetStub.Object);

            EventsController controller = new EventsController(dbStub.Object);

            Assert.That(async () => await controller.Register(Id), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void Register_IdInDb_ReturnsMatchedEventAsModel()
        {
            Event eventItem = new Event
            {
                Id = Id,
                RegisteredMembers = new List<Member>()
            };

            Member member = new Member
            {
                UserId = Id,
                RegisteredEvents = new List<Event>()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Event>> eventDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Event> { eventItem }.AsQueryable());
            eventDbSetStub.Setup(db => db.FindAsync(eventItem.Id)).ReturnsAsync(eventItem);
            dbStub.Setup(db => db.Events).Returns(eventDbSetStub.Object);

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(eventItem.Id)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            var claim = new Claim("CurrentUserTest", Id.ToString());
            var mockIdentity = Mock.Of<ClaimsIdentity>(ci => ci.FindFirst(It.IsAny<string>()) == claim);
            var context = Mock.Of<ControllerContext>(c => c.HttpContext.User.Identity == mockIdentity);

            EventsController controller = new EventsController(dbStub.Object)
            {
                ControllerContext = context
            };

            var result = await controller.Register(eventItem.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<EventDetailsViewModel>());

            var model = (EventDetailsViewModel)result.Model;

            Assert.That(model.Event.Id, Is.EqualTo(eventItem.Id));
        }

        [Test]
        public void Unregister_NullId_Throws404Exception()
        {
            EventsController controller = new EventsController(veilDataAccess: null);

            Assert.That(async () => await controller.Unregister(null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public void Unregister_IdNotInDb_Throws404Exception()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Event>> eventDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Event>().AsQueryable());

            dbStub.Setup(db => db.Events).Returns(eventDbSetStub.Object);

            EventsController controller = new EventsController(dbStub.Object);

            Assert.That(async () => await controller.Unregister(Id), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void Unregister_IdInDb_ReturnsMatchedEventAsModel()
        {
            Event eventItem = new Event
            {
                Id = Id,
                RegisteredMembers = new List<Member>()
            };

            Member member = new Member
            {
                UserId = Id,
                RegisteredEvents = new List<Event>()
            };

            eventItem.RegisteredMembers.Add(member);
            member.RegisteredEvents.Add(eventItem);

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Event>> eventDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Event> { eventItem }.AsQueryable());
            eventDbSetStub.Setup(db => db.FindAsync(eventItem.Id)).ReturnsAsync(eventItem);
            dbStub.Setup(db => db.Events).Returns(eventDbSetStub.Object);

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(eventItem.Id)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            var claim = new Claim("CurrentUserTest", Id.ToString());
            var mockIdentity = Mock.Of<ClaimsIdentity>(ci => ci.FindFirst(It.IsAny<string>()) == claim);
            var context = Mock.Of<ControllerContext>(c => c.HttpContext.User.Identity == mockIdentity);

            EventsController controller = new EventsController(dbStub.Object)
            {
                ControllerContext = context
            };

            var result = await controller.Unregister(eventItem.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<EventDetailsViewModel>());

            var model = (EventDetailsViewModel)result.Model;

            Assert.That(model.Event.Id, Is.EqualTo(eventItem.Id));
        }

        [Test]
        public void Delete_NullId_Throws404Exception()
        {
            EventsController controller = new EventsController(veilDataAccess: null);

            Assert.That(async () => await controller.Delete(null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public void Delete_IdNotInDb_Throws404Exception()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Event>> eventDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Event>().AsQueryable());

            dbStub.Setup(db => db.Events).Returns(eventDbSetStub.Object);

            EventsController controller = new EventsController(dbStub.Object);

            Assert.That(async () => await controller.Delete(Id), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void Delete_IdInDb_ReturnsMatchedEventAsModel()
        {
            Event item = new Event
            {
                Id = Id
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Event>> eventDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Event> { item }.AsQueryable());
            eventDbSetStub.Setup(edb => edb.FindAsync(item.Id)).ReturnsAsync(item);

            dbStub.Setup(db => db.Events).Returns(eventDbSetStub.Object);

            EventsController controller = new EventsController(dbStub.Object);

            var result = await controller.Delete(item.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<Event>());

            var model = (Event) result.Model;

            Assert.That(model.Id, Is.EqualTo(item.Id));
        }

        [Test]
        public async void DeleteConfirmed_EmptyGuid_RedirectsToIndex()
        {
            EventsController controller = new EventsController(veilDataAccess: null);

            var result = await controller.DeleteConfirmed(Guid.Empty) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Index"));
        }

        [Test]
        public void DeleteConfirmed_IdNotInDb_Throws404Exception()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Event>> eventDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Event>().AsQueryable());

            dbStub.Setup(db => db.Events).Returns(eventDbSetStub.Object);

            EventsController controller = new EventsController(dbStub.Object);

            Assert.That(async () => await controller.DeleteConfirmed(Id), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void DeleteConfirmed_IdInDb_CallsRemoveOnDbSet()
        {
            Event item = new Event
            {
                Id = Id
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Event>> eventDbSetMock =
                TestHelpers.GetFakeAsyncDbSet(new List<Event> { item }.AsQueryable());
            eventDbSetMock.Setup(edb => edb.FindAsync(item.Id)).ReturnsAsync(item);
            eventDbSetMock.Setup(edb => edb.Remove(item)).Returns(item).Verifiable();

            dbStub.Setup(db => db.Events).Returns(eventDbSetMock.Object);

            EventsController controller = new EventsController(dbStub.Object);

            await controller.DeleteConfirmed(item.Id);

            Assert.That(
                () => 
                    eventDbSetMock.Verify(edb => edb.Remove(item),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void DeleteConfirmed_IdInDb_CallsSaveChangesOnTheDb()
        {
            Event item = new Event
            {
                Id = Id
            };

            Mock<IVeilDataAccess> dbMock = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Event>> eventDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(new List<Event> { item }.AsQueryable());
            eventDbSetStub.Setup(edb => edb.FindAsync(item.Id)).ReturnsAsync(item);
            eventDbSetStub.Setup(edb => edb.Remove(item)).Returns(item);

            dbMock.Setup(db => db.Events).Returns(eventDbSetStub.Object);
            dbMock.Setup(db => db.SaveChangesAsync()).ReturnsAsync(1).Verifiable();

            EventsController controller = new EventsController(dbMock.Object);

            await controller.DeleteConfirmed(item.Id);

            Assert.That(
                () =>
                    dbMock.Verify(db => db.SaveChangesAsync(),
                    Times.Exactly(1)),
                Throws.Nothing);
        }
    }
}
