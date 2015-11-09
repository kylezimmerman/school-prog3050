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
