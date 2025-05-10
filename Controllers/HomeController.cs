using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Health_Care_MIS.Models;
using Health_Care_MIS.Filters;
using System.Net.Mail;

namespace Health_Care_MIS.Controllers
{    
    [CustomAuthorize(Roles = "user")]
    public class HomeController : Controller
    {
        private readonly Health_Care_MISEntities1 db = new Health_Care_MISEntities1();

        [AllowAnonymous]
        public ActionResult Index()
        {
            var model = new DashboardViewModel
            {
                UserName = User.Identity.IsAuthenticated ? User.Identity.Name : string.Empty
            };

            if (User.Identity.IsAuthenticated)
            {
                try
                {
                    var username = User.Identity.Name;
                    var user = db.SignUps.FirstOrDefault(u => u.email == username);
                    
                    if (user == null)
                    {
                        FormsAuthentication.SignOut();
                        return RedirectToAction("Login", "Account");
                    }

                    var userRole = user.Role?.Trim().ToLower() ?? "user";
                    
                    // Get user details from Registration if available
                    var registration = db.SignUps.AsNoTracking()
                        .Where(s => s.email.Equals(username))
                        .Join(db.Registrations,
                            s => s.useId,
                            r => r.email,
                            (s, r) => r)
                        .FirstOrDefault();

                    if (registration != null)
                    {
                        model.FirstName = registration.Firstname != null ? 
                            registration.Firstname : string.Empty;
                        model.LastName = registration.Lastname ?? string.Empty;
                        model.Gender = registration.Gender ?? string.Empty;
                        model.ContactInfo = registration.ContactInfo ?? string.Empty;

                        // Load Appointments
                        var appointments = db.AppointMents
                            .Include(a => a.Staff1)
                            .Include(a => a.Room)
                            .Where(a => a.Patient == registration.PatientID)
                            .OrderByDescending(a => a.DateTime)
                            .Take(5)
                            .ToList();
                        model.Appointments = appointments;
                        model.TotalAppointments = appointments.Count;

                        // Load Prescriptions
                        var prescriptions = db.prescriptions
                            .Include(p => p.Staff1)
                            .Include(p => p.Medication1)
                            .Where(p => p.patient == registration.PatientID)
                            .OrderByDescending(p => p.id)
                            .Take(5)
                            .AsEnumerable()
                            .Select(p => new prescription
                            {
                                id = p.id,
                                staff = p.staff,
                                patient = p.patient,
                                Dosage = p.Dosage,
                                frequency = p.frequency,
                                Medication1 = p.Medication1,
                                Staff1 = p.Staff1
                            })
                            .ToList();
                        model.Prescriptions = prescriptions;
                        model.TotalPrescriptions = prescriptions.Count;

                        // Load Lab Results
                        var labResults = db.laboratories
                            .Include(l => l.Staff1)
                            .Where(l => l.patient == registration.PatientID)
                            .OrderByDescending(l => l.orderDate)
                            .Take(5)
                            .AsEnumerable()
                            .Select(l => new laboratory
                            {
                                testId = l.testId,
                                staff = l.staff,
                                patient = l.patient,
                                testType = l.testType,
                                orderDate = l.orderDate,
                                Staff1 = l.Staff1
                            })
                            .ToList();
                        model.LabResults = labResults;
                        model.TotalLabResults = labResults.Count;

                        // Load Bills and Calculate Statistics
                        var bills = db.bills
                            .Where(b => b.patient == registration.PatientID)
                            .ToList();
                        
                        model.PaidBills = bills.Count(b => b.status?.ToLower() == "paid");
                        model.UnpaidBills = bills.Count(b => b.status?.ToLower() == "unpaid");
                        model.PaidAmount = bills.Where(b => b.status?.ToLower() == "paid")
                            .Sum(b => (decimal)(b.price ?? 0));
                        model.UnpaidAmount = bills.Where(b => b.status?.ToLower() == "unpaid")
                            .Sum(b => (decimal)(b.price ?? 0));
                        model.TotalBillAmount = model.UnpaidAmount;

                        // Load Available Rooms
                        model.AvailableRooms = db.Rooms
                            .Where(r => r.Status == "Available")
                            .Take(5)
                            .ToList();
                    }
                    else
                    {
                        // If no registration found, use email as name
                        var emailParts = user.email.Split('@');
                        model.FirstName = emailParts[0];
                        model.LastName = string.Empty;
                    }

                    model.UserRole = userRole;
                    model.UserName = user.email;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in Home/Index: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }

            return View(model);
        }

        [AllowAnonymous]
        public ActionResult Welcome()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "All about YEAUS SMART CLINIC";
            return View();
        }

        [AllowAnonymous]
        public ActionResult Contact()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Contact(ContactFormModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get email settings from Web.config
                    var host = System.Configuration.ConfigurationManager.AppSettings["EmailHost"];
                    var port = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["EmailPort"]);
                    var enableSsl = Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["EmailSsl"]);
                    var fromEmail = System.Configuration.ConfigurationManager.AppSettings["EmailFrom"];
                    var username = System.Configuration.ConfigurationManager.AppSettings["EmailUser"];
                    var password = System.Configuration.ConfigurationManager.AppSettings["EmailPassword"];
                    var displayName = System.Configuration.ConfigurationManager.AppSettings["EmailDisplayName"];

