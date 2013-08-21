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
            MailAccount mailAccount = MailAccount.FindByAddress(new CookieHelper().getMailAddressFromCookie());

            if (mailAccount == null)
            {
                return this.LogOut();
            }

            AccountInterface accountInterface = (AccountInterface)Session[AccountController.MAIL_INTERFACE];

            if (accountInterface == null)
            {
                try
                {
                    accountInterface = mailAccount.LoginExternal();
                    Session[AccountController.MAIL_INTERFACE] = accountInterface;
                }
                catch (InvalidAuthenticationException)
                {
                    return this.LogOut();
                }
            }

            // RESOLVER ESTOOOOOOOOOO, PERO YA MIERDA, YA FALTA POCO

            MailManager manager = new MailManager(accountInterface, mailAccount);
            manager.FetchFromMailbox("INBOX");

            //ViewBag.InboxMessages = manager.FetchFromMailbox("INBOX");
            ViewBag.Email = mailAccount.Entity.Address;

            return View();
        }

        [NonAction]
        private ActionResult LogOut()
        {
            return RedirectToAction("Logout", "Account");
        }

    }
}
