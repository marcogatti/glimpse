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
using System.Text;
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
                userView.FilterNullAccounts();
                mailAccounts = this.ValidateUserMailAccounts(userView, session); //direcciones de correo y contraseñas

                String cipherPassword = CryptoHelper.EncryptDefaultKey(userView);

                User newUser = new User(userView.Username, cipherPassword, userView.Firstname, userView.Lastname);
                newUser.SaveOrUpdate(session);

                foreach (MailAccount mailAccount in mailAccounts)
                {
                    mailAccount.SetUser(newUser);
                    mailAccount.SetOldestMailDate();
                    mailAccount.Activate(session); //saveOrUpdate adentro
                    mailAccount.UpdateLabels(session);
                    newUser.AddAccount(mailAccount);
                }

                tran.Commit();
                Session[AccountController.USER_NAME] = newUser;
                new CookieHelper().AddUsernameCookie(newUser.Entity.Username);
                FormsAuthentication.SetAuthCookie(newUser.Entity.Username, true);

                return Redirect(Url.Action("Index", "Home"));
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
                return Json(new { success = false, message = exc.GlimpseMessage}, JsonRequestBehavior.AllowGet);
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

        [HttpPost]
        public ActionResult EditUser(UserViewModel userView)
        {
            String exceptionMessage = "";
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                this.UpdateModel(userView); //corre todos los regex
                this.ValidateUserGenericFields(userView, session); //usuarioGlimpse y contraseñas de usuario
                this.ValidateUserMailAccounts(userView, session); //direcciones de correo y contraseñas

                User editedUser = Glimpse.Models.User.FindByUsername(userView.Username, session);
                if (editedUser == null)
                    throw new GlimpseException("Usuario inexistente: " + userView.Username + ".");
                User sessionUser = (User)Session[AccountController.USER_NAME];
                if (sessionUser == null || editedUser.Entity.Username != sessionUser.Entity.Username)
                    throw new GlimpseException("Usuario de la sesión es distinto del usuario realizando la operación.");

                editedUser.Entity.Password = CryptoHelper.EncryptDefaultKey(userView);
                editedUser.Entity.Firstname = userView.Firstname;
                editedUser.Entity.Lastname = userView.Lastname;

                editedUser.SaveOrUpdate(session);

                foreach (MailAccount removedMailAccount in editedUser.mailAccounts
                            .Where(x => !userView.ListMailAccounts.Any(c => c.Address == x.Entity.Address)))
                {
                    removedMailAccount.Disconnect();
                    removedMailAccount.Deactivate(session); //saveOrUpdate adentro
                    editedUser.mailAccounts.Remove(removedMailAccount);
                }

                foreach (MailAccountViewModel mailAccountView in userView.ListMailAccounts)
                {
                    if (editedUser.mailAccounts.Any(x => x.Entity.Address == mailAccountView.Address)) //si la cuenta ya existia
                    {
                        MailAccount editedMailAccount = editedUser.mailAccounts.Where(x => x.Entity.Address == mailAccountView.Address).Single();
                        editedMailAccount.Entity.Password = CryptoHelper.EncryptDefaultKey(mailAccountView);
                        editedMailAccount.SetUser(editedUser);
                        editedMailAccount.ConnectLight();
                        if(mailAccountView.IsMainAccount)
                            editedMailAccount.SetAsMainAccount();
                        editedMailAccount.Activate(session); //saveOrUpdate adentro
                    }
                    else //si la cuenta es nueva
                    {
                        MailAccount newMailAccount = new MailAccount(mailAccountView.Address, CryptoHelper.EncryptDefaultKey(mailAccountView.Password));
                        newMailAccount.SetUser(editedUser);
                        if (mailAccountView.IsMainAccount)
                            newMailAccount.SetAsMainAccount();
                        newMailAccount.Activate(session); //saveOrUpdate adentro
                        newMailAccount.ConnectFull();
                        editedUser.AddAccount(newMailAccount);
                    }
                }

                tran.Commit();
                Session[AccountController.USER_NAME] = editedUser;

                return Redirect(Url.Action("Index", "Home"));
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
                return Json(new { success = false, message = "Error modificando usuario." }, JsonRequestBehavior.AllowGet);
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
                return Json(new { success = false, message = exc.Message }, JsonRequestBehavior.AllowGet);
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

        [HttpPost]
        public ActionResult ResetPassword(String username)
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                User user = Glimpse.Models.User.FindByUsername(username, session);
                if (user == null)
                    throw new Exception("Usuario inexistente: " + username + ".");
                User sessionUser = (User)Session[AccountController.USER_NAME];
                if (sessionUser == null || user.Entity.Username != sessionUser.Entity.Username)
                    throw new GlimpseException("Usuario de la sesión es distinto del usuario realizando la operación.");
                String newPassword = this.GenerateRandomPassword(16);
                String newPasswordEnc = CryptoHelper.EncryptDefaultKey(newPassword);
                user.ChangePassword(user.Entity.Password, newPasswordEnc, session);
                MailAccount.SendResetPasswordMail(user, newPassword, session);
                tran.Commit();

                JsonResult result = Json(new { success = true, message = "La contraseña ha sido reinicializada." }, JsonRequestBehavior.AllowGet);
                return result;
            }
            catch (Exception exc)
            {
                tran.Rollback();
                Log.LogException(exc);
                return Json(new { success = false, message = "Error al reiniciar la contraseña." }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                session.Close();
            }
        }

        [HttpPost]
        public ActionResult ChangePassword(String username, String oldPassword, String newPassword)
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                if (!Glimpse.Models.User.IsPassword(newPassword))
                    throw new GlimpseException("La nueva contraseña ingresada no posee caracteres válidos, es muy corta o muy larga.");
                String oldPasswordEnc = CryptoHelper.EncryptDefaultKey(oldPassword);
                String newPasswordEnc = CryptoHelper.EncryptDefaultKey(newPassword);
                User user = Glimpse.Models.User.FindByUsername(username, session);
                if (user == null)
                    throw new GlimpseException("Usuario inexistente: " + username + ".");
                User sessionUser = (User)Session[AccountController.USER_NAME];
                if (sessionUser == null || user.Entity.Username != sessionUser.Entity.Username)
                    throw new GlimpseException("Usuario de la sesión es distinto del usuario realizando la operación.");
                sessionUser.ChangePassword(oldPasswordEnc, newPasswordEnc, session);
                tran.Commit();

                JsonResult result = Json(new { success = true, message = "La contraseña ha sido cambiada con éxito." }, JsonRequestBehavior.AllowGet);
                return result;
            }
            catch (WrongClassException exc)
            {
                tran.Rollback();
                Log.LogException(exc);
                return Json(new { success = false, message = exc.Message }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exc)
            {
                tran.Rollback();
                Log.LogException(exc);
                return Json(new { success = false, message = "Error al reiniciar la contraseña." }, JsonRequestBehavior.AllowGet);
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
        private String GenerateRandomPassword(Int16 size)
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            char ch;
            for (Int16 i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            return CryptoHelper.EncryptDefaultKey(builder.ToString());
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
                        mailAccount.ConnectLight(); //si pasa este punto es que esta bien y va a ser devuelta
                        if (mailAccountView.IsMainAccount)
                            mailAccount.Entity.IsMainAccount = true;
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
                if (databaseMailAccount != null && databaseMailAccount.Entity.User.Username != userView.Username)
                    exceptionMessage += "La dirección: " + mailAccountView.Address + " ya se encuentra " +
                                        "asociada a otro usuario Glimpse.\n";
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
