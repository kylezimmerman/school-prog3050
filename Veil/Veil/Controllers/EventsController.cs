using System;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Net;
using System.Web.Mvc;
using System.Linq;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Models;
using Veil.Extensions;
using System.Web;
using Veil.DataModels;
using Veil.Helpers;

namespace Veil.Controllers
{
    public class EventsController : BaseController
    {
        private readonly IVeilDataAccess db;

        public EventsController(IVeilDataAccess veilDataAccess)
        {
            db = veilDataAccess;
        }

        // GET: Events
        /// <summary>
        ///     Displays a list of upcoming events
        /// </summary>
        /// <returns>
        ///     Index view with all upcoming events
        /// </returns>
        public async Task<ActionResult> Index()
        {
            var model = new EventListViewModel
            {
                Events = await db.Events
                    .Where(e => e.Date > DateTime.Now)
                    .OrderBy(e => e.Date).ToListAsync(),
                OnlyRegisteredEvents = false
            };
            return View(model);
        }

        // GET: Events/MyEvents
        /// <summary>
        ///     Displays a list of upcoming events that the current member is registered to attend
        /// </summary>
        /// <returns>
        ///     Index view filtered to the current member's registered events
        /// </returns>
        [Authorize(Roles = VeilRoles.MEMBER_ROLE)]
        public async Task<ActionResult> MyEvents()
        {
            Guid currentUserId = User.Identity.GetUserId();
            Member currentMember = await db.Members.FindAsync(currentUserId);

            var model = new EventListViewModel
            {
                Events = currentMember.RegisteredEvents
                    .Where(e => e.Date > DateTime.Now)
                    .OrderBy(e => e.Date),
                OnlyRegisteredEvents = true
            };

            return View("Index", model);
        }

        /// <summary>
        ///     Renders an individual event for a list of events
        /// </summary>
        /// <param name="eventItem">
        ///     The Event being displayed on this item of the list
        /// </param>
        /// <param name="onlyRegisteredEvents">
        ///     If true filters out any events the current member is not registered for
        /// </param>
        /// <returns>
        ///     Partial view specific to eventItem
        /// </returns>
        [ChildActionOnly]
        public ActionResult RenderEventListItem(Event eventItem, bool onlyRegisteredEvents)
        {
            var model = new EventListItemViewModel
            {
                Event = eventItem,
                OnlyRegisteredEvents = onlyRegisteredEvents
            };

            Guid currentUserId = User.Identity.GetUserId();
            Member currentMember = db.Members.Find(currentUserId);

            if (currentMember != null)
            {
                model.CurrentMemberIsRegistered = currentMember.RegisteredEvents.Contains(model.Event);
            }

            return PartialView("_EventListItem", model);
        }

        // GET: Events/Details/5
        /// <summary>
        ///     Displays information about a specific event
        /// </summary>
        /// <param name="id">
        ///     The id of the event to view details of
        /// </param>
        /// <returns>
        ///     Details view for the Event matching id
        ///     404 Not Found view if the id does not match an Event
        /// </returns>
        public async Task<ActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                throw new HttpException((int)HttpStatusCode.NotFound, nameof(Event));
            }

            var model = new EventDetailsViewModel
            {
                Event = await db.Events.FindAsync(id),
            };

            if (model.Event == null)
            {
                throw new HttpException((int)HttpStatusCode.NotFound, nameof(Event));
            }

            Guid currentUserId = User.Identity.GetUserId();
            Member currentMember = await db.Members.FindAsync(currentUserId);

            if (currentMember != null)
            {
                model.CurrentMemberIsRegistered = model.Event.RegisteredMembers.Contains(currentMember);
            }

