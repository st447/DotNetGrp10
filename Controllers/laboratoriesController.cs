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
    [CustomAuthorize(Roles = "doctor")]
    public class laboratoriesController : Controller
    {
        private Health_Care_MISEntities1 db = new Health_Care_MISEntities1();

        // GET: laboratories
        public ActionResult Index()
        {
            try
            {
                var laboratories = db.laboratories
                    .AsNoTracking()
                    .Include(l => l.Registration)
                    .Include(l => l.Staff1)
                    .Select(l => new
                    {
                        TestId = l.testId,
                        Patient = l.patient,
                        Staff = l.staff,
                        TestType = l.testType,
                        OrderDate = l.orderDate,
                        RegLastname = l.Registration.Lastname,
                        RegFirstname = l.Registration.Firstname,
                        RegDOB = (DateTime?)l.Registration.Date_of_birth,  // Cast to nullable DateTime
                        StaffFirstname = l.Staff1.Firstname,
                        StaffLastName = l.Staff1.LastName,
                        StaffSpec = l.Staff1.Specialisation
                    })
                    .ToList()  // Execute query here
                    .Select(x => new laboratory  // Then create objects in memory
                    {
                        testId = x.TestId,
                        patient = x.Patient,
                        staff = x.Staff,
                        testType = x.TestType,
                        orderDate = x.OrderDate,
                        Registration = new Registration 
                        { 
                            Lastname = x.RegLastname,
                            Firstname = x.RegFirstname,
                            Date_of_birth = x.RegDOB.GetValueOrDefault(DateTime.Now)  // Provide default value
                        },
                        Staff1 = new Staff 
                        { 
                            Firstname = x.StaffFirstname,
                            LastName = x.StaffLastName,
                            Specialisation = x.StaffSpec
                        }
                    })
                    .ToList();

                return View(laboratories);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Index: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "Error loading laboratory data: " + ex.Message;
                return View(new List<laboratory>());
            }
        }

        // GET: laboratories/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            try
            {
                var laboratory = db.laboratories
                    .AsNoTracking()
                    .Include(l => l.Registration)
                    .Include(l => l.Staff1)
                    .Where(l => l.testId == id)
                    .Select(l => new
                    {
                        TestId = l.testId,
                        Patient = l.patient,
                        Staff = l.staff,
                        TestType = l.testType,
                        OrderDate = l.orderDate,
                        RegLastname = l.Registration.Lastname,
                        RegFirstname = l.Registration.Firstname,
                        RegDOB = (DateTime?)l.Registration.Date_of_birth,  // Cast to nullable DateTime
                        StaffFirstname = l.Staff1.Firstname,
                        StaffLastName = l.Staff1.LastName,
                        StaffSpec = l.Staff1.Specialisation
                    })
                    .ToList()
                    .Select(x => new laboratory
                    {
                        testId = x.TestId,
                        patient = x.Patient,
                        staff = x.Staff,
                        testType = x.TestType,
                        orderDate = x.OrderDate,
                        Registration = new Registration 
                        { 
                            Lastname = x.RegLastname,
                            Firstname = x.RegFirstname,
                            Date_of_birth = x.RegDOB.GetValueOrDefault(DateTime.Now)  // Provide default value
                        },
                        Staff1 = new Staff 
                        { 
                            Firstname = x.StaffFirstname,
                            LastName = x.StaffLastName,
                            Specialisation = x.StaffSpec
                        }
                    })
                    .FirstOrDefault();

                if (laboratory == null)
                {
                    return HttpNotFound();
                }

                return View(laboratory);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Details: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "Error loading laboratory details: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // GET: laboratories/Create
        public ActionResult Create()
        {
            try
            {
                // Get patients with their names
                var patients = db.Registrations
                    .AsNoTracking()
                    .Select(r => new
                    {
                        r.PatientID,
                        r.Lastname,
                        r.Firstname,
                        Date_of_birth = r.Date_of_birth != null ? r.Date_of_birth : DateTime.Now // Provide default value
                    })
                    .ToList()
                    .Select(r => new SelectListItem
                    {
                        Value = r.PatientID.ToString(),
                        Text = $"{r.Lastname}, {(r.Firstname != null ? r.Firstname : string.Empty)}"
                    });

                // Get staff with specialization
                var staffList = db.Staffs
                    .AsNoTracking()
                    .Select(s => new
                    {
                        s.StaffId,
                        s.Firstname,
                        s.LastName,
                        s.Specialisation
                    })
                    .ToList()
                    .Select(s => new SelectListItem
                    {
                        Value = s.StaffId.ToString(),
                        Text = string.Format("{0} {1}{2}", 
                            s.Firstname, 
                            s.LastName,
                            s.Specialisation != null ? " - " + s.Specialisation : string.Empty)
                    });

                ViewBag.patient = new SelectList(patients, "Value", "Text");
                ViewBag.staff = new SelectList(staffList, "Value", "Text");
                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Create GET: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "Error loading form data: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: laboratories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "testId,patient,staff,testType,orderDate")] laboratory laboratory)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (!laboratory.patient.HasValue || laboratory.patient.Value <= 0)
                    {
                        ModelState.AddModelError("patient", "Patient is required.");
                        return RedirectToCreate();
                    }

                    if (!laboratory.staff.HasValue || laboratory.staff.Value <= 0)
                    {
                        ModelState.AddModelError("staff", "Staff is required.");
                        return RedirectToCreate();
                    }

                    if (string.IsNullOrWhiteSpace(laboratory.testType))
                    {
                        ModelState.AddModelError("testType", "Test Type is required.");
                        return RedirectToCreate();
                    }

                    // Set order date to current date if not provided
                    if (!laboratory.orderDate.HasValue)
                    {
                        laboratory.orderDate = DateTime.Now;
                    }

                    db.laboratories.Add(laboratory);
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Laboratory test created successfully.";
                    return RedirectToAction("Index");
                }

                // If we got this far, something failed, redisplay form
                return RedirectToCreate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Create POST: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "Error creating laboratory test: " + ex.Message;
                return RedirectToCreate();
            }
        }

        private ActionResult RedirectToCreate()
        {
            try
            {
                // Get patients with their names
                var patients = db.Registrations
                    .AsNoTracking()
                    .Select(r => new
                    {
                        r.PatientID,
                        r.Lastname,
                        r.Firstname,
                        Date_of_birth = r.Date_of_birth != null ? r.Date_of_birth : DateTime.Now // Provide default value
                    })
                    .ToList()
                    .Select(r => new SelectListItem
                    {
                        Value = r.PatientID.ToString(),
                        Text = $"{r.Lastname}, {(r.Firstname != null ? r.Firstname : string.Empty)}"
                    });

                // Get staff with specialization
                var staffList = db.Staffs
                    .AsNoTracking()
                    .Select(s => new
                    {
                        s.StaffId,
                        s.Firstname,
                        s.LastName,
                        s.Specialisation
                    })
                    .ToList()
                    .Select(s => new SelectListItem
                    {
                        Value = s.StaffId.ToString(),
                        Text = string.Format("{0} {1}{2}", 
                            s.Firstname, 
                            s.LastName,
                            s.Specialisation != null ? " - " + s.Specialisation : string.Empty)
                    });

                ViewBag.patient = new SelectList(patients, "Value", "Text");
                ViewBag.staff = new SelectList(staffList, "Value", "Text");
                return View("Create");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in RedirectToCreate: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "Error loading form data: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // GET: laboratories/Edit/5
        public ActionResult Edit(int? id)
        {
            try
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                laboratory laboratory = db.laboratories.Find(id);
                if (laboratory == null)
                {
                    return HttpNotFound();
                }

                // First fetch the raw data
                var rawPatients = db.Registrations
                    .AsNoTracking()
                    .Select(r => new
                    {
                        r.PatientID,
                        r.Lastname,
                        r.Firstname,
                        r.Date_of_birth
                    })
                    .ToList();

                // Then process the patient names
                var patients = rawPatients
                    .Select(r => new
                    {
                        r.PatientID,
                        DisplayName = $"{r.Lastname}, {(r.Firstname != null ? r.Firstname : string.Empty)}"
                    })
                    .OrderBy(r => r.DisplayName);

                // Get staff with specialization
                var staffList = db.Staffs
                    .AsNoTracking()
                    .Select(s => new
                    {
                        s.StaffId,
                        s.Firstname,
                        s.LastName,
                        s.Specialisation
                    })
                    .ToList()
                    .Select(s => new
                    {
                        s.StaffId,
                        DisplayName = string.Format("{0} {1}{2}", 
                            s.Firstname, 
                            s.LastName,
                            s.Specialisation != null ? " - " + s.Specialisation : string.Empty)
                    })
                    .OrderBy(s => s.DisplayName);

                ViewBag.patient = new SelectList(patients, "PatientID", "DisplayName", laboratory.patient);
                ViewBag.staff = new SelectList(staffList, "StaffId", "DisplayName", laboratory.staff);
                return View(laboratory);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Edit GET: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "Error loading laboratory test: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: laboratories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "testId,patient,staff,testType,orderDate")] laboratory laboratory)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (!laboratory.patient.HasValue || laboratory.patient.Value <= 0)
                    {
                        ModelState.AddModelError("patient", "Patient is required.");
                        return RedirectToEdit(laboratory.testId);
                    }

                    if (!laboratory.staff.HasValue || laboratory.staff.Value <= 0)
                    {
                        ModelState.AddModelError("staff", "Staff is required.");
                        return RedirectToEdit(laboratory.testId);
                    }

                    if (string.IsNullOrWhiteSpace(laboratory.testType))
                    {
                        ModelState.AddModelError("testType", "Test Type is required.");
                        return RedirectToEdit(laboratory.testId);
                    }

                    // Set order date to current date if not provided
                    if (!laboratory.orderDate.HasValue)
                    {
                        laboratory.orderDate = DateTime.Now;
                    }

                    db.Entry(laboratory).State = EntityState.Modified;
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Laboratory test updated successfully.";
                    return RedirectToAction("Index");
                }

                return RedirectToEdit(laboratory.testId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Edit POST: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "Error updating laboratory test: " + ex.Message;
                return RedirectToEdit(laboratory.testId);
            }
        }

        private ActionResult RedirectToEdit(int? id)
        {
            try
            {
                // Get patients with their names
                var patients = db.Registrations
                    .AsNoTracking()
                    .Select(r => new
                    {
                        r.PatientID,
                        r.Lastname,
                        r.Firstname,
                        r.Date_of_birth
                    })
                    .ToList()
                    .Select(r => new SelectListItem
                    {
                        Value = r.PatientID.ToString(),
                        Text = $"{r.Lastname}, {(r.Firstname != null ? r.Firstname : string.Empty)}"
                    });

                // Get staff with specialization
                var staffList = db.Staffs
                    .AsNoTracking()
                    .Select(s => new
                    {
                        s.StaffId,
                        s.Firstname,
                        s.LastName,
                        s.Specialisation
                    })
                    .ToList()
                    .Select(s => new
                    {
                        s.StaffId,
                        DisplayName = string.Format("{0} {1}{2}", 
                            s.Firstname, 
                            s.LastName,
                            s.Specialisation != null ? " - " + s.Specialisation : string.Empty)
                    })
                    .OrderBy(s => s.DisplayName);

                ViewBag.patient = new SelectList(patients, "Value", "Text");
                ViewBag.staff = new SelectList(staffList, "StaffId", "DisplayName");

                // Get the laboratory test
                var laboratory = db.laboratories
                    .AsNoTracking()
                    .Include(l => l.Registration)
                    .Include(l => l.Staff1)
                    .FirstOrDefault(l => l.testId == id);

                if (laboratory == null)
                {
                    TempData["ErrorMessage"] = "Laboratory test not found.";
                    return RedirectToAction("Index");
                }

                return View("Edit", laboratory);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in RedirectToEdit: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "Error loading form data: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // GET: laboratories/Delete/5
        public ActionResult Delete(int? id)
        {
            try
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }

                var laboratory = db.laboratories
                    .AsNoTracking()
                    .Include(l => l.Registration)
                    .Include(l => l.Staff1)
                    .Where(l => l.testId == id)
                    .Select(l => new
                    {
                        Laboratory = l,
                        Registration = l.Registration,
                        Staff = l.Staff1
                    })
                    .ToList()
                    .Select(x => new laboratory
                    {
                        testId = x.Laboratory.testId,
                        patient = x.Laboratory.patient,
                        staff = x.Laboratory.staff,
                        testType = x.Laboratory.testType,
                        orderDate = x.Laboratory.orderDate,
                        Registration = new Registration 
                        { 
                            Lastname = x.Registration.Lastname,
                            Firstname = x.Registration.Firstname,
                            Date_of_birth = x.Registration.Date_of_birth == default(DateTime) ? DateTime.Now : x.Registration.Date_of_birth
                        },
                        Staff1 = new Staff 
                        { 
                            Firstname = x.Staff.Firstname,
                            LastName = x.Staff.LastName,
                            Specialisation = x.Staff.Specialisation
                        }
                    })
                    .FirstOrDefault();

                if (laboratory == null)
                {
                    return HttpNotFound();
                }

                return View(laboratory);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Delete GET: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "Error loading laboratory test: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: laboratories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                laboratory laboratory = db.laboratories.Find(id);
                db.laboratories.Remove(laboratory);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Laboratory test deleted successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Delete POST: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "Error deleting laboratory test: " + ex.Message;
                return RedirectToAction("Index");
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
