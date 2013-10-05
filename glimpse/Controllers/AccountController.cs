using Glimpse.DataAccessLayer;
using Glimpse.Exceptions.MailInterfacesExceptions;
using Glimpse.Helpers;
using Glimpse.Models;
using Glimpse.ViewModels;
using NHibernate;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;

namespace Glimpse.Controllers
{
    public class AccountController : Controller
    {
        public const String USER_NAME = "Username";

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

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(UserViewModel userView, string returnUrl)
        {
            User user;
            MailAccount mailAccount;
            MailAccount existingMailAccount;
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                this.UpdateModel(userView);
                String cipherPassword = CryptoHelper.EncryptDefaultKey(userView);

                if (Glimpse.Models.User.IsEmail(userView.Username)) //si es un email
                {
                    mailAccount = new MailAccount(userView.Username, cipherPassword);
                    mailAccount.ConnectLight(); //si pasa este punto es que los datos ingresados son correctos
                    user = Glimpse.Models.User.FindByUsername(userView.Username, session);
                    existingMailAccount = MailAccount.FindByAddress(userView.Username, session);

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
                    if (existingMailAccount != null && existingMailAccount.Entity.User.Username != user.Entity.Username)
                    {
                        this.ModelState.AddModelError("User", "Email account already associated with another account.");
                        tran.Rollback();
                        return View(userView);
                    }
                    mailAccount.SetUser(user);
                    mailAccount.SetAsMainAccount(session);
                    mailAccount.SetOldestMailDate();
                    mailAccount.SaveOrUpdate(session);
                    mailAccount.UpdateLabels(session);
                    user.AddAccount(mailAccount);
                }
                else //si es un usuario glimpse
                {
                    user = Glimpse.Models.User.FindByUsername(userView.Username, session);
                    if (user == null)
                    {
                        this.ModelState.AddModelError("User", "Username doesn't exist");
                        return View(userView);
                    }
                    user.UpdateAccounts(session);
                    user.ConnectLight();
                    user.UpdateLabels(session);
                }

                new CookieHelper().AddUsernameCookie(user.Entity.Username);
                FormsAuthentication.SetAuthCookie(userView.Username, userView.rememberMe);
                tran.Commit();
                Session[AccountController.USER_NAME] = user;

                return RedirectToLocal(returnUrl);
            }
            catch (InvalidOperationException)
            {
                //model state invalido
                tran.Rollback();
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

        [Authorize]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            new CookieHelper().ClearUserCookie();
            User user = (User)Session[AccountController.USER_NAME];
            if (user != null)
            {
                user.Disconnect();
                Session.Remove(AccountController.USER_NAME);
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
