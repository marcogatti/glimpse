using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using glimpse.ViewModels;
using glimpse.Helpers;
using System.Web.Security;

namespace glimpse.Controllers
{
    public class AccountController : Controller
    {
        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            return View();
        }

        //
        // POST: /Account/Login
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

        //
        // GET: /Account/
        [Authorize]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            new CookieHelper().clearLoginCookie("Login");

            return Redirect("/");
        }


        #region Helpers

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

        #endregion Helpers

    }

}
