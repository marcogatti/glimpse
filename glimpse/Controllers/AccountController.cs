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
using Glimpse.Models;
using NHibernate;
using Glimpse.DataAccessLayer;

namespace Glimpse.Controllers
{
    public class AccountController : Controller
    {
        public const String USER_NAME = "user-name";

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
        public ActionResult Login(UserViewModel userView, string returnUrl)
        {
            User user;
            MailAccount mailAccount;
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                this.UpdateModel(userView);
                String cipherPassword = CryptoHelper.EncryptDefaultKey(userView);

                if (Glimpse.Models.User.IsEmail(userView.Username)) //si es un email
                {
                    mailAccount = new MailAccount(userView.Username, cipherPassword);
                    mailAccount.connectLight(); //si pasa este punto es que los datos ingresados son correctos
                    user = Glimpse.Models.User.FindByUsername(userView.Username, session);

                    if (user == null)
                    {
                        user = new User(userView.Username, cipherPassword);
                        user.SaveOrUpdate(session);
                    }
                    else if (user.Entity.Password != cipherPassword)
                    {
                        user.Entity.Password = cipherPassword;
                        user.SaveOrUpdate(session);
                    }

                    mailAccount.SetUser(user);
                    mailAccount.SetAsMainAccount(session);
                    mailAccount.SaveOrUpdate(session);
                    mailAccount.UpdateLabels(session);
                    mailAccount.Disconnect();
                }
                else //si es un usuario glimpse
                {
                    user = Glimpse.Models.User.FindByUsername(userView.Username, session);
                    if (user == null)
                    {
                        this.ModelState.AddModelError("User", "Username doesn't exist");
                        return View(userView);
                    }
                }

                new CookieHelper().AddUsernameCookie(user.Entity.Username);
                FormsAuthentication.SetAuthCookie(userView.Username, userView.rememberMe);

                tran.Commit();

                return RedirectToLocal(returnUrl);
            }
            catch (InvalidOperationException)
            {
                //model state invalido
                return View(userView);
            }
            catch (InvalidAuthenticationException)
            {
                tran.Rollback();
                ModelState.AddModelError("", "The email address or password provided is incorrect.");
                return View(userView);
            }
            finally
            {
                session.Close();
            }
        }

        // GET: /Logout
        [Authorize]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            new CookieHelper().clearMailAddressCookie();
            MailAccount mailAccount = (MailAccount)Session[HomeController.MAIL_ACCOUNTS];
            if (mailAccount != null)
            {
                mailAccount.Disconnect();
                Session.Remove(USER_NAME);
            }
            return Redirect("/");
        }

        [NonAction]
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