            return View(model);
        }

        // GET: Events/Register/5
        /// <summary>
        ///     Registers the current member as an attendee of an Event
        /// </summary>
        /// <param name="id">
        ///     The id of the event the member is registering for
        /// </param>
        /// <returns>
        ///     Details view for the Event matching id
        ///     404 Not Found view if the id does not match an Event
        /// </returns>
        [Authorize(Roles = VeilRoles.MEMBER_ROLE)]
        public async Task<ActionResult> Register(Guid? id)
        {
            if (id == null)
            {
                throw new HttpException((int)HttpStatusCode.NotFound, nameof(Event));
            }

            Event currentEvent = await db.Events.FindAsync(id);

            if (currentEvent == null)
            {
                throw new HttpException((int)HttpStatusCode.NotFound, nameof(Event));
            }

            Guid currentUserId = User.Identity.GetUserId();
            Member currentMember = await db.Members.FindAsync(currentUserId);

            currentMember.RegisteredEvents.Add(currentEvent);
            db.MarkAsModified(currentMember);

            await db.SaveChangesAsync();

            var model = new EventDetailsViewModel
            {
                Event = currentEvent,
                CurrentMemberIsRegistered = currentEvent.RegisteredMembers.Contains(currentMember)
            };

            this.AddAlert(AlertType.Success, $"You have been registered to attend {currentEvent.Name}");

            return View("Details", model);
        }

        /// <summary>
        ///     Unregisters the current member to no longer be an attendee of an Event
        /// </summary>
        /// <param name="id">
        ///     The id of the event the member is unregistering from
        /// </param>
        /// <returns>
        ///     Details view for the Event matching id
        ///     404 Not Found view if the id does not match an Event
        /// </returns>
        [Authorize(Roles = VeilRoles.MEMBER_ROLE)]
        public async Task<ActionResult> Unregister(Guid? id)
        {
            if(id == null)
            {
                throw new HttpException((int)HttpStatusCode.NotFound, nameof(Event));
            }

            Event currentEvent = await db.Events.FindAsync(id);

            if (currentEvent == null)
            {
                throw new HttpException((int)HttpStatusCode.NotFound, nameof(Event));
            }

            Guid currentUserId = User.Identity.GetUserId();
            Member currentMember = await db.Members.FindAsync(currentUserId);

            currentMember.RegisteredEvents.Remove(currentEvent);
            db.MarkAsModified(currentMember);

            await db.SaveChangesAsync();

            var model = new EventDetailsViewModel
            {
                Event = currentEvent,
                CurrentMemberIsRegistered = currentEvent.RegisteredMembers.Contains(currentMember)
            };

            this.AddAlert(AlertType.Info, $"You are no longer attending {currentEvent.Name}");

            return View("Details", model);
        }

        // TODO: Every action after this should be employee only

        // GET: Events/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Events/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(EventViewModel eventViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(eventViewModel);
            }

            Event @event = new Event
            {
                Date = eventViewModel.DateTime,
                Description = eventViewModel.Description,
                Duration = eventViewModel.Duration,
                Name = eventViewModel.Name
            };

            db.Events.Add(@event);
            await db.SaveChangesAsync();

            this.AddAlert(AlertType.Success, $"Successfully created the \"{@event.Name}\" event.");

            return RedirectToAction("Index");
        }

        /// <summary>
        ///     Displays a form for editting the Event
        /// </summary>
        /// <param name="id">
        ///     The id of the event to be editted
        /// </param>
        /// <returns>
        ///     The edit page if a matching Event is found
        ///     404 Not Found is a matching Event is not found
        /// </returns>
        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
        public async Task<ActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                throw new HttpException(NotFound, nameof(Event));
            }

            Event item = await db.Events.FindAsync(id);

            if (item == null)
            {
                throw new HttpException(NotFound, nameof(Event));
            }

            EventViewModel viewModel = new EventViewModel
            {
                Date = item.Date.Date,
                Description = item.Description,
                Duration = item.Duration,
                Id = item.Id,
                Name = item.Name,
                Time = item.Date
            };

            return View(viewModel);
        }

        /// <summary>
        ///     Persists the edits to the event.
        /// </summary>
        /// <param name="editedEvent">
        ///     The view model for containing the details for the event being editted.
        /// </param>
        /// <returns>
        ///     Redirection to the Details page for the Event if successful
        ///     Redisplays the Edit page if the information isn't valid
        ///     404 Not Found if the information is valid but an Event matching the Id can't be found
        /// </returns>
        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,Name,Description,Date,Time,Duration")] EventViewModel editedEvent)
        {
            if (!ModelState.IsValid)
            {
                this.AddAlert(AlertType.Error, "Some Event information was invalid.");
                return View(editedEvent);
            }

            Event item = await db.Events.FindAsync(editedEvent.Id);

            if (item == null)
            {
                throw new HttpException(NotFound, nameof(Event));
            }

            item.Date = editedEvent.DateTime;
            item.Description = editedEvent.Description;
            item.Duration = editedEvent.Duration;
            item.Name = editedEvent.Name;

            db.MarkAsModified(item);

            await db.SaveChangesAsync();

            this.AddAlert(AlertType.Success, "Successfully edited the event.");

            return RedirectToAction("Details", new { Id = item.Id });
        }

        /// <summary>
        ///     Displays a delete confirmation page for the identified event
        /// </summary>
        /// <param name="id">
        ///     The Id of the event to delete
        /// </param>
        /// <returns>
        ///     The Delete confirmation page
        /// </returns>
        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
        public async Task<ActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                throw new HttpException(NotFound, nameof(Event));
            }

            Event item = await db.Events.FindAsync(id);

            if (item == null)
            {
                throw new HttpException(NotFound, nameof(Event));
            }

            return View(item);
        }

        /// <summary>
        ///     Deletes the identified event
        /// </summary>
        /// <param name="id">
        ///     The Id of the event to delete
        /// </param>
        /// <returns>
        ///     Redirection to Index with a success alert if successful
        ///     404 Not Found view if the identified event can't be found
        /// </returns>
        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(Guid id = default(Guid))
        {
            if (id == Guid.Empty)
            {
                this.AddAlert(AlertType.Error, "You must select an Event to delete.");
                return RedirectToAction("Index");
            }

            Event item = await db.Events.FindAsync(id);

            if (item == null)
            {
                throw new HttpException(NotFound, nameof(Event));
            }

            db.Events.Remove(item);

            await db.SaveChangesAsync();

            this.AddAlert(AlertType.Success, "Successfully deleted the event.");
            return RedirectToAction("Index");
        }
    }
}
