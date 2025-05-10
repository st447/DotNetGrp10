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
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using iTextSharp.text.pdf.draw;

namespace Health_Care_MIS.Controllers
{
    public class laboratoryResultsController : Controller
    {
        private Health_Care_MISEntities1 db = new Health_Care_MISEntities1();

        // GET: laboratoryResults
        [CustomAuthorize(Roles = "doctor,lab,admin")]
        public ActionResult Index()
        {
            try
            {
                // Get the current user's email and role
                string userEmail = User.Identity.Name;
                var signUpUser = db.SignUps.FirstOrDefault(s => s.email == userEmail);

                if (signUpUser == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Index", "Home");
                }

                var query = db.laboratoryResults
                    .Include(l => l.laboratory)
                    .Include(l => l.laboratory.Registration)
                    .Include(l => l.laboratory.Staff1)
                    .OrderByDescending(l => l.date);

                // For doctors, show all lab results
                if (signUpUser.Role.ToLower() == "doctor" || signUpUser.Role.ToLower() == "admin")
                {
                    var allLabResults = query.ToList();
                    return View(allLabResults);
                }
                // For lab staff, show all lab results they can process
                else if (signUpUser.Role.ToLower() == "lab")
                {
                    var staffId = db.Staffs.FirstOrDefault(s => s.ContactInfo.Equals(signUpUser.useId))?.StaffId;
                    if (staffId.HasValue)
                    {
                        var labResults = query.Where(l => l.laboratory.staff == staffId).ToList();
                        return View(labResults);
                    }
                }

                TempData["ErrorMessage"] = "You do not have permission to view laboratory results.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading laboratory results: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: laboratoryResults/Details/5
        [CustomAuthorize(Roles = "user, doctor")]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            laboratoryResult laboratoryResult = db.laboratoryResults.Find(id);
            if (laboratoryResult == null)
            {
                return HttpNotFound();
            }
            return View(laboratoryResult);
        }

        // GET: laboratoryResults/Create
        [CustomAuthorize(Roles = "admin,doctor,lab")]
        public ActionResult Create()
        {
            try
            {
                ViewBag.testId = new SelectList(db.laboratories, "testId", "testType");
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading laboratory tests: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: laboratoryResults/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "admin,doctor,lab")]
        public ActionResult Create([Bind(Include = "id,testId,Results,Notes,date")] laboratoryResult laboratoryResult)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    laboratoryResult.date = DateTime.Now;
                    db.laboratoryResults.Add(laboratoryResult);
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Laboratory result created successfully.";
                    return RedirectToAction("Index");
                }

