﻿using Glimpse.DataAccessLayer;
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
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Glimpse.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ISession session = NHibernateManager.OpenSession();

            try
            {
                String cookieUsername = new CookieHelper().GetUserFromCookie();
                User cookieUser = Glimpse.Models.User.FindByUsername(cookieUsername, session);
                if (cookieUser == null)
                    return this.LogOut();
                User sessionUser = (User)Session[AccountController.USER_NAME];
                if (sessionUser == null)
                {
                    sessionUser = cookieUser; //cookieUser no tiene mailAccounts
                    sessionUser.UpdateAccounts(session);
                    Session[AccountController.USER_NAME] = sessionUser;
                }
                else if (sessionUser.Entity.Id != cookieUser.Entity.Id || sessionUser.Entity.Password != cookieUser.Entity.Password)
                    return this.LogOut();
                ViewBag.MailErrors = "";
                IList<MailAccount> mailAccounts = sessionUser.GetAccounts();
                List<LabelEntity> accountLabels = new List<LabelEntity>();
                List<LabelViewModel> viewLabels = new List<LabelViewModel>();
                foreach (MailAccount mailAccount in mailAccounts)
                {
                    try
                    {
                        if (!mailAccount.IsFullyConnected())
                            mailAccount.ConnectFull();
                        Task.Factory.StartNew(() => MailsTasksHandler.StartSynchronization(mailAccount.Entity.Address));
                        accountLabels.AddRange(Label.FindByAccount(mailAccount.Entity, session));
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
                }
                accountLabels = Label.RemoveDuplicates(accountLabels);
                DateTime oldestMailDate = mailAccounts[0].GetLowestMailDate(); //TODO: buscar en la base de datos

                foreach (LabelEntity label in accountLabels)
                    viewLabels.Add(new LabelViewModel(label.Name, label.SystemName));

                ViewBag.Username = sessionUser.Entity.Username;
                ViewBag.Labels = viewLabels;
                ViewBag.OldestAge = DateTime.Now.Ticks - oldestMailDate.Ticks;

                return View();
            }
            catch (Exception exc)
            {
                Log.LogException(exc);
                return this.LogOut();
            }
            finally
            {
                session.Close();
            }
        }

        [NonAction]
        private ActionResult LogOut()
        {
            return RedirectToAction("Logout", "Account");
        }
    }
}
