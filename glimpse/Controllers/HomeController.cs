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
using System.Linq;
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
                    accountLabels.AddRange(Label.FindByAccount(mailAccount.Entity, session));

                    try
                    {
                        if (!mailAccount.IsFullyConnected())
                            mailAccount.ConnectFull(session);
                        Task.Factory.StartNew(() => MailsTasksHandler.StartSynchronization(mailAccount.Entity.Address));     
                    }
                    catch (InvalidAuthenticationException exc)
                    {
                        Log.LogException(exc, "No se puede conectar con IMAP, cambio el password de :" + mailAccount.Entity.Address + ".");
                        ViewBag.MailErrors += "Glimpse no puede conectarse con la cuenta: " + mailAccount.Entity.Address + ". Por favor reconfigure la contraseña.";
                        //TODO: ver como mostrar los mailAccounts que no se pudieron conectar en la Vista
                    }
                    catch (SocketException exc)
                    {
                        Log.LogException(exc, "Error al conectar con IMAP.");
                    }
                }
                accountLabels = Label.RemoveDuplicates(accountLabels);
                DateTime oldestMailDate = mailAccounts.OrderBy(x => x.Entity.OldestMailDate)
                                                      .Take(1)
                                                      .Select(x => x.Entity.OldestMailDate)
                                                      .Single();

                foreach (LabelEntity label in accountLabels)
                    viewLabels.Add(new LabelViewModel(label.Name, label.SystemName, label.Color));

                ViewBag.Username = sessionUser.Entity.Username;
                ViewBag.Labels = viewLabels;
                ViewBag.OldestAge = DateTimeHelper.GetAgeInSeconds(oldestMailDate);
                ViewBag.Firstname = sessionUser.Entity.Firstname ?? "";
                ViewBag.Lastname = sessionUser.Entity.Lastname ?? "";
                ViewBag.Country = sessionUser.Entity.Country ?? "";
                ViewBag.City = sessionUser.Entity.City ?? "";
                ViewBag.Telephone = sessionUser.Entity.Telephone ?? "";
                ViewBag.MailAccounts = sessionUser.GetAccounts().Select(x => new { address = x.Entity.Address, 
                                                                                   mainAccount = x.Entity.IsMainAccount, 
                                                                                   mailAccountId = x.Entity.Id });
                ViewBag.IsGlimpseUser = Glimpse.Models.User.IsGlimpseUser(sessionUser.Entity.Username);

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
