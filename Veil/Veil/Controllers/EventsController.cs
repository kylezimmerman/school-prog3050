using System;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Net;
using System.Web.Mvc;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;

namespace Veil.Controllers
{
    public class EventsController : BaseController
    {
        protected readonly IVeilDataAccess db;

        public EventsController(IVeilDataAccess veilDataAccess)
        {
            db = veilDataAccess;
        }

        // GET: Events
        public async Task<ActionResult> Index()
        {
            return View(await db.Events.ToListAsync());
        }

        // GET: Events/Details/5
        public async Task<ActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Event theEvent = await db.Events.FindAsync(id);
            /*if (theEvent == null)
            {
                return HttpNotFound();
            }*/

            // TODO: Remove this and add back DB usage
            theEvent = new Event();
            return View(theEvent);
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

        // TODO: Member only page
        public async Task<ActionResult> MyEvents()
        {
            return View("Index", await db.Events.ToListAsync());
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
        public async Task<ActionResult> Create([Bind(Include = "Id,Name,Description,Date,Duration")] Event @event)
        {
            if (ModelState.IsValid)
            {
                @event.Id = Guid.NewGuid();
                db.Events.Add(@event);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(@event);
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
