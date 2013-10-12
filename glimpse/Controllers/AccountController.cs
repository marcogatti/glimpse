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
using System.Net.Sockets;
using System.Web.Mvc;
using System.Web.Security;

namespace Glimpse.Controllers
{
    public class AccountController : Controller
    {
        public const String USER_NAME = "Username";

        #region Action Methods
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
            Boolean workingOffline = false;

            try
            {
                this.UpdateModel(userView);
                String cipherPassword = CryptoHelper.EncryptDefaultKey(userView);

                if (Glimpse.Models.User.IsEmail(userView.Username)) //si es un email
                {
                    mailAccount = new MailAccount(userView.Username, cipherPassword);
                    try
                    {
                        mailAccount.ConnectLight(); //si pasa este punto es que los datos ingresados son correctos
                    }
                    catch (SocketException)
                    {
                        workingOffline = true;
                        mailAccount.ValidateCredentials();
                    }
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

                    if(!workingOffline)
                        mailAccount.UpdateLabels(session);

                    user.AddAccount(mailAccount);
                }
                else //si es un usuario glimpse
                {
                    user = Glimpse.Models.User.FindByUsername(userView.Username, session);
                    if (user == null)
                    {
                        this.ModelState.AddModelError("User", "Usuario inexistente.");
                        tran.Rollback();
                        return View(userView);
                    }
                    else if (CryptoHelper.DecryptDefaultKey(user.Entity.Password) != userView.Password)
                    {
                        this.ModelState.AddModelError("User", "Contraseña incorrecta.");
                        tran.Rollback();
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
            catch (Exception)
            {
                tran.Rollback();
                ModelState.AddModelError("", "Existen problemas para iniciar sesión, intentalo de nuevo más tarde.");
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
            IList<MailAccount> mailAccounts;
            String exceptionMessage = "";
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                this.UpdateModel(userView); //corre todos los regex
                this.ValidateUserGenericFields(userView, session); //usuarioGlimpse y contraseñas de usuario
                mailAccounts = this.ValidateUserMailAccounts(userView, session); //direcciones de correo y contraseñas

                String cipherPassword = CryptoHelper.EncryptDefaultKey(userView);

                User newUser = new User(userView.Username, cipherPassword, userView.Firstname, userView.Lastname);
                foreach (MailAccount mailAccount in mailAccounts)
                {
                    mailAccount.SetUser(newUser);
                    mailAccount.SetOldestMailDate();
                    mailAccount.UpdateLabels(session);
                    mailAccount.Activate(session); //saveOrUpdate adentro
                    newUser.AddAccount(mailAccount);
                }
                newUser.SaveOrUpdate(session);
                tran.Commit();

                Session[AccountController.USER_NAME] = newUser;

                return JavaScript("window.location = '" + Url.Action("Index", "Home") + "'");
            }
            catch (InvalidOperationException exc) //model state invalido
            {
                tran.Rollback();
                foreach (ModelState wrongState in this.ModelState.Values.Where(x => x.Errors.Count > 0))
                    foreach (ModelError error in wrongState.Errors)
                        exceptionMessage += error.ErrorMessage;
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

        [AllowAnonymous]
        public ActionResult ValidateUserFields(UserViewModel userView)
        {
            String exceptionMessage = "";
            ISession session = NHibernateManager.OpenSession();
            try
            {
                this.UpdateModel(userView); //corre todos los regex
                this.ValidateUserGenericFields(userView, session); //nombre, apellido, usuarioGlimpse, contraseñas

                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (InvalidOperationException exc) //model state invalido
            {
                foreach (ModelState wrongState in this.ModelState.Values.Where(x => x.Errors.Count > 0))
                    foreach (ModelError error in wrongState.Errors)
                        exceptionMessage += error.ErrorMessage;
                if (String.IsNullOrEmpty(exceptionMessage))
                    exceptionMessage = exc.Message;
                return Json(new { success = false, message = exceptionMessage }, JsonRequestBehavior.AllowGet);
            }
            catch (GlimpseException exc)
            {
                Log.LogException(exc);
                return Json(new { success = false, message = exceptionMessage }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exc)
            {
                Log.LogException(exc, "Parametros: userName:(" + userView.Username + "), userPassowrd( " + userView.Password +
                                      "), userConfirmPassword(" + userView.ConfirmationPassword + "), userFirstName(" + userView.Firstname +
                                      "), userLastName(" + userView.Lastname + ").");
                return Json(new { success = false, message = "Error validando usuario." }, JsonRequestBehavior.AllowGet);
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
        #endregion
        #region Private Helpers
        [NonAction]
        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            else
                return RedirectToAction("Index", "Home");
        }
        [NonAction]
        private void ValidateUserGenericFields(UserViewModel userView, ISession session)
        {
            String exceptionMessage = "";

            if (!Glimpse.Models.User.IsGlimpseUser(userView.Username))
                exceptionMessage += "El nombre de usuario elegido no posee caracteres válidos.\n";
            if (userView.Password != userView.ConfirmationPassword)
                exceptionMessage += "Las contraseñas ingresadas deben coincidir.\n";
            if (Glimpse.Models.User.FindByUsername(userView.Username, session) != null)
                exceptionMessage += "El nombre de usuario elegido ya existe.\n";
            if (exceptionMessage != "")
                throw new GlimpseException(exceptionMessage);
        }
        [NonAction]
        private IList<MailAccount> ValidateUserMailAccounts(UserViewModel userView, ISession session)
        {
            String exceptionMessage = "";
            Boolean hasMainAccount = false;
            Boolean checkMainAccounts = true;
            IList<MailAccount> connectedMailAccounts = new List<MailAccount>();
            MailAccount mailAccount;
            MailAccount databaseMailAccount;

            foreach (MailAccountViewModel mailAccountView in userView.ListMailAccounts)
            {
                #region Valida Credenciales
                if (mailAccountView.Password == mailAccountView.ConfirmationPassword)
                {
                    try
                    {
                        mailAccount = new MailAccount(mailAccountView.Address, CryptoHelper.EncryptDefaultKey(mailAccountView));
                        mailAccount.ConnectLight();
                        connectedMailAccounts.Add(mailAccount);
                    }
                    catch (InvalidAuthenticationException)
                    {
                        exceptionMessage += "La dirección de correo (" + mailAccountView.Address +
                                            ") o la contraseña no son válidos.\n";
                    }
                }
                else
                    exceptionMessage += "Las contraseñas de la dirección: " + mailAccountView.Address + 
                                        " no son iguales.\n";
                #endregion
                #region Valida Cuentas Principales
                if (hasMainAccount && mailAccountView.IsMainAccount && checkMainAccounts)
                {
                    exceptionMessage += "Sólo puede existir una dirección de correo principal.\n";
                    checkMainAccounts = false;
                }
                else if (!hasMainAccount && mailAccountView.IsMainAccount)
                    hasMainAccount = true;
                #endregion
                #region Valida Unicidad en Base de datos
                databaseMailAccount = MailAccount.FindByAddress(mailAccountView.Address, session, true);
                if (databaseMailAccount != null)
                    exceptionMessage += "La dirección: " + mailAccountView.Address + " ya se encuentra "+
                                        "asociada a un usuario Glimpse.\n";
                #endregion
            }

            #region Validaciones de Requerimientos Obligatorios
            if (!hasMainAccount)
                exceptionMessage += "Una dirección de correo debe ser indicada como principal.\n";
            if (userView.ListMailAccounts.Count == 0)
                exceptionMessage += "El usuario Glimpse debe tener al menos una dirección de correo.\n";
            #endregion

            if (exceptionMessage != "")
                throw new GlimpseException(exceptionMessage);
            else
                return connectedMailAccounts; //para no tener que conectar de vuelta despues
        }
        #endregion
    }
}
