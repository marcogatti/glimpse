using Glimpse.DataAccessLayer;
using Glimpse.DataAccessLayer.Entities;
using Glimpse.Exceptions.MailInterfacesExceptions;
using Glimpse.Helpers;
using Glimpse.MailInterfaces;
using Glimpse.Models;
using Glimpse.ViewModels;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Web.Mvc;

namespace Glimpse.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public const String MAIL_ACCOUNTS = "mail-accounts";

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

            User sessionUser = (User)Session[AccountController.USER_NAME]; //siempre null la primera vez

            if (sessionUser == null)
            {
                sessionUser = cookieUser;
                Session[AccountController.USER_NAME] = sessionUser;
            }
            else if (sessionUser.Entity.Id != cookieUser.Entity.Id || sessionUser.Entity.Password != cookieUser.Entity.Password)
            {
                //si el cookie tiene un usuario diferente al de la sesion
                session.Close();
                return this.LogOut();
            }

            IList<MailAccount> mailAccounts = sessionUser.GetAccounts(session);
            ViewBag.MailErrors = "";
            foreach(MailAccount mailAccount in mailAccounts)
                try
                {
                    mailAccount.ConnectFull();
                }
                catch (InvalidAuthenticationException exc)
                {
                    Log.LogException(exc, "No se puede conectar con IMAP, cambio el password de :" + mailAccount.Entity.Address + ".");
                    ViewBag.MailErrors += "Could not log in with " + mailAccount.Entity.Address + ", password was changed.";
                    //TODO: ver como mostrar los mailAccounts que no se pudieron conectar en la Vista
                }
                catch (SocketException exc)
                {
                    Log.LogException(exc, "Error al conectar con IMAP.");
                }

            MailAccount mainMailAccount = mailAccounts[0];
            Session[HomeController.MAIL_ACCOUNTS] = mainMailAccount; //solo se guarda el primero por ahora

            MailsTasksHandler.StartSynchronization(mainMailAccount.Entity.Address);

            IList<LabelEntity> accountLabels = Label.FindByAccount(mainMailAccount.Entity, session);
            List<LabelViewModel> viewLabels = new List<LabelViewModel>(accountLabels.Count);
            DateTime oldestMailDate = mainMailAccount.GetLowestMailDate();

            foreach (LabelEntity label in accountLabels)
                viewLabels.Add(new LabelViewModel(label.Name, label.SystemName));

            ViewBag.Username = sessionUser.Entity.Username;
            ViewBag.Labels = viewLabels;
            ViewBag.oldestDate = DateTime.Now.Ticks - oldestMailDate.Ticks;

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
