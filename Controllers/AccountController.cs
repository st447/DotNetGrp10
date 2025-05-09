using System;
using System.Web;
using System.Web.Mvc;
using System.Linq;
using System.Web.Security;
using Health_Care_MIS.Models;
using Health_Care_MIS.Helpers;

namespace Health_Care_MIS.Controllers
{
    public class AccountController : Controller
    {
        private readonly Health_Care_MISEntities1 db = new Health_Care_MISEntities1();

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = db.SignUps.FirstOrDefault(u => u.email.ToLower() == model.Email.ToLower());
                if (user != null && PasswordHelper.VerifyPassword(model.Password, user.password))
                {
                    // Create the authentication ticket with user role
                    var role = user.Role?.Trim().ToLower() ?? "user";
                    var authTicket = new FormsAuthenticationTicket(
                        1,                              // version
                        user.email,                     // user name
                        DateTime.Now,                   // creation
                        DateTime.Now.AddMinutes(30),    // expiration
                        model.RememberMe,               // persistent?
                        role                            // user data (role)
                    );

                    // Encrypt the ticket and create the cookie
                    string encryptedTicket = FormsAuthentication.Encrypt(authTicket);
                    var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                    if (model.RememberMe)
                        authCookie.Expires = authTicket.Expiration;
                    Response.Cookies.Add(authCookie);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    // Redirect based on role
                    switch (role)
                    {
                        case "admin":
                            return RedirectToAction("Dashboard", "Admin");
                        case "doctor":
                            return RedirectToAction("Dashboard", "Doctor");
                        case "finance":
                            return RedirectToAction("Index", "Bills");
                        default:
                            return RedirectToAction("Index", "Home");
                    }
                }

                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
                return View(model);
            }
        }

        [AllowAnonymous]
        public ActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if email already exists
                    if (db.SignUps.Any(u => u.email.ToLower() == model.Email.ToLower()))
                    {
                        ModelState.AddModelError("", "Email already exists");
                        return View(model);
                    }

                    // Create new user
                    var user = new SignUp
                    {
                        email = model.Email,
                        password = PasswordHelper.HashPassword(model.Password),
                        Role = "user" // Default role
                    };

                    db.SignUps.Add(user);
                    db.SaveChanges();

                    // Log in the user after registration
                    var authTicket = new FormsAuthenticationTicket(
                        1,
                        user.email,
                        DateTime.Now,
                        DateTime.Now.AddMinutes(30),
                        false,
                        "user"
                    );

                    string encryptedTicket = FormsAuthentication.Encrypt(authTicket);
                    var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                    Response.Cookies.Add(authCookie);

                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                }
            }

            return View(model);
        }

        // GET: Account/Profile
        public ActionResult Profile()
        {
            var username = User.Identity.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login");
            }

            var user = db.SignUps.FirstOrDefault(u => u.email == username);
            if (user == null)
            {
                return HttpNotFound();
            }

            var profile = new ProfileViewModel
            {
                Username = user.email,
                Email = user.email,
                Role = user.Role,
                LastLoginDate = null // LastLoginDate is not available in SignUp model
            };

            return View(profile);
        }

        // POST: Account/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var username = User.Identity.Name;
            var user = db.SignUps.FirstOrDefault(u => u.email == username);
            
            if (user == null)
            {
                return HttpNotFound();
            }

            // Update user information
            user.email = model.Email;

            // If new password is provided, update it
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                if (string.IsNullOrEmpty(model.CurrentPassword))
                {
                    ModelState.AddModelError("CurrentPassword", "Current password is required to change password");
                    return View(model);
                }

                var hashedCurrentPassword = PasswordHelper.HashPassword(model.CurrentPassword);
                if (user.password != hashedCurrentPassword)
                {
                    ModelState.AddModelError("CurrentPassword", "Current password is incorrect");
                    return View(model);
                }

                user.password = PasswordHelper.HashPassword(model.NewPassword);
            }

            try
            {
                db.SaveChanges();
                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction("Profile");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred while saving changes.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}