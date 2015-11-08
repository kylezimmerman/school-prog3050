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

            model.CurrentMemberIsRegistered =
                currentMember.RegisteredEvents.Contains(model.Event);

            return PartialView("_EventListItem", model);
        }

        // GET: Events/Details/5
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

        // TODO: Member only action
        public async Task<ActionResult> Register(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Member currentMember = new Member();
            Event currentEvent = await db.Events.FindAsync(id);

            currentMember.RegisteredEvents.Add(currentEvent);
            db.MarkAsModified(currentMember);

            await db.SaveChangesAsync();

            return View("Details", currentEvent);
        }

        // TODO: Member only action
        public async Task<ActionResult> Unregister(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Member currentMember = new Member();
            Event currentEvent = await db.Events.FindAsync(id);


            currentMember.RegisteredEvents.Remove(currentEvent);
            db.MarkAsModified(currentMember);

            await db.SaveChangesAsync();

            return View("Details", currentEvent);
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
