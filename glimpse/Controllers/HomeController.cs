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
using Glimpse.Exceptions.MailInterfacesExceptions;
using Glimpse.Exceptions;
using Glimpse.DataAccessLayer.Entities;
using Glimpse.Models;

namespace Glimpse.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        //
        // GET: /Home/
        public ActionResult Index()
        {
            String mailAddress = new CookieHelper().getMailAddressFromCookie();

            MailAccount cookieMailAccount = MailAccount.FindByAddress(mailAddress);

            if (cookieMailAccount.Entity == null)
            {
                return this.LogOut();
            }

            MailAccount mailAccount = (MailAccount)Session[AccountController.MAIL_INTERFACE];

            if (mailAccount == null)
            {
                try
                {
                    mailAccount = cookieMailAccount.LoginExternal();
                    Session[AccountController.MAIL_INTERFACE] = mailAccount;
                }
                catch (InvalidAuthenticationException)
                {
                    return this.LogOut();
                }
            }

            MailManager manager = new MailManager(mailAccount);

            ViewBag.InboxMessages = manager.FetchFromMailbox("INBOX");
            ViewBag.Email = cookieMailAccount.Entity.Address;

            return View();
        }

        [NonAction]
        private ActionResult LogOut()
        {
            return RedirectToAction("Logout", "Account");
        }

    }
}
