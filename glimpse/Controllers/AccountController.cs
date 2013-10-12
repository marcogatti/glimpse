using Glimpse.DataAccessLayer;
using Glimpse.Exceptions;
using Glimpse.Exceptions.MailInterfacesExceptions;
using Glimpse.Helpers;
using Glimpse.Models;
using Glimpse.ViewModels;
using NHibernate;
using System;
using System.Collections.Generic;
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
                    existingMailAccount = MailAccount.FindByAddress(userView.Username, session, false);

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

                    if (existingMailAccount == null)
                    {
                        mailAccount.SetUser(user);
                        mailAccount.SetOldestMailDate();
                        mailAccount.Deactivate(session); //llama a saveOrUpdate adentro
                    }
                    else
                    {
                        if (existingMailAccount.Entity.Password != mailAccount.Entity.Password)
                        {
                            existingMailAccount.Entity.Password = mailAccount.Entity.Password;
                            existingMailAccount.SaveOrUpdate(session);
                        }
                        mailAccount.Entity = existingMailAccount.Entity;
                    }
                    mailAccount.UpdateLabels(session);
                    user.AddAccount(mailAccount);
                }
                else //si es un usuario glimpse
                {
                    user = Glimpse.Models.User.FindByUsername(userView.Username, session);
                    if (user == null)
                    {
                        this.ModelState.AddModelError("User", "Usuario inexistente.");
                        return View(userView);
                    }
                    user.UpdateAccounts(session);
                    user.ConnectLight();
                    user.UpdateLabels(session);
                }

                new CookieHelper().AddUsernameCookie(user.Entity.Username);
                FormsAuthentication.SetAuthCookie(userView.Username, true);
                tran.Commit();
                Session[AccountController.USER_NAME] = user;

                return RedirectToLocal(returnUrl);
            }
            catch (InvalidOperationException) //model state invalido
            {
                tran.Rollback();
                return View(userView);
            }
            catch (InvalidAuthenticationException)
            {
                tran.Rollback();
                ModelState.AddModelError("", "La dirección de correo o la contraseña no son correctos.");
                return View(userView);
            }
            finally
            {
                session.Close();
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult CreateUser(UserViewModel userView)
        {
            String exceptionMessage = "";
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                this.UpdateModel(userView); //corre todos los regex

                if(!Glimpse.Models.User.IsGlimpseUser(userView.Username))
                    exceptionMessage += "El nombre de usuario elegido no posee caracteres válidos.\n";
                String cipherPassword = CryptoHelper.EncryptDefaultKey(userView.Password);
                if (userView.Password != userView.ConfirmationPassword)
                    exceptionMessage += "Las contraseñas ingresadas deben coincidir.\n";

                User existingUser = Glimpse.Models.User.FindByUsername(userView.Username, session);
                if (existingUser != null)
                    exceptionMessage += "El nombre de usuario elegido ya existe.\n";

                if (exceptionMessage != "")
                    throw new GlimpseException(exceptionMessage);

                User newUser = new User(userView.Username, cipherPassword, userView.Firstname, userView.Lastname);
                newUser.SaveOrUpdate(session);
                tran.Commit();
                return Json(new { success = true }, JsonRequestBehavior.AllowGet);

            }
            catch (InvalidOperationException exc) //model state invalido
            {
                tran.Rollback();
                IEnumerable<ModelState> wrongStates = this.ModelState.Values.Where(x => x.Errors.Count > 0);
                foreach (ModelState wrongState in wrongStates)
                {
                    foreach (ModelError error in wrongState.Errors)
                    {
                        exceptionMessage += error.ErrorMessage;
                    }
                }
                if (String.IsNullOrEmpty(exceptionMessage))
                    exceptionMessage = exc.Message;
                return Json(new { success = false, message = exceptionMessage }, JsonRequestBehavior.AllowGet);
            }
            catch (GlimpseException exc)
            {
                tran.Rollback();
                Log.LogException(exc);
                return Json(new { success = false, message = exceptionMessage }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exc)
            {
                tran.Rollback();
                Log.LogException(exc, "Parametros: userName:(" + userView.Username + "), userPassowrd( " + userView.Password +
                                      "), userConfirmPassword(" + userView.ConfirmationPassword + "), userFirstName(" + userView.Firstname +
                                      "), userLastName(" + userView.Lastname + ").");
                return Json(new { success = false, message = "Error creando usuario." }, JsonRequestBehavior.AllowGet);
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
