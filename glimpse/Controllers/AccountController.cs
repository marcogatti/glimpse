using Glimpse.Attributes;
using Glimpse.DataAccessLayer;
using Glimpse.Exceptions;
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
using System.Text;
using System.Web.Mvc;
using System.Web.Security;

namespace Glimpse.Controllers
{
    public class AccountController : Controller
    {
        public const String USER_NAME = "Username";

        #region Public Methods
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

                    if (!workingOffline)
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
        public ActionResult CreateUser(String viewAccountName1, String viewAccountPass1, Boolean viewAccountCheck1,
                                       String viewAccountName2, String viewAccountPass2, Boolean viewAccountCheck2,
                                       String viewAccountName3, String viewAccountPass3, Boolean viewAccountCheck3,
                                       String username, String userPassword, String userConfirmationPassword,
                                       String firstName, String Lastname)
        {
            IList<MailAccount> mailAccounts;
            String exceptionMessage = "";
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                #region Initialize UserView
                MailAccountViewModel mailAccountView1 = new MailAccountViewModel();
                MailAccountViewModel mailAccountView2 = new MailAccountViewModel();
                MailAccountViewModel mailAccountView3 = new MailAccountViewModel();
                List<MailAccountViewModel> mailAccountsView = new List<MailAccountViewModel>();
                UserViewModel userView = new UserViewModel();

                mailAccountView1.Address = viewAccountName1;
                mailAccountView1.Password = viewAccountPass1;
                mailAccountView1.IsMainAccount = viewAccountCheck1;

                mailAccountView2.Address = viewAccountName2;
                mailAccountView2.Password = viewAccountPass2;
                mailAccountView2.IsMainAccount = viewAccountCheck2;

                mailAccountView3.Address = viewAccountName3;
                mailAccountView3.Password = viewAccountPass3;
                mailAccountView3.IsMainAccount = viewAccountCheck3;

                mailAccountsView.Add(mailAccountView1);
                mailAccountsView.Add(mailAccountView2);
                mailAccountsView.Add(mailAccountView3);

                userView.Username = username;
                userView.Password = userPassword;
                userView.ConfirmationPassword = userConfirmationPassword;
                userView.Firstname = firstName;
                userView.Lastname = Lastname;
                userView.ListMailAccounts = mailAccountsView;
                userView.FilterNullAccounts();
                #endregion

                this.UpdateModel(userView); //corre todos los regex
                this.ValidateUserGenericFields(userView, session); //usuarioGlimpse y contraseñas de usuario
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

                Sender.SendGreetingsPassword(newUser.Entity.Username, newUser.mailAccounts.Where(x => x.Entity.IsMainAccount).Single().Entity.Address);

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
                return Json(new { success = false, message = exc.GlimpseMessage }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exc)
            {
                tran.Rollback();
                Log.LogException(exc);
                return Json(new { success = false, message = "Error creando usuario." }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                session.Close();
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [AjaxOnly]
        public ActionResult ValidateUserFields(String firstname, String lastname, String username, String password, String confirmationPassword)
        {
            String exceptionMessage = "";
            ISession session = NHibernateManager.OpenSession();
            
            try
            {
                UserViewModel userView = new UserViewModel();

                userView.Firstname = firstname;
                userView.Lastname = lastname;
                userView.Username = username;
                userView.Password = password;
                userView.ConfirmationPassword = confirmationPassword;

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
                return Json(new { success = false, message = exc.GlimpseMessage }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exc)
            {
                Log.LogException(exc, "Parametros: userName:(" + username + "), password( " + password +
                                      "), confirmationPassword(" + confirmationPassword + "), firstname(" + firstname +
                                      "), lastname(" + lastname + ").");
                return Json(new { success = false, message = "Error validando usuario." }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                session.Close();
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [AjaxOnly]
        public ActionResult ResetPassword(String username)
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                User user = Glimpse.Models.User.FindByUsername(username, session);
                if (user == null)
                    throw new GlimpseException("Usuario inexistente: " + username + ".");
                String newPassword = this.GenerateRandomPassword(16);
                String newPasswordEnc = CryptoHelper.EncryptDefaultKey(newPassword);
                user.ChangePassword(user.Entity.Password, newPasswordEnc, session);
                MailAccount.SendResetPasswordMail(user, newPassword, session);
                tran.Commit();

                JsonResult result = Json(new { success = true, message = "La contraseña ha sido reinicializada." }, JsonRequestBehavior.AllowGet);
                return result;
            }
            catch (GlimpseException exc)
            {
                tran.Rollback();
                return Json(new { success = false, message = exc.GlimpseMessage }, JsonRequestBehavior.AllowGet);
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
        #endregion
        #region Authorized Methods
        [HttpPost]
        [AjaxOnly]
        [Authorize]
        public ActionResult EditUserAccounts(String mailAccount1, String password1, Boolean isMainAccount1,
                                             String mailAccount2, String password2, Boolean isMainAccount2,
                                             String mailAccount3, String password3, Boolean isMainAccount3)
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                User sessionUser = (User)Session[AccountController.USER_NAME];
                if (sessionUser == null)
                    throw new GlimpseException("No se encontró el usuario.");

                #region Initialize UserView
                MailAccountViewModel mailAccountView1 = new MailAccountViewModel();
                MailAccountViewModel mailAccountView2 = new MailAccountViewModel();
                MailAccountViewModel mailAccountView3 = new MailAccountViewModel();
                UserViewModel userView = new UserViewModel();
                List<MailAccountViewModel> mailAccountsView = new List<MailAccountViewModel>();
                List<MailAccount> removedMailAccounts = new List<MailAccount>();

                mailAccountView1.Address = mailAccount1;
                mailAccountView1.Password = password1;
                mailAccountView1.IsMainAccount = isMainAccount1;

                mailAccountView2.Address = mailAccount2;
                mailAccountView2.Password = password2;
                mailAccountView2.IsMainAccount = isMainAccount2;

                mailAccountView3.Address = mailAccount3;
                mailAccountView3.Password = password3;
                mailAccountView3.IsMainAccount = isMainAccount3;

                mailAccountsView.Add(mailAccountView1);
                mailAccountsView.Add(mailAccountView2);
                mailAccountsView.Add(mailAccountView3);

                userView.Username = sessionUser.Entity.Username;
                userView.ListMailAccounts = mailAccountsView;
                userView.FilterNullAccounts();
                #endregion

                this.ValidateUserMailAccounts(userView, session); //direcciones de correo y contraseñas

                foreach (MailAccount removedMailAccount in sessionUser.mailAccounts
                            .Where(x => !userView.ListMailAccounts.Any(c => c.Address == x.Entity.Address)))
                {
                    removedMailAccount.Disconnect();
                    removedMailAccount.Deactivate(session); //saveOrUpdate adentro
                    removedMailAccounts.Add(removedMailAccount);
                }

                foreach (MailAccount removedMailAccount in removedMailAccounts)
                    sessionUser.mailAccounts.Remove(removedMailAccount);

                foreach (MailAccountViewModel mailAccountView in userView.ListMailAccounts)
                {
                    if (sessionUser.mailAccounts.Any(x => x.Entity.Address == mailAccountView.Address)) //si la cuenta ya existia
                    {
                        MailAccount editedMailAccount = sessionUser.mailAccounts.Where(x => x.Entity.Address == mailAccountView.Address).Single();
                        editedMailAccount.Entity.Password = CryptoHelper.EncryptDefaultKey(mailAccountView);
                        editedMailAccount.SetUser(sessionUser);
                        editedMailAccount.ConnectLight();
                        if (mailAccountView.IsMainAccount)
                            editedMailAccount.SetAsMainAccount(true);
                        else
                            editedMailAccount.SetAsMainAccount(false);
                        editedMailAccount.Activate(session); //saveOrUpdate adentro
                    }
                    else //si la cuenta es nueva
                    {
                        MailAccount newMailAccount = new MailAccount(mailAccountView.Address, CryptoHelper.EncryptDefaultKey(mailAccountView.Password));
                        newMailAccount.SetUser(sessionUser);
                        if (mailAccountView.IsMainAccount)
                            newMailAccount.SetAsMainAccount(true);
                        else
                            newMailAccount.SetAsMainAccount(false);
                        newMailAccount.Activate(session); //saveOrUpdate adentro
                        newMailAccount.ConnectFull();
                        sessionUser.AddAccount(newMailAccount);
                    }
                }

                tran.Commit();
                Session[AccountController.USER_NAME] = sessionUser;

                return Json(new { success = true, url = Url.Action("Index", "Home") }, JsonRequestBehavior.AllowGet);
            }
            catch (GlimpseException exc)
            {
                tran.Rollback();
                return Json(new { success = false, message = exc.GlimpseMessage }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exc)
            {
                tran.Rollback();
                Log.LogException(exc, "Parametros: viewAccountName1:(" + mailAccount1 + "), viewAccountPass1( " + password1 +
                                      "), viewAccountCheck1(" + isMainAccount1 + "), viewAccountName2(" + mailAccount2 +
                                      "), viewAccountPass1(" + password1 + "), viewAccountCheck2(" + isMainAccount2 +
                                      "),  viewAccountName3:(" + mailAccount3 + "), viewAccountPass3( " + password3 +
                                      "), viewAccountCheck3(" + isMainAccount3 + ").");
                return Json(new { success = false, message = "Error modificando usuario." }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                session.Close();
            }
        }

        [HttpPost]
        [AjaxOnly]
        [Authorize]
        public ActionResult EditUserPassword(String oldPassword, String newPassword, String newPasswordConfirmation)
        {
            String exceptionMessage = "";
            ISession session = NHibernateManager.OpenSession();
            try
            {
                User sessionUser = (User)Session[AccountController.USER_NAME];
                if (sessionUser == null)
                    throw new GlimpseException("No se encontró el usuario.");

                if (!Glimpse.Models.User.IsPassword(newPassword))
                    exceptionMessage += "La contraseña ingresada contiene caracteres no válidos, es muy corta o muy larga.";
                if (newPassword != newPasswordConfirmation)
                    exceptionMessage += "Las contraseñas ingresadas deben coincidir.";

                if (exceptionMessage != "")
                    throw new GlimpseException(exceptionMessage);

                sessionUser.ChangePassword(CryptoHelper.EncryptDefaultKey(oldPassword), CryptoHelper.EncryptDefaultKey(newPassword), session);
                session.Flush();

                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (GlimpseException exc)
            {
                return Json(new { success = false, message = exc.GlimpseMessage }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exc)
            {
                Log.LogException(exc, "Parametros: oldPassword:(" + oldPassword + "), newPassword( " + newPassword +
                                      "), newPasswordConfirmation(" + newPasswordConfirmation + ").");
                return Json(new { success = false, message = "Error actualizando contraseñas." }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                session.Close();
            }
        }

        [HttpPost]
        [AjaxOnly]
        [Authorize]
        public ActionResult EditUserPersonalData(String firstName, String lastName, String country, String city, String tel)
        {
            ISession session = NHibernateManager.OpenSession();
            try
            {
                User sessionUser = (User)Session[AccountController.USER_NAME];
                if (sessionUser == null)
                    throw new GlimpseException("No se encontró el usuario.");

                sessionUser.Entity.Firstname = firstName;
                sessionUser.Entity.Lastname = lastName;
                sessionUser.Entity.Country = country;
                sessionUser.Entity.City = city;
                sessionUser.Entity.Telephone = tel;
                sessionUser.SaveOrUpdate(session);
                session.Flush();

                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (GlimpseException exc)
            {
                Log.LogException(exc);
                return Json(new { success = false, message = exc.GlimpseMessage }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exc)
            {
                Log.LogException(exc, "Parametros: firstName:(" + firstName + "), lastName( " + lastName +
                                      "), country(" + country + "), city(" + city + "), tel(" + tel + ").");
                return Json(new { success = false, message = "Error modificando los datos del usuario." }, JsonRequestBehavior.AllowGet);
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
            return builder.ToString();
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
                try
                {
                    mailAccount = new MailAccount(mailAccountView.Address, CryptoHelper.EncryptDefaultKey(mailAccountView));
                    mailAccount.ConnectLight(); //si pasa este punto es que esta bien y va a ser devuelta
                    if (mailAccountView.IsMainAccount)
                        mailAccount.Entity.IsMainAccount = true;
                    connectedMailAccounts.Add(mailAccount);
                }
                catch (ArgumentNullException)
                {
                    exceptionMessage += "La dirección de correo (" + mailAccountView.Address +
                                        ") no posee contraseña.\n";
                }
                catch (InvalidAuthenticationException)
                {
                    exceptionMessage += "La dirección de correo (" + mailAccountView.Address +
                                        ") o la contraseña no son válidos.\n";
                }
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
