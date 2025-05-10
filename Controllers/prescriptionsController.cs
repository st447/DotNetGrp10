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
    [CustomAuthorize(Roles = "doctor, admin")]
    public class prescriptionsController : Controller
    {
        private Health_Care_MISEntities1 db = new Health_Care_MISEntities1();

        // GET: prescriptions
        public ActionResult Index()
        {
            try
            {
                var prescriptions = db.prescriptions
                    .Include(p => p.Medication1)
                    .Include(p => p.Registration)
                    .Include(p => p.Staff1)
                    .ToList();

                return View(prescriptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Index: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "Error loading prescriptions: " + ex.Message;
                return View(new List<prescription>());
            }
        }

        // GET: prescriptions/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            prescription prescription = db.prescriptions.Find(id);
            if (prescription == null)
            {
                return HttpNotFound();
            }
            return View(prescription);
        }

        // GET: prescriptions/Create
        [CustomAuthorize(Roles = "doctor")]
        public ActionResult Create()
        {
            try
            {
                ViewBag.medication = new SelectList(db.Medications, "id", "name");
                
                // First fetch the raw data
                var rawPatients = db.Registrations
                    .AsNoTracking()
                    .Select(r => new
                    {
                        r.PatientID,
                        r.Lastname,
                        r.Firstname
                    })
                    .ToList();

                // Then process the byte array after fetching from database
                var patients = rawPatients
                    .Select(r => new
                    {
                        r.PatientID,
                        DisplayName = $"{r.Lastname}, {(r.Firstname != null ? r.Firstname : string.Empty)}"
                    })
                    .OrderBy(r => r.DisplayName);

                // First fetch staff data
                var rawStaff = db.Staffs
                    .AsNoTracking()
                    .Select(s => new
                    {
                        s.StaffId,
                        s.Firstname,
                        s.LastName,
                        s.Specialisation
                    })
                    .ToList();

                // Then format the display name
                var staffList = rawStaff
                    .Select(s => new
                    {
                        s.StaffId,
                        DisplayName = s.Specialisation != null 
                            ? $"{s.Firstname} {s.LastName} - {s.Specialisation}"
                            : $"{s.Firstname} {s.LastName}"
                    })
                    .OrderBy(s => s.DisplayName);

                // Get consultations with patient info
                var consultations = db.consulations
                    .AsNoTracking()
                    .Include(c => c.Registration)
                    .Select(c => new
                    {
                        c.id,
                        PatientName = c.Registration.Lastname
                    })
                    .OrderByDescending(c => c.id)
                    .ToList()
                    .Select(c => new
                    {
                        c.id,
                        DisplayName = $"Consultation #{c.id} - {c.PatientName}"
                    });

                ViewBag.patient = new SelectList(patients, "PatientID", "DisplayName");
                ViewBag.staff = new SelectList(staffList, "StaffId", "DisplayName");
                ViewBag.conultation = new SelectList(consultations, "id", "DisplayName");
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

        // POST: prescriptions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "doctor")]
        public ActionResult Create([Bind(Include = "patient,staff,conultation,medication,Dosage,frequency,Duration")] prescription prescription)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Create a new prescription instance to avoid tracking issues
                    var newPrescription = new prescription
                    {
                        patient = prescription.patient,
                        staff = prescription.staff,
                        conultation = prescription.conultation,
                        medication = prescription.medication,
                        Dosage = prescription.Dosage?.Trim(),
                        frequency = prescription.frequency?.Trim(),
                        Duration = prescription.Duration
                    };

                    // Validate required fields
                    if (!newPrescription.patient.HasValue || newPrescription.patient.Value <= 0)
                    {
                        ModelState.AddModelError("patient", "Patient is required.");
                        return RedirectToCreate();
                    }

                    if (!newPrescription.staff.HasValue || newPrescription.staff.Value <= 0)
                    {
                        ModelState.AddModelError("staff", "Staff is required.");
                        return RedirectToCreate();
                    }

                    if (!newPrescription.medication.HasValue || newPrescription.medication.Value <= 0)
                    {
                        ModelState.AddModelError("medication", "Medication is required.");
                        return RedirectToCreate();
                    }

                    if (!newPrescription.conultation.HasValue || newPrescription.conultation.Value <= 0)
                    {
                        ModelState.AddModelError("consultation", "Consultation is required.");
                        return RedirectToCreate();
                    }

                    if (string.IsNullOrWhiteSpace(newPrescription.Dosage))
                    {
                        ModelState.AddModelError("Dosage", "Dosage is required.");
                        return RedirectToCreate();
                    }

                    if (string.IsNullOrWhiteSpace(newPrescription.frequency))
                    {
                        ModelState.AddModelError("frequency", "Frequency is required.");
                        return RedirectToCreate();
                    }

                    if (!newPrescription.Duration.HasValue || newPrescription.Duration.Value <= 0)
                    {
                        ModelState.AddModelError("Duration", "Duration must be greater than 0.");
                        return RedirectToCreate();
                    }

                    // Verify that patient exists
                    var patientExists = db.Registrations.Any(r => r.PatientID == newPrescription.patient.Value);
                    if (!patientExists)
                    {
                        ModelState.AddModelError("patient", "Selected patient does not exist.");
                        return RedirectToCreate();
                    }

                    // Verify that staff exists
                    var staffExists = db.Staffs.Any(s => s.StaffId == newPrescription.staff.Value);
                    if (!staffExists)
                    {
                        ModelState.AddModelError("staff", "Selected staff does not exist.");
                        return RedirectToCreate();
                    }

                    // Verify that medication exists
                    var medicationExists = db.Medications.Any(m => m.id == newPrescription.medication.Value);
                    if (!medicationExists)
                    {
                        ModelState.AddModelError("medication", "Selected medication does not exist.");
                        return RedirectToCreate();
                    }

                    // Verify that consultation exists
                    var consultationExists = db.consulations.Any(c => c.id == newPrescription.conultation.Value);
                    if (!consultationExists)
                    {
                        ModelState.AddModelError("consultation", "Selected consultation does not exist.");
                        return RedirectToCreate();
                    }

                    // Add and save
                    db.prescriptions.Add(newPrescription);
                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Prescription created successfully.";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Create POST: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                ModelState.AddModelError("", "Error creating prescription: " + ex.Message);
            }

            // If we got this far, something failed, redisplay form
            return RedirectToCreate();
        }

        // GET: prescriptions/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            prescription prescription = db.prescriptions.Find(id);
            if (prescription == null)
            {
                return HttpNotFound();
            }
            ViewBag.medication = new SelectList(db.Medications, "id", "name", prescription.medication);
            ViewBag.patient = new SelectList(db.Registrations, "PatientID", "Lastname", prescription.patient);
            ViewBag.staff = new SelectList(db.Staffs, "StaffId", "Firstname", prescription.staff);
            return View(prescription);
        }

        // POST: prescriptions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "id,consultation,patient,staff,medication,Dosage,frequency,Duration")] prescription prescription)
        {
            if (ModelState.IsValid)
            {
                db.Entry(prescription).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.medication = new SelectList(db.Medications, "id", "name", prescription.medication);
            ViewBag.patient = new SelectList(db.Registrations, "PatientID", "Lastname", prescription.patient);
            ViewBag.staff = new SelectList(db.Staffs, "StaffId", "Firstname", prescription.staff);
            return View(prescription);
        }

        // GET: prescriptions/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            prescription prescription = db.prescriptions.Find(id);
            if (prescription == null)
            {
                return HttpNotFound();
            }
            return View(prescription);
        }

        // POST: prescriptions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            prescription prescription = db.prescriptions.Find(id);
            db.prescriptions.Remove(prescription);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        private ActionResult RedirectToCreate()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Starting RedirectToCreate");
                
                // Get all patients
                var rawPatients = db.Registrations
                    .AsNoTracking()
                    .Select(r => new 
                    {
                        r.PatientID,
                        r.Firstname,
                        r.Lastname
                    })
                    .ToList();

                var patients = rawPatients
                    .Select(r => new SelectListItem
                    {
                        Value = r.PatientID.ToString(),
                        Text = (r.Firstname != null ? r.Firstname.Trim() : "") + " " + r.Lastname
                    });

                System.Diagnostics.Debug.WriteLine($"Found {patients.Count()} patients");
                ViewBag.patient = new SelectList(patients, "Value", "Text");

                // Get medications
                var medications = db.Medications
                    .AsNoTracking()
                    .Select(m => new SelectListItem
                    {
                        Value = m.id.ToString(),
                        Text = m.name
                    })
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Found {medications.Count} medications");
                ViewBag.medication = new SelectList(medications, "Value", "Text");

                // Get staff members
                var staffMembers = db.Staffs
                    .AsNoTracking()
                    .Select(s => new SelectListItem
                    {
                        Value = s.StaffId.ToString(),
                        Text = s.Firstname + " " + s.LastName + " (" + s.Role + ")"
                    })
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Found {staffMembers.Count} staff members");
                ViewBag.staff = new SelectList(staffMembers, "Value", "Text");

                // Get consultations
                var consultations = db.consulations
                    .AsNoTracking()
                    .Select(c => new 
                    {
                        Id = c.id,
                        Date = c.Date
                    })
                    .ToList()
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = string.Format("Consultation #{0} - {1}", 
                            c.Id, 
                            c.Date != null ? ((DateTime)c.Date).ToString("dd/MM/yyyy") : "No date")
                    });

                System.Diagnostics.Debug.WriteLine($"Found {consultations.Count()} consultations");
                ViewBag.conultation = new SelectList(consultations, "Value", "Text");

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
