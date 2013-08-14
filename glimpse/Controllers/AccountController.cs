using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Glimpse.ViewModels;
using Glimpse.Helpers;
using System.Web.Security;
using System.Xml;

namespace Glimpse.Controllers
{
    public class AccountController : Controller
    {
        // GET: /Login
        [AllowAnonymous]
        public ActionResult Login()
        {
            if (this.Request.UserLanguages.Count() > 0)
            {
                Session["Language"] = this.Request.UserLanguages[0].Substring(0, 2);
            }
            else
            {
                Session["Language"] = "en";
            }

            return View();
        }

        // POST: /Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(UserViewModel user, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                FormsAuthentication.SetAuthCookie(user.Email, user.rememberMe);

                new CookieHelper().addMailAddressCookie(user);

                return RedirectToLocal(returnUrl);
            }
            ModelState.AddModelError("", "The user name or password provided is incorrect.");
            return View(user);
        }

        // GET: /Logout
        [Authorize]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            new CookieHelper().clearLoginCookie("Login");

            return Redirect("/");
        }


        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }

}
