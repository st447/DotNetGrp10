using System;
using System.Linq;
using System.Web.Mvc;
using Health_Care_MIS.Models;
using Health_Care_MIS.Filters;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;

namespace Health_Care_MIS.Controllers
{
    [CustomAuthorize(Roles = "admin")]
    public class AdminController : Controller
    {
        private readonly Health_Care_MISEntities1 db = new Health_Care_MISEntities1();

        // GET: Admin
        public ActionResult Dashboard()
        {
            try
            {
                var today = DateTime.Now;
                var model = new AdminDashboardViewModel
                {
                    Users = db.SignUps.ToList(),
                    TotalDoctors = db.Staffs.Count(s => s.Role.ToLower() == "doctor"),
                    TotalPatients = db.Registrations.Count(),
                    TotalAppointments = db.AppointMents.Count(),
                    TotalPrescriptions = db.prescriptions.Count(),
                    TodayAppointments = db.AppointMents
                        .Where(a => a.DateTime.HasValue)
                        .Count(a => EntityFunctions.TruncateTime(a.DateTime) == EntityFunctions.TruncateTime(today)),
                    ActiveDoctors = db.Staffs
                        .Count(s => s.Role.ToLower() == "doctor"),
                    PendingLabResults = db.laboratories
                        .Count(l => !db.laboratoryResults.Any(r => r.testId == l.testId))
                };

                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Admin Dashboard: {ex.Message}");
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateRole(int userId, string newRole)
        {
            var user = db.SignUps.Find(userId);
            if (user != null)
            {
                user.Role = newRole?.Trim().ToLower();
                db.SaveChanges();
                TempData["SuccessMessage"] = "Role updated successfully.";
            }
            return RedirectToAction("Dashboard");
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
