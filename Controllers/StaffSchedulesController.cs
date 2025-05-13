using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Health_Care_MIS.Models;
using Health_Care_MIS.Filters;

namespace Health_Care_MIS.Controllers
{
    [CustomAuthorize(Roles = "admin")]
    public class StaffSchedulesController : Controller
    {
        private Health_Care_MISEntities1 db = new Health_Care_MISEntities1();

        // GET: StaffSchedules
        public ActionResult Index()
        {
            var staffSchedules = db.StaffSchedules.Include(s => s.Staff1);
            return View(staffSchedules.ToList());
        }

        // GET: StaffSchedules/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            StaffSchedule staffSchedule = db.StaffSchedules.Find(id);
            if (staffSchedule == null)
            {
                return HttpNotFound();
            }
            return View(staffSchedule);
        }

        // GET: StaffSchedules/Create
        public ActionResult Create()
        {
            ViewBag.Staff = new SelectList(db.Staffs, "StaffId", "Firstname");
            return View();
        }

        // POST: StaffSchedules/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Staff,ShiftStart,ShifEcnd")] StaffSchedule staffSchedule)
        {
            if (ModelState.IsValid)
            {
                db.StaffSchedules.Add(staffSchedule);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Staff = new SelectList(db.Staffs, "StaffId", "Firstname", staffSchedule.Staff);
            return View(staffSchedule);
        }

        // GET: StaffSchedules/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            StaffSchedule staffSchedule = db.StaffSchedules.Find(id);
            if (staffSchedule == null)
            {
                return HttpNotFound();
            }
            ViewBag.Staff = new SelectList(db.Staffs, "StaffId", "Firstname", staffSchedule.Staff);
            return View(staffSchedule);
        }

        // POST: StaffSchedules/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Staff,ShiftStart,ShifEcnd")] StaffSchedule staffSchedule)
        {
            if (ModelState.IsValid)
            {
                db.Entry(staffSchedule).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Staff = new SelectList(db.Staffs, "StaffId", "Firstname", staffSchedule.Staff);
            return View(staffSchedule);
        }

        // GET: StaffSchedules/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            StaffSchedule staffSchedule = db.StaffSchedules.Find(id);
            if (staffSchedule == null)
            {
                return HttpNotFound();
            }
            return View(staffSchedule);
        }

        // POST: StaffSchedules/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            StaffSchedule staffSchedule = db.StaffSchedules.Find(id);
            db.StaffSchedules.Remove(staffSchedule);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
