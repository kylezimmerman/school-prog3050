using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Veil.Controllers;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Helpers;
using Veil.Models;

namespace Veil.Tests.Controllers
{
    [TestFixture]
    public class EventsControllerTests
    {
        private Guid Id;
        private Guid UserId;

        [SetUp]
        public void Setup()
        {
            Id = new Guid("45B0752E-998B-466A-AAAD-3ED535BA3559");
            UserId = new Guid("09EABF21-D5AC-4A5D-ADF8-27180E6D889B");
        }

        [TestCase(0, 0)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(0, 2)]
        public async void Index_WithEvents_ReturnsMatchingModel(int futureEventCount, int pastEventCount)
        {
            DateTime futureDate = DateTime.Now.AddDays(1);
            DateTime pastDate = DateTime.Now.AddDays(-1);

            List<Event> events = new List<Event>();

            for (int i = 0; i < futureEventCount; i++)
            {
                events.Add(new Event()
                {
                    Date = futureDate
                });
            }

            for (int i = 0; i < futureEventCount; i++)
            {
                events.Add(new Event()
                {
                    Date = pastDate
                });
            }

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Event>> eventDbSetStub = TestHelpers.GetFakeAsyncDbSet(events.AsQueryable());
            dbStub.Setup(db => db.Events).Returns(eventDbSetStub.Object);

            EventsController controller = new EventsController(dbStub.Object, idGetter: null);

            var result = await controller.Index() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<EventListViewModel>());

            var model = (EventListViewModel)result.Model;

            Assert.That(model.Events, Has.Count.EqualTo(futureEventCount));
        }

        [Test]
        public async void MyEvents_ReturnsMatchingModel()
        {
            DateTime futureDate = DateTime.Now.AddDays(1);

            Member member = new Member
            {
                UserId = UserId,
                RegisteredEvents = new List<Event>
                {  
                    new Event
                    {
                        Date = futureDate
                    },
                    new Event
                    {
                        Date = futureDate
                    }
                }
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            EventsController controller = new EventsController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            var result = await controller.MyEvents() as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<EventListViewModel>());

            var model = (EventListViewModel)result.Model;

            Assert.That(model.Events.Count(), Is.EqualTo(2));
        }

        [Test]
        public void RenderEventListItem_MemberNotRegistered_ReturnsMatchingModel()
        {
            DateTime futureDate = DateTime.Now.AddDays(1);

            Event eventItem = new Event
            {
                Id = Id,
                Date = futureDate
            };

            Member member = new Member
            {
                UserId = UserId,
                RegisteredEvents = new List<Event>()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.Find(member.UserId)).Returns(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            EventsController controller = new EventsController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
            };

            var result = controller.RenderEventListItem(eventItem, false) as PartialViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<EventListItemViewModel>());

            var model = (EventListItemViewModel)result.Model;

