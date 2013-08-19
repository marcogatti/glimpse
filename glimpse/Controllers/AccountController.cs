using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Glimpse.ViewModels;
using Glimpse.Helpers;
using System.Web.Security;
using System.Xml;
using Glimpse.Exceptions.MailInterfacesExceptions;
using Glimpse.MailInterfaces;
using Glimpse.DataAccessLayer.Entities;

namespace Glimpse.Controllers
{
    public class AccountController : Controller
    {
        public const String MAIL_INTERFACE = "mail-interface";


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

            try
            {
                UpdateModel(user);

                MailAccount mailAccount = new MailAccount(user.Email, user.Password);
                Session[MAIL_INTERFACE] = mailAccount.LoginExternal();
                mailAccount.CreateOrUpdate();

                new CookieHelper().addMailAddressCookie(mailAccount.Address);
                FormsAuthentication.SetAuthCookie(user.Email, user.rememberMe);

                return RedirectToLocal(returnUrl);
            } 
            catch (InvalidAuthenticationException)
            {
                ModelState.AddModelError("", "The email address or password provided is incorrect.");
                return View(user);
            }
            catch (InvalidOperationException)
            {
                return View(user);
            }

        }

        // GET: /Logout
        [Authorize]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            new CookieHelper().clearMailAddressCookie();
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