                    // Configure the email client
                    using (var client = new SmtpClient())
                    {
                        client.Host = host;
                        client.Port = port;
                        client.EnableSsl = enableSsl;
                        client.DeliveryMethod = SmtpDeliveryMethod.Network;
                        client.UseDefaultCredentials = false;
                        client.Credentials = new NetworkCredential(username, password);

                        // Create the email message
                        using (var message = new MailMessage())
                        {
                            message.From = new MailAddress(fromEmail, displayName);
                            message.To.Add(new MailAddress(model.Recipient));
                            message.ReplyToList.Add(new MailAddress(model.Email, model.Name));
                            message.Subject = "Contact Form: " + model.Subject;

                            // Create a nice looking message body
                            var body = new StringBuilder();
                            body.AppendLine("<html><body>");
                            body.AppendLine("<h2>New Contact Form Submission</h2>");
                            body.AppendLine("<hr>");
                            body.AppendLine($"<p><strong>From:</strong> {model.Name} ({model.Email})</p>");
                            body.AppendLine($"<p><strong>Subject:</strong> {model.Subject}</p>");
                            body.AppendLine("<p><strong>Message:</strong></p>");
                            body.AppendLine($"<p>{model.Message.Replace("\n", "<br>")}</p>");
                            body.AppendLine("<hr>");
                            body.AppendLine("<p><em>This email was sent from the YEAUS Smart Clinic contact form.</em></p>");
                            body.AppendLine("</body></html>");

                            message.Body = body.ToString();
                            message.IsBodyHtml = true;

                            // Send the email
                            client.Send(message);
                        }
                    }

                    // Show success message
                    TempData["SuccessMessage"] = "Your message has been sent successfully! We'll get back to you soon.";
                    return RedirectToAction("Contact");
                }
                catch (Exception ex)
                {
                    // Log the error
                    System.Diagnostics.Debug.WriteLine($"Email Error: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                    // Show error message
                    ModelState.AddModelError("", "Failed to send email. Please try again later or contact us directly.");
                }
            }

            // If we got this far, something failed; redisplay form
            return View(model);
        }

        [AllowAnonymous]
        public ActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public ActionResult GetAppointments()
        {
            try
            {
                var username = User.Identity.Name;
                var registration = db.SignUps.AsNoTracking()
                    .Where(s => s.email.Equals(username))
                    .Join(db.Registrations,
                        s => s.useId,
                        r => r.email,
                        (s, r) => r)
                    .FirstOrDefault();

                if (registration != null)
                {
                    var appointments = db.AppointMents
                        .Include(a => a.Staff1)
                        .Include(a => a.Room)
                        .Where(a => a.Patient == registration.PatientID)
                        .OrderByDescending(a => a.DateTime)
                        .Take(5)
                        .ToList();

                    return PartialView("_AppointmentsPartial", appointments);
                }
                return Content("No appointments found.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetAppointments: {ex.Message}");
                return Content("Error loading appointments.");
            }
        }

        [HttpGet]
        public ActionResult GetPrescriptions()
        {
            try
            {
                var username = User.Identity.Name;
                var registration = db.SignUps.AsNoTracking()
                    .Where(s => s.email.Equals(username))
                    .Join(db.Registrations,
                        s => s.useId,
                        r => r.email,
                        (s, r) => r)
                    .FirstOrDefault();

                if (registration != null)
                {
                    var prescriptions = db.prescriptions
                        .Include(p => p.Staff1)
                        .Include(p => p.Medication1)
                        .Where(p => p.patient == registration.PatientID)
                        .OrderByDescending(p => p.id)
                        .Take(5)
                        .AsEnumerable()
                        .Select(p => new prescription
                        {
                            id = p.id,
                            staff = p.staff,
                            patient = p.patient,
                            Dosage = p.Dosage,
                            frequency = p.frequency,
                            Medication1 = p.Medication1,
                            Staff1 = p.Staff1
                        })
                        .ToList();

                    return PartialView("_PrescriptionsPartial", prescriptions);
                }
                return Content("No prescriptions found.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetPrescriptions: {ex.Message}");
                return Content("Error loading prescriptions.");
            }
        }

        [HttpGet]
        public ActionResult GetLabResults()
        {
            try
            {
                var username = User.Identity.Name;
                var registration = db.SignUps.AsNoTracking()
                    .Where(s => s.email.Equals(username))
                    .Join(db.Registrations,
                        s => s.useId,
                        r => r.email,
                        (s, r) => r)
                    .FirstOrDefault();

                if (registration != null)
                {
                    var labResults = db.laboratories
                        .Include(l => l.Staff1)
                        .Where(l => l.patient == registration.PatientID)
                        .OrderByDescending(l => l.orderDate)
                        .Take(5)
                        .AsEnumerable()
                        .Select(l => new laboratory
                        {
                            testId = l.testId,
                            staff = l.staff,
                            patient = l.patient,
                            testType = l.testType,
                            orderDate = l.orderDate,
                            Staff1 = l.Staff1
                        })
                        .ToList();

                    return PartialView("_LabResultsPartial", labResults);
                }
                return Content("No lab results found.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetLabResults: {ex.Message}");
                return Content("Error loading lab results.");
            }
        }

        [HttpGet]
        public ActionResult GetBills()
        {
            try
            {
                var username = User.Identity.Name;
                var registration = db.SignUps.AsNoTracking()
                    .Where(s => s.email.Equals(username))
                    .Join(db.Registrations,
                        s => s.useId,
                        r => r.email,
                        (s, r) => r)
                    .FirstOrDefault();

                if (registration != null)
                {
                    var bills = db.bills
                        .Where(b => b.patient == registration.PatientID)
                        .OrderByDescending(b => b.Date)
                        .Take(5)
                        .ToList();

                    return PartialView("_BillsPartial", bills);
                }
                return Content("No bills found.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetBills: {ex.Message}");
                return Content("Error loading bills.");
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