using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Health_Care_MIS.Models;
using Health_Care_MIS.Filters;
using System.Collections.Generic;
using System.Diagnostics;

namespace Health_Care_MIS.Controllers
{
    [CustomAuthorize(Roles = "doctor")]
    public class DoctorController : Controller
    {
        private readonly Health_Care_MISEntities1 db = new Health_Care_MISEntities1();

        private void PopulateStaffDropdown(string selectedFirstname = null)
        {
            try
            {
                var staffList = db.Staffs
                    .OrderBy(s => s.Firstname)
                    .ToList();

                var items = staffList.Select(s => new SelectListItem
                {
                    Value = s.StaffId.ToString(),
                    Text = s.Firstname,
                    Selected = selectedFirstname != null && s.Firstname == selectedFirstname
                }).ToList();

                items.Insert(0, new SelectListItem { Value = "", Text = "Select Staff" });
                ViewBag.StaffNames = new SelectList(items, "Value", "Text");

                Debug.WriteLine($"Populated dropdown with {items.Count - 1} staff members");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error populating staff dropdown: {ex.Message}");
                ViewBag.StaffNames = new SelectList(new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "Error loading staff" }
                }, "Value", "Text");
            }
        }

        // GET: Doctor/Dashboard
        public ActionResult Dashboard()
        {
            Debug.WriteLine("=== Starting Dashboard Action ===");
            try
            {
                Debug.WriteLine($"User Authenticated: {User.Identity.IsAuthenticated}");
                Debug.WriteLine($"User in Doctor Role: {User.IsInRole("doctor")}");

                PopulateStaffDropdown();

                var viewModel = new DoctorDashboardViewModel
                {
                    TodaysAppointments = new List<AppointMent>(),
                    RecentPrescriptions = new List<prescription>(),
                    RecentLabTests = new List<laboratory>(),
                    TotalPatients = 0,
                    PendingAppointments = 0
                };

                Debug.WriteLine("=== Successfully created view model, returning view ===");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("=== Error in Dashboard Action ===");
                Debug.WriteLine($"Error Message: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SelectDoctor(string StaffId)
        {
            Debug.WriteLine("=== Starting SelectDoctor Action ===");
            try
            {
                Debug.WriteLine($"SelectDoctor called with Staff ID: {StaffId}");

                if (string.IsNullOrEmpty(StaffId))
                {
                    Debug.WriteLine("No Staff ID provided, returning empty dashboard");
                    var emptyModel = new DoctorDashboardViewModel
                    {
                        TodaysAppointments = new List<AppointMent>(),
                        RecentPrescriptions = new List<prescription>(),
                        RecentLabTests = new List<laboratory>(),
                        TotalPatients = 0,
                        PendingAppointments = 0
                    };
                    return View("Dashboard", emptyModel);
                }

                int staffIdInt;
                if (!int.TryParse(StaffId, out staffIdInt))
                {
                    Debug.WriteLine("Invalid Staff ID format");
                    TempData["ErrorMessage"] = "Invalid staff ID format.";
                    return RedirectToAction("Dashboard");
                }

                Debug.WriteLine($"Looking up staff with ID: {staffIdInt}");

                // Load the selected staff with related data
                var selectedStaff = db.Staffs
                    .Include(s => s.AppointMents)
                    .Include(s => s.prescriptions)
                    .Include(s => s.laboratories)
                    .FirstOrDefault(s => s.StaffId == staffIdInt);

                Debug.WriteLine($"Selected Staff: {selectedStaff?.StaffId}, Name: {selectedStaff?.Firstname}");

                if (selectedStaff != null)
                {
                    var today = DateTime.Today;
                    Debug.WriteLine($"Calculating statistics for date: {today}");

                    // Load appointments separately with all related data
                    var username = User.Identity.Name;
                    var appointMents = db.AppointMents
                        .Include(a => a.Staff1)
                        .Include(a => a.Room)
                        .Include(a => a.Registration)
                        .Where(a => a.Staff == staffIdInt && 
                               a.DateTime.HasValue && 
                               DbFunctions.TruncateTime(a.DateTime.Value) == today)
                        .Select(a => new
                        {
                            a.id,
                            a.Patient,
                            a.Staff,
                            a.DateTime,
                            a.Description,
                            a.DefaultRoom,
                            a.Staff1,
                            a.Room,
                            Registration = a.Registration != null ? new { a.Registration.Firstname } : null
                        })
                        .OrderBy(a => a.DateTime)
                        .ToList();

                    var appointments = appointMents.Select(a => new AppointMent
                    {
                        id = a.id,
                        Patient = a.Patient,
                        Staff = a.Staff,
                        DateTime = a.DateTime,
                        Description = a.Description,
                        DefaultRoom = a.DefaultRoom,
                        Staff1 = a.Staff1,
                        Room = a.Room,
                        Registration = a.Registration != null ? new Registration { Firstname = a.Registration.Firstname } : null
                    }).ToList();

                    Debug.WriteLine($"Found {appointments.Count()} appointments for today");

                    // Count unique patients using Patient field
                    var totalPatients = db.AppointMents
                        .Where(a => a.Staff == staffIdInt && a.Patient.HasValue)
                        .Select(a => a.Patient.Value)
                        .Distinct()
                        .Count();

                    Debug.WriteLine($"Total unique patients: {totalPatients}");

                    // Count pending appointments (future appointments)
                    var pendingAppointments = db.AppointMents
                        .Count(a => a.Staff == staffIdInt && 
                                  a.DateTime.HasValue && 
                                  a.DateTime.Value > DateTime.Now);

                    Debug.WriteLine($"Pending appointments: {pendingAppointments}");

                    // Load prescriptions separately with related data
                    var recentPrescriptions = db.prescriptions
                        .Include(p => p.Registration)
                        .Include(p => p.Staff1)
                        .Include(p => p.Medication1)
                        .Where(p => p.staff == staffIdInt)
                        .OrderByDescending(p => p.id)
                        .Take(5)
                        .AsEnumerable()  // Evaluate the query here to prevent proxy issues
                        .Select(p => new prescription
                        {
                            id = p.id,
                            staff = p.staff,
                            patient = p.patient,
                            Dosage = p.Dosage,
                            frequency = p.frequency,
                            Medication1 = p.Medication1,
                            Registration = p.Registration  // Direct assignment since it's now string type
                        })
                        .ToList();

                    Debug.WriteLine($"Found {recentPrescriptions.Count()} recent prescriptions");

                    // Load lab tests separately with related data
                    var recentLabTests = db.laboratories
                        .Include(l => l.Registration)
                        .Include(l => l.Staff1)
                        .Where(l => l.staff == staffIdInt)
                        .OrderByDescending(l => l.orderDate)
                        .Take(5)
                        .AsEnumerable()  // Evaluate the query here to prevent proxy issues
                        .Select(l => new laboratory
                        {
                            testId = l.testId,
                            staff = l.staff,
                            patient = l.patient,
                            testType = l.testType,
                            orderDate = l.orderDate,
                            Registration = l.Registration,  // Direct assignment since it's now string type
                            Staff1 = l.Staff1
                        })
                        .ToList();

                    Debug.WriteLine($"Found {recentLabTests.Count()} recent lab tests");

                    // Create view model with loaded data
                    var viewModel = new DoctorDashboardViewModel
                    {
                        Staff = selectedStaff,
                        TodaysAppointments = appointments,
                        RecentPrescriptions = recentPrescriptions,
                        RecentLabTests = recentLabTests,
                        TotalPatients = totalPatients,
                        PendingAppointments = pendingAppointments
                    };

                    PopulateStaffDropdown(selectedStaff.Firstname);
                    Debug.WriteLine("=== Successfully created view model with data ===");
                    return View("Dashboard", viewModel);
                }
                else
                {
                    Debug.WriteLine("Staff not found");
                    TempData["ErrorMessage"] = "Selected staff member not found.";
                    return RedirectToAction("Dashboard");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("=== Error in SelectDoctor Action ===");
                Debug.WriteLine($"Error Message: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "An error occurred while loading the staff dashboard: " + ex.Message;
                return RedirectToAction("Dashboard");
            }
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
