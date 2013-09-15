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
using NHibernate;
using Glimpse.DataAccessLayer;

namespace Glimpse.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        //
        // GET: /Home/
        public ActionResult Index()
        {
            ISession session = NHibernateManager.OpenSession();

            String mailAddress = new CookieHelper().getMailAddressFromCookie();
            
            MailAccount cookieMailAccount = MailAccount.FindByAddress(mailAddress, session);

            if (cookieMailAccount == null)
            {
                session.Flush();
                session.Close();
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
                    session.Flush();
                    session.Close();
                    return this.LogOut();
                }
            }

            IList<LabelEntity> accountLabels = Label.FindByAccount(cookieMailAccount.Entity, session);
            List<LabelViewModel> viewLabels = new List<LabelViewModel>(accountLabels.Count);

            foreach (LabelEntity label in accountLabels)
                viewLabels.Add(new LabelViewModel(label.Name, label.SystemName));

            MailsTasksHandler.StartSynchronization(mailAccount);

            ViewBag.Email = cookieMailAccount.Entity.Address;
            ViewBag.Labels = viewLabels;

            session.Flush();
            session.Close();

            return View();
        }

        [NonAction]
        private ActionResult LogOut()
        {
            return RedirectToAction("Logout", "Account");
        }
    }
}
