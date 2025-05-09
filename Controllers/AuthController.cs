using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Health_Care_MIS.Models;

namespace Health_Care_MIS.Controllers
{
    public class AuthController : Controller
    {
        private Health_Care_MISEntities db = new Health_Care_MISEntities();

        [AllowAnonymous]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(SignUp model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                var user = db.SignUps.FirstOrDefault(u => u.email == model.email && u.password == model.password);
                if (user != null)
                {
                    FormsAuthentication.SetAuthCookie(user.email, false);
                    var authTicket = new FormsAuthenticationTicket(1, user.email, DateTime.Now,
                        DateTime.Now.AddMinutes(20), false, user.Role);
                    string encryptedTicket = FormsAuthentication.Encrypt(authTicket);
                    var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                    HttpContext.Response.Cookies.Add(authCookie);

                    if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                        && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                    {
                        return Redirect(returnUrl);
                    }

                    switch (user.Role.ToLower())
                    {
                        case "admin":
                            return RedirectToAction("Index", "Staffs");
                        case "doctor":
                            return RedirectToAction("Index", "Prescriptions");
                        case "finance":
                            return RedirectToAction("Index", "Bills");
                        default:
                            return RedirectToAction("Index", "Home");
                    }
                }
                ModelState.AddModelError("", "Invalid email or password");
            }
            return View(model);
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }
    }
}
