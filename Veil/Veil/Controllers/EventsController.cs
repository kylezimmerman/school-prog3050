using System;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Net;
using System.Web.Mvc;
using System.Linq;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Models;
using Veil.Services;
using Veil.Extensions;
using System.Web;

namespace Veil.Controllers
{
    public class EventsController : BaseController
    {
        private readonly IVeilDataAccess db;
        private readonly VeilUserManager userManager;

        public EventsController(IVeilDataAccess veilDataAccess, VeilUserManager userManager)
        {
            db = veilDataAccess;
            this.userManager = userManager;
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
        [Authorize(Roles = "Member")]
        public async Task<ActionResult> MyEvents()
        {
            Guid currentUserId = IIdentityExtensions.GetUserId(User.Identity);
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

            Guid currentUserId = IIdentityExtensions.GetUserId(User.Identity);
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

            Guid currentUserId = IIdentityExtensions.GetUserId(User.Identity);
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
        [Authorize(Roles = "Member")]
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

            Guid currentUserId = IIdentityExtensions.GetUserId(User.Identity);
            Member currentMember = await db.Members.FindAsync(currentUserId);

            currentMember.RegisteredEvents.Add(currentEvent);
            db.MarkAsModified(currentMember);

            await db.SaveChangesAsync();

            var model = new EventDetailsViewModel
            {
                Event = currentEvent,
                CurrentMemberIsRegistered = currentEvent.RegisteredMembers.Contains(currentMember)
            };

            return View("Details", model);
        }

        // GET: Events/Unregister/5
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
        [Authorize(Roles = "Member")]
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

            Guid currentUserId = IIdentityExtensions.GetUserId(User.Identity);
            Member currentMember = await db.Members.FindAsync(currentUserId);

            currentMember.RegisteredEvents.Remove(currentEvent);
            db.MarkAsModified(currentMember);

            await db.SaveChangesAsync();

            var model = new EventDetailsViewModel
            {
                Event = currentEvent,
                CurrentMemberIsRegistered = currentEvent.RegisteredMembers.Contains(currentMember)
            };

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
            return RedirectToAction("Index");
        }

        // GET: Events/Edit/5
        public async Task<ActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Event @event = await db.Events.FindAsync(id);
            if (@event == null)
            {
                return HttpNotFound();
            }
            return View(@event);
        }

        // POST: Events/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,Name,Description,Date,Duration")] Event editedEvent)
        {
            if (ModelState.IsValid)
            {
                db.MarkAsModified(editedEvent);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(editedEvent);
        }

        // GET: Events/Delete/5
        public async Task<ActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Event @event = await db.Events.FindAsync(id);
            if (@event == null)
            {
                return HttpNotFound();
            }
            return View(@event);
        }

        // POST: Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(Guid id)
        {
            Event @event = await db.Events.FindAsync(id);
            db.Events.Remove(@event);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
