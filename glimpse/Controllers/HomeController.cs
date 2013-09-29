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
using System.Net.Sockets;
using NHibernate.Criterion;

namespace Glimpse.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public const String MAIL_ACCOUNTS = "mail-accounts";
        //
        // GET: /Home/
        public ActionResult Index()
        {
            ISession session = NHibernateManager.OpenSession();

            String username = new CookieHelper().GetUserFromCookie();
            
            User cookieUser = Glimpse.Models.User.FindByUsername(username, session);

            if (cookieUser == null)
            {
                session.Close();
                return this.LogOut();
            }

            User sessionUser = (User)Session[AccountController.USER_NAME];

            if (sessionUser == null)
            {
                sessionUser = cookieUser;
                Session[AccountController.USER_NAME] = sessionUser;
            }

            IList<MailAccount> mailAccounts = sessionUser.GetAccounts(session);
            ViewBag.MailErrors = "";
            foreach(MailAccount mailAccount in mailAccounts)
                try
                {
                    mailAccount.connectFull();
                }
                catch (InvalidAuthenticationException)
                {
                    ViewBag.MailErrors += "Could not log in with " + mailAccount.Entity.Address + ", password is outdated.";
                    session.Close();
                    return this.LogOut();
                }
                catch (SocketException exc)
                {
                    Log.LogException(exc, "Error al conectar con IMAP");
                }

            MailAccount mainMailAccount = mailAccounts[0];
            Session[HomeController.MAIL_ACCOUNTS] = mainMailAccount; //solo se guarda el primero por ahora

            MailsTasksHandler.StartSynchronization(mainMailAccount.Entity.Address);

            IList<LabelEntity> accountLabels = Label.FindByAccount(mainMailAccount.Entity, session);
            List<LabelViewModel> viewLabels = new List<LabelViewModel>(accountLabels.Count);

            foreach (LabelEntity label in accountLabels)
                viewLabels.Add(new LabelViewModel(label.Name, label.SystemName));

            ViewBag.Email = mainMailAccount.Entity.Address;
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