                ViewBag.testId = new SelectList(db.laboratories, "testId", "testType", laboratoryResult.testId);
                return View(laboratoryResult);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error creating laboratory result: " + ex.Message;
                ViewBag.testId = new SelectList(db.laboratories, "testId", "testType", laboratoryResult.testId);
                return View(laboratoryResult);
            }
        }

        // GET: laboratoryResults/Edit/5
        [CustomAuthorize(Roles = "admin,doctor")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            try
            {
                var laboratoryResult = db.laboratoryResults
                    .Include(l => l.laboratory)
                    .Include(l => l.laboratory.Registration)
                    .FirstOrDefault(l => l.id == id);

                if (laboratoryResult == null)
                {
                    return HttpNotFound();
                }

                ViewBag.testId = new SelectList(db.laboratories, "testId", "testType", laboratoryResult.testId);
                return View(laboratoryResult);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading laboratory result: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: laboratoryResults/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "admin,doctor")]
        public ActionResult Edit([Bind(Include = "id,testId,Results,Notes,date")] laboratoryResult laboratoryResult)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    db.Entry(laboratoryResult).State = EntityState.Modified;
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Laboratory result updated successfully.";
                    return RedirectToAction("Index");
                }

                ViewBag.testId = new SelectList(db.laboratories, "testId", "testType", laboratoryResult.testId);
                return View(laboratoryResult);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating laboratory result: " + ex.Message;
                ViewBag.testId = new SelectList(db.laboratories, "testId", "testType", laboratoryResult.testId);
                return View(laboratoryResult);
            }
        }

        // GET: laboratoryResults/Delete/5
        [CustomAuthorize(Roles = "admin,doctor")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            try
            {
                var laboratoryResult = db.laboratoryResults
                    .Include(l => l.laboratory)
                    .Include(l => l.laboratory.Registration)
                    .FirstOrDefault(l => l.id == id);

                if (laboratoryResult == null)
                {
                    return HttpNotFound();
                }

                return View(laboratoryResult);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading laboratory result: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: laboratoryResults/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "admin,doctor")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                var laboratoryResult = db.laboratoryResults.Find(id);
                if (laboratoryResult == null)
                {
                    TempData["ErrorMessage"] = "Laboratory result not found.";
                    return RedirectToAction("Index");
                }

                db.laboratoryResults.Remove(laboratoryResult);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Laboratory result deleted successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting laboratory result: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // GET: laboratoryResults/Download/5
        public ActionResult Download(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            laboratoryResult result = db.laboratoryResults.Find(id);
            if (result == null)
            {
                return HttpNotFound();
            }

            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 36, 36, 54, 36);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                document.Open();

                // Set up fonts
                BaseFont baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                Font headerFont = new Font(baseFont, 16, Font.BOLD);
                Font subHeaderFont = new Font(baseFont, 12, Font.BOLD);
                Font normalFont = new Font(baseFont, 10, Font.NORMAL);
                Font smallFont = new Font(baseFont, 8, Font.NORMAL);
                Font boldFont = new Font(baseFont, 10, Font.BOLD);

                // Add hospital header
                Paragraph header = new Paragraph("HEALTH CARE MEDICAL CENTER", headerFont);
                header.Alignment = Element.ALIGN_CENTER;
                document.Add(header);

                Paragraph subHeader = new Paragraph("Laboratory Test Report", subHeaderFont);
                subHeader.Alignment = Element.ALIGN_CENTER;
                document.Add(subHeader);

                document.Add(new Paragraph("123 Medical Center Drive\nCity, State 12345\nPhone: (555) 123-4567\nFax: (555) 123-4568", smallFont) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph("\n"));

                // Add line separator
                LineSeparator line = new LineSeparator(0.5f, 100f, BaseColor.BLACK, Element.ALIGN_CENTER, -1);
                document.Add(line);
                document.Add(new Paragraph("\n"));

                // Patient Information Section
                PdfPTable patientTable = new PdfPTable(2);
                patientTable.WidthPercentage = 100;
                patientTable.SetWidths(new float[] { 1f, 1f });

                patientTable.AddCell(new PdfPCell(new Phrase("PATIENT INFORMATION", boldFont)) { Colspan = 2, BackgroundColor = new BaseColor(240, 240, 240), Padding = 5 });
                
                patientTable.AddCell(new PdfPCell(new Phrase("Patient Name:", boldFont)) { Border = Rectangle.NO_BORDER });
                patientTable.AddCell(new PdfPCell(new Phrase($"{result.laboratory?.Registration?.Firstname} {result.laboratory?.Registration?.Lastname}", normalFont)) { Border = Rectangle.NO_BORDER });
                
                patientTable.AddCell(new PdfPCell(new Phrase("Test Date:", boldFont)) { Border = Rectangle.NO_BORDER });
                patientTable.AddCell(new PdfPCell(new Phrase(result.date?.ToString("MMM dd, yyyy") ?? "N/A", normalFont)) { Border = Rectangle.NO_BORDER });
                
                patientTable.AddCell(new PdfPCell(new Phrase("Test Type:", boldFont)) { Border = Rectangle.NO_BORDER });
                patientTable.AddCell(new PdfPCell(new Phrase(result.laboratory?.testType ?? "N/A", normalFont)) { Border = Rectangle.NO_BORDER });
                
                patientTable.AddCell(new PdfPCell(new Phrase("Ordering Physician:", boldFont)) { Border = Rectangle.NO_BORDER });
                patientTable.AddCell(new PdfPCell(new Phrase($"Dr. {result.laboratory?.Staff1?.Firstname} {result.laboratory?.Staff1?.LastName}", normalFont)) { Border = Rectangle.NO_BORDER });
                
                document.Add(patientTable);
                document.Add(new Paragraph("\n"));

                // Test Results Section
                PdfPTable resultTable = new PdfPTable(1);
                resultTable.WidthPercentage = 100;

                resultTable.AddCell(new PdfPCell(new Phrase("TEST RESULTS", boldFont)) { BackgroundColor = new BaseColor(240, 240, 240), Padding = 5 });
                resultTable.AddCell(new PdfPCell(new Phrase(result.Results ?? "N/A", normalFont)) { PaddingLeft = 5, PaddingRight = 5, PaddingTop = 10, PaddingBottom = 10 });
                
                document.Add(resultTable);
                document.Add(new Paragraph("\n"));

                // Notes Section
                if (!string.IsNullOrEmpty(result.Notes))
                {
                    PdfPTable notesTable = new PdfPTable(1);
                    notesTable.WidthPercentage = 100;

                    notesTable.AddCell(new PdfPCell(new Phrase("ADDITIONAL NOTES", boldFont)) { BackgroundColor = new BaseColor(240, 240, 240), Padding = 5 });
                    notesTable.AddCell(new PdfPCell(new Phrase(result.Notes, normalFont)) { PaddingLeft = 5, PaddingRight = 5, PaddingTop = 10, PaddingBottom = 10 });
                    
                    document.Add(notesTable);
                }

                // Add footer with disclaimer
                document.Add(new Paragraph("\n"));
                Paragraph disclaimer = new Paragraph(
                    "DISCLAIMER: This laboratory report contains confidential medical information and is intended only for authorized medical professionals. " +
                    "Results should be interpreted in conjunction with clinical findings and patient history. For any questions about these results, " +
                    "please contact our laboratory department.", smallFont);
                disclaimer.Alignment = Element.ALIGN_LEFT;
                document.Add(disclaimer);

                // Add signature line
                document.Add(new Paragraph("\n\n"));
                PdfPTable signatureTable = new PdfPTable(2);
                signatureTable.WidthPercentage = 100;
                signatureTable.SetWidths(new float[] { 1f, 1f });

                signatureTable.AddCell(new PdfPCell(new Phrase("_______________________\nLaboratory Director", smallFont)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_CENTER });
                signatureTable.AddCell(new PdfPCell(new Phrase("_______________________\nReviewing Physician", smallFont)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_CENTER });
                
                document.Add(signatureTable);

                document.Close();
                writer.Close();

                var fileName = $"LabReport_{result.laboratory?.Registration?.Firstname}_{result.laboratory?.Registration?.Lastname}_{DateTime.Now:yyyyMMdd}.pdf";
                return File(ms.ToArray(), "application/pdf", fileName);
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
