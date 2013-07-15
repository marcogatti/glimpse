using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using glimpse.Models;
using System.Web.Security;

namespace glimpse.Controllers
{
    public class AccountController : Controller
    {
        //
        // GET: /Account/
        [AllowAnonymous]
        public ActionResult Index(string returnUrl)
        {        
            return View();
        }

        //
        // POST: /Account/
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Index(User user, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                return RedirectToLocal("/Home/Index");
            }
            ModelState.AddModelError("", "The user name or password provided is incorrect.");
            return View(user);
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