            Assert.That(model.Event.Id, Is.EqualTo(eventItem.Id));
        }

        [Test]
        public void CreateGET()
        {
            EventsController controller = new EventsController(veilDataAccess: null, idGetter: null);

            Assert.That(controller.Create() != null);
        }

        [Test]
        public async void CreatePOST_InvalidModelState_ReturnsToCreateView()
        {
            EventViewModel item = new EventViewModel
            {
                Id = Id
            };

            EventsController controller = new EventsController(veilDataAccess: null, idGetter: null);
            controller.ModelState.AddModelError("Name", "Name is required");

            var result = await controller.Create(item) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<EventViewModel>());
            Assert.That(result.Model, Is.EqualTo(item));
        }

        [Test]
        public async void CreatePOST_EventCreatedWithCorrectData_ItemFromDbMatchesSubmittedViewModel()
        {
            Event addedEvent = null;

            EventViewModel viewModel = new EventViewModel
            {
                Name = "New Name",
                Description = "New Description",
                Time = new DateTime(635827525788997554L, DateTimeKind.Local),
                Date = new DateTime(2015, 11, 10),
                Duration = "New Duration"
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            dbStub.Setup(db => db.Events.Add(It.IsAny<Event>())).Callback<Event>((model) =>
            {
                addedEvent = model;
            });

            EventsController controller = new EventsController(dbStub.Object, idGetter: null);

            await controller.Create(viewModel);

            Assert.That(addedEvent != null);
            Assert.That(addedEvent.Name, Is.EqualTo(viewModel.Name));
            Assert.That(addedEvent.Description, Is.EqualTo(viewModel.Description));
            Assert.That(addedEvent.Date, Is.EqualTo(viewModel.DateTime));
            Assert.That(addedEvent.Duration, Is.EqualTo(viewModel.Duration));
        }

        [Test]
        public async void CreatePOST_DbSavedChagnesCalled_ConfirmsSavedChanges()
        {
            EventViewModel viewModel = new EventViewModel
            {
                Name = "New Name",
                Description = "New Description",
                Time = new DateTime(635827525788997554L, DateTimeKind.Local),
                Date = new DateTime(2015, 11, 10),
                Duration = "New Duration"
            };

            Mock<IVeilDataAccess> dbMock = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Event>> eventDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Event>().AsQueryable());
            eventDbSetStub.Setup(edb => edb.Add(viewModel));

            dbMock.Setup(db => db.Events).Returns(eventDbSetStub.Object);
            dbMock.Setup(db => db.SaveChangesAsync()).ReturnsAsync(1).Verifiable();

            EventsController controller = new EventsController(dbMock.Object, idGetter: null);

            await controller.Create(viewModel);

            Assert.That(
                () =>
                    dbMock.Verify(db => db.SaveChangesAsync(),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public void EditGET_NullId_Throws404Exception()
        {
            EventsController controller = new EventsController(veilDataAccess: null, idGetter: null);

            Assert.That(async () => await controller.Edit((Guid?)null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public void EditGET_IdNotInDb_Throws404Exception()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Event>> eventDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Event>().AsQueryable());

            dbStub.Setup(db => db.Events).Returns(eventDbSetStub.Object);

            EventsController controller = new EventsController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.Edit(Id), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void EditGET_IdInDb_ReturnsViewModelWithMatchedEventDetails()
        {
            Event item = new Event
            {
                Id = Id,
                Date = new DateTime(635826779187565538L, DateTimeKind.Local),
                Description = "A Description",
                Duration = "Some duration",
                Name = "An event"
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Event>> eventDbSetStub =
                TestHelpers.GetFakeAsyncDbSet(new List<Event> { item }.AsQueryable());

            eventDbSetStub.Setup(edb => edb.FindAsync(item.Id)).ReturnsAsync(item);

            dbStub.Setup(db => db.Events).Returns(eventDbSetStub.Object);

            EventsController controller = new EventsController(dbStub.Object, idGetter: null);

            var result = await controller.Edit(item.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<EventViewModel>());

            var model = (EventViewModel) result.Model;

            Assert.That(model.Id, Is.EqualTo(item.Id));
            Assert.That(model.Date, Is.EqualTo(item.Date.Date));
            Assert.That(model.Time.TimeOfDay, Is.EqualTo(item.Date.TimeOfDay));
            Assert.That(model.Description, Is.EqualTo(item.Description));
            Assert.That(model.Duration, Is.EqualTo(item.Duration));
            Assert.That(model.Name, Is.EqualTo(item.Name));
        }

        [Test]
        public async void EditPOST_InvalidModelState_RedisplaysViewWithSameViewModel()
        {
            EventViewModel item = new EventViewModel
            {
                Id = Id
            };

            EventsController controller = new EventsController(veilDataAccess: null, idGetter: null);
            controller.ModelState.AddModelError("Name", "Name is required");

            var result = await controller.Edit(item) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<EventViewModel>());
            Assert.That(result.Model, Is.EqualTo(item));
        }

        [Test]
        public void EditPOST_IdNotInDb_Throws404Exception()
        {
            EventViewModel viewModel = new EventViewModel
            {
                Id = Id
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Event>> eventDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Event>().AsQueryable());
            eventDbSetStub.Setup(edb => edb.FindAsync(Id)).ReturnsAsync(null);

            dbStub.Setup(db => db.Events).Returns(eventDbSetStub.Object);

            EventsController controller = new EventsController(dbStub.Object, idGetter: null);

            Assert.That(async () => await controller.Edit(viewModel), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public async void EditPOST_IdInDb_MarksReturnedEventAsModified()
        {
            Event item = new Event
            {
                Id = Id,
            };

            EventViewModel viewModel = new EventViewModel
            {
                Id = item.Id
            };

            Mock<IVeilDataAccess> dbMock = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Event>> eventDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Event>().AsQueryable());
            eventDbSetStub.Setup(edb => edb.FindAsync(viewModel.Id)).ReturnsAsync(item);

            dbMock.Setup(db => db.Events).Returns(eventDbSetStub.Object);
            dbMock.Setup(db => db.MarkAsModified(item)).Verifiable();

            EventsController controller = new EventsController(dbMock.Object, idGetter: null);

            await controller.Edit(viewModel);

            Assert.That(
                () => 
                    dbMock.Verify(db => db.MarkAsModified(item), 
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void EditPOST_IdInDb_UpdatesQueriedItemToMatchEditedViewModel()
        {
            Event item = new Event
            {
                Id = Id,
                Name = "An Event",
                Description = "A description",
                Date = new DateTime(635826779187565538L, DateTimeKind.Local),
                Duration = "A duration"
            };

            EventViewModel viewModel = new EventViewModel
            {
                Id = item.Id,
                Name = "Edit Name",
                Description = "Edit Description",
                Time = new DateTime(635826926597335093L, DateTimeKind.Local),
                Date = new DateTime(2015, 11, 9),
                Duration = "Edit Duration"
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Event>> eventDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Event>().AsQueryable());
            eventDbSetStub.Setup(edb => edb.FindAsync(viewModel.Id)).ReturnsAsync(item);

            dbStub.Setup(db => db.Events).Returns(eventDbSetStub.Object);

            EventsController controller = new EventsController(dbStub.Object, idGetter: null);

            await controller.Edit(viewModel);

            Assert.That(item.Id, Is.EqualTo(viewModel.Id));
            Assert.That(item.Name, Is.EqualTo(viewModel.Name));
            Assert.That(item.Description, Is.EqualTo(viewModel.Description));
            Assert.That(item.Date, Is.EqualTo(viewModel.DateTime));
            Assert.That(item.Duration, Is.EqualTo(viewModel.Duration));
        }

        [Test]
        public async void EditPOST_IdInDb_CallsSaveChangesAsync()
        {
            Event item = new Event
            {
                Id = Id,
            };

            EventViewModel viewModel = new EventViewModel
            {
                Id = item.Id,
            };

            Mock<IVeilDataAccess> dbMock = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Event>> eventDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Event>().AsQueryable());
            eventDbSetStub.Setup(edb => edb.FindAsync(viewModel.Id)).ReturnsAsync(item);

            dbMock.Setup(db => db.Events).Returns(eventDbSetStub.Object);
            dbMock.Setup(db => db.SaveChangesAsync()).ReturnsAsync(1).Verifiable();

            EventsController controller = new EventsController(dbMock.Object, idGetter: null);

            await controller.Edit(viewModel);

            Assert.That(
                () =>
                    dbMock.Verify(db => db.SaveChangesAsync(),
                    Times.Exactly(1)),
                Throws.Nothing);
        }

        [Test]
        public async void EditPOST_UpdateSuccess_RedirectToDetailsForTheItem()
        {
            Event item = new Event
            {
                Id = Id,
            };

            EventViewModel viewModel = new EventViewModel
            {
                Id = item.Id,
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Event>> eventDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Event>().AsQueryable());
            eventDbSetStub.Setup(edb => edb.FindAsync(viewModel.Id)).ReturnsAsync(item);

            dbStub.Setup(db => db.Events).Returns(eventDbSetStub.Object);

            EventsController controller = new EventsController(dbStub.Object, idGetter: null);

            var result = await controller.Edit(viewModel) as RedirectToRouteResult;

            Assert.That(result != null);
            Assert.That(result.RouteValues["Id"], Is.EqualTo(item.Id));
            Assert.That(result.RouteValues["Action"], Is.EqualTo("Details"));
        }

        [Test]
        public void Details_NullId_Throws404Exception()
        {
            EventsController controller = new EventsController(veilDataAccess: null,  idGetter: null);

            Assert.That(async () => await controller.Details(null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(e => e.GetHttpCode() == 404));
        }

        [Test]
        public void Details_IdNotInDb_Throws404Exception()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Event>> gameDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Event>().AsQueryable());
            
            dbStub.Setup(db => db.Events).Returns(gameDbSetStub.Object);

            EventsController controller = new EventsController(dbStub.Object, idGetter: null);

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
                UserId = UserId,
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
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            EventsController controller = new EventsController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
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
            EventsController controller = new EventsController(veilDataAccess: null, idGetter: null);

            Assert.That(async () => await controller.Register(null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public void Register_IdNotInDb_Throws404Exception()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Event>> eventDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Event>().AsQueryable());

            dbStub.Setup(db => db.Events).Returns(eventDbSetStub.Object);

            EventsController controller = new EventsController(dbStub.Object, idGetter: null);

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
                UserId = UserId,
                RegisteredEvents = new List<Event>()
            };

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Event>> eventDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Event> { eventItem }.AsQueryable());
            eventDbSetStub.Setup(db => db.FindAsync(eventItem.Id)).ReturnsAsync(eventItem);
            dbStub.Setup(db => db.Events).Returns(eventDbSetStub.Object);

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            EventsController controller = new EventsController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
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
            EventsController controller = new EventsController(veilDataAccess: null, idGetter: null);

            Assert.That(async () => await controller.Unregister(null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public void Unregister_IdNotInDb_Throws404Exception()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Event>> eventDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Event>().AsQueryable());

            dbStub.Setup(db => db.Events).Returns(eventDbSetStub.Object);

            EventsController controller = new EventsController(dbStub.Object, idGetter: null);

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
                UserId = UserId,
                RegisteredEvents = new List<Event>()
            };

            eventItem.RegisteredMembers.Add(member);
            member.RegisteredEvents.Add(eventItem);

            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();

            Mock<DbSet<Event>> eventDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Event> { eventItem }.AsQueryable());
            eventDbSetStub.Setup(db => db.FindAsync(eventItem.Id)).ReturnsAsync(eventItem);
            dbStub.Setup(db => db.Events).Returns(eventDbSetStub.Object);

            Mock<DbSet<Member>> memberDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Member> { member }.AsQueryable());
            memberDbSetStub.Setup(db => db.FindAsync(member.UserId)).ReturnsAsync(member);
            dbStub.Setup(db => db.Members).Returns(memberDbSetStub.Object);

            Mock<ControllerContext> context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.User.Identity).Returns<IIdentity>(null);

            Mock<IGuidUserIdGetter> idGetterStub = new Mock<IGuidUserIdGetter>();
            idGetterStub.Setup(id => id.GetUserId(It.IsAny<IIdentity>())).Returns(member.UserId);

            EventsController controller = new EventsController(dbStub.Object, idGetterStub.Object)
            {
                ControllerContext = context.Object
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
            EventsController controller = new EventsController(veilDataAccess: null, idGetter: null);

            Assert.That(async () => await controller.Delete(null), Throws.InstanceOf<HttpException>().And.Matches<HttpException>(ex => ex.GetHttpCode() == 404));
        }

        [Test]
        public void Delete_IdNotInDb_Throws404Exception()
        {
            Mock<IVeilDataAccess> dbStub = TestHelpers.GetVeilDataAccessFake();
            Mock<DbSet<Event>> eventDbSetStub = TestHelpers.GetFakeAsyncDbSet(new List<Event>().AsQueryable());

            dbStub.Setup(db => db.Events).Returns(eventDbSetStub.Object);

            EventsController controller = new EventsController(dbStub.Object, idGetter: null);

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

            EventsController controller = new EventsController(dbStub.Object, idGetter: null);

            var result = await controller.Delete(item.Id) as ViewResult;

            Assert.That(result != null);
            Assert.That(result.Model, Is.InstanceOf<Event>());

            var model = (Event) result.Model;

            Assert.That(model.Id, Is.EqualTo(item.Id));
        }

        [Test]
        public async void DeleteConfirmed_EmptyGuid_RedirectsToIndex()
        {
            EventsController controller = new EventsController(veilDataAccess: null, idGetter: null);

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

            EventsController controller = new EventsController(dbStub.Object, idGetter: null);

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

            EventsController controller = new EventsController(dbStub.Object, idGetter: null);

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

            EventsController controller = new EventsController(dbMock.Object, idGetter: null);

            await controller.DeleteConfirmed(item.Id);

            Assert.That(
                () =>
                    dbMock.Verify(db => db.SaveChangesAsync(),
                    Times.Exactly(1)),
                Throws.Nothing);
        }
    }
}
