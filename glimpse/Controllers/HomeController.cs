using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Glimpse.Helpers;
using Glimpse.ViewModels;
using Glimpse.MailInterfaces;
using System.Web.Security;
using Glimpse.Exceptions.ControllersExceptions;
using Glimpse.Models;
using Glimpse.Exceptions.MailInterfacesExceptions;
using Glimpse.Exceptions;

namespace Glimpse.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        //
        // GET: /Home/
        public ActionResult Index()
        {
            FakeMailAddressPersistible mailAddress;

            try
            {
                mailAddress = FakeMailAddressPersistible.FindByAddress(new CookieHelper().getMailAddressFromCookie());
            }
            catch (GlimpseException)
            {
                return this.LogOut();
            }

            if (Session[AccountController.MAIL_ACCOUNT] == null)
            {
                try
                {
                    MailAccount account = mailAddress.LoginExternal();
                    Session[AccountController.MAIL_ACCOUNT] = account;
                }
                catch (InvalidAuthenticationException)
                {
                    return this.LogOut();
                }
            }

            ViewBag.InboxMessages = ((MailAccount)Session[AccountController.MAIL_ACCOUNT]).GetInboxMessages();
            ViewBag.Email = mailAddress.MailAddress;

            return View();
        }

        [NonAction]
        private ActionResult LogOut()
        {
            return RedirectToAction("Logout", "Account");
        }

    }
}
