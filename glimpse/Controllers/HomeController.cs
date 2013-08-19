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

namespace Glimpse.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        //
        // GET: /Home/
        public ActionResult Index()
        {
            MailAccount mailAccount = MailAccount.FindByAddress(new CookieHelper().getMailAddressFromCookie());

            if (mailAccount == null)
            {
                return this.LogOut();
            }

            AccountInterface account = (AccountInterface)Session[AccountController.MAIL_INTERFACE];

            if (account == null)
            {
                try
                {
                    account = mailAccount.LoginExternal();
                    Session[AccountController.MAIL_INTERFACE] = account;
                }
                catch (InvalidAuthenticationException)
                {
                    return this.LogOut();
                }
            }

            ViewBag.InboxMessages = account.GetInboxMessages();
            ViewBag.Email = mailAccount.Address;

            return View();
        }

        [NonAction]
        private ActionResult LogOut()
        {
            return RedirectToAction("Logout", "Account");
        }

    }
}
