using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Health_Care_MIS.Filters;
using Health_Care_MIS.Models;

namespace Health_Care_MIS.Controllers
{
    [CustomAuthorize(Roles = "user, admin")]
    public class RegistrationsController : Controller
    {
        private Health_Care_MISEntities1 db = new Health_Care_MISEntities1();

        // GET: Registrations
        public ActionResult Index()
        {
            var registrations = db.Registrations
                .Include(r => r.SignUp)
                .AsNoTracking()
                .ToList();
            return View(registrations);
        }

        // GET: Registrations/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Registration registration = db.Registrations.Find(id);
            if (registration == null)
            {
                return HttpNotFound();
            }
            return View(registration);
        }

        // GET: Registrations/Create        
        public ActionResult Create()
        {
            // Get the current user's email
            string currentUserEmail = User.Identity.Name;

            // Find the user ID associated with this email
            var currentUser = db.SignUps.FirstOrDefault(u => u.email == currentUserEmail);

            if (currentUser != null)
            {
                // Create a new registration with pre-populated email
                var registration = new Registration
                {
                    email = currentUser.useId  // Assuming useId is the foreign key that links to SignUps
                };

                return View(registration);
            }

            // Fallback to default behavior if user not found
            ViewBag.email = new SelectList(db.SignUps, "useId", "email");
            return View();
        }

        // POST: Registrations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "PatientID,Firstname,Lastname,Date_of_birth,Gender,ContactInfo,Address,EmergencyContact,BloodType,InsuranceInfo,email")] Registration registration)
        {
            // If email is not set (for some reason), get the current user's email
            if (registration.email == 0) // Assuming email is an integer ID
            {
                string currentUserEmail = User.Identity.Name;
                var currentUser = db.SignUps.FirstOrDefault(u => u.email == currentUserEmail);

                if (currentUser != null)
                {
                    registration.email = currentUser.useId;
                }
            }

            if (ModelState.IsValid)
            {
                db.Registrations.Add(registration);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            // If we get here, there was an error. Just show the current user's email rather than all emails
            return View(registration);
        }

        // GET: Registrations/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Registration registration = db.Registrations.Find(id);
            if (registration == null)
            {
                return HttpNotFound();
            }
            ViewBag.email = new SelectList(db.SignUps, "useId", "email", registration.email);
            return View(registration);
        }

        // POST: Registrations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "PatientID,Firstname,Lastname,Date_of_birth,Gender,ContactInfo,Address,EmergencyContact,BloodType,InsuranceInfo,email")] Registration registration)
        {
            if (ModelState.IsValid)
            {
                db.Entry(registration).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.email = new SelectList(db.SignUps, "useId", "email", registration.email);
            return View(registration);
        }

        // GET: Registrations/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Registration registration = db.Registrations.Find(id);
            if (registration == null)
            {
                return HttpNotFound();
            }
            return View(registration);
        }

        // POST: Registrations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Registration registration = db.Registrations.Find(id);
            db.Registrations.Remove(registration);
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
