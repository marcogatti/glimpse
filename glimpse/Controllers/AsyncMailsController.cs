﻿using ActiveUp.Net.Mail;
using Glimpse.DataAccessLayer;
using Glimpse.DataAccessLayer.Entities;
using Glimpse.Exceptions;
using Glimpse.Helpers;
using Glimpse.MailInterfaces;
using Glimpse.Models;
using Glimpse.ViewModels;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Web.Mvc;

namespace Glimpse.Controllers
{
    [Authorize]
    public class AsyncMailsController : Controller
    {
        #region Action Methods
        public ActionResult GetMailBody(Int64 id = 0, Int64 mailAccountId = 0)
        {
            ISession session = NHibernateManager.OpenSession();
            try
            {
                MailAccount currentMailAccount = this.GetMailAccount(mailAccountId);
                String body = currentMailAccount.ReadMail(id, session);

                IList<ExtraEntity> mailExtras = Extra.FindByMailId(id, session);

                if (body.Contains("<img src=\"cid:"))
                    this.InsertEmbeddedExtraUrl(ref body, id, session);

                var returnInfo = this.PrepareBodyMail(body, mailExtras);

                JsonResult result = Json(new { success = true, mail = returnInfo }, JsonRequestBehavior.AllowGet);
                return result;
            }
            catch (Exception exc)
            {
                Log.LogException(exc, "Error generico GetMailBody. Parametros del mail: idMail(" + id.ToString() + ").");
                return Json(new { success = false, message = "Error al obtener el cuerpo del mail." }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                session.Close();
            }
        }
        public FileResult GetImage(Int64 id)
        {
            ISession session = NHibernateManager.OpenSession();
            try
            {
                Extra extra = Extra.FindByID(id, session);
                return File(extra.Entity.Data, extra.Entity.FileType, extra.Entity.Name);
            }
            catch (Exception exc)
            {
                Log.LogException(exc, "Parametros de la llamada: idExtra(" + id.ToString() + ").");
                return null;
            }
            finally
            {
                session.Close();
            }
        }
        public ActionResult GetMailsByDate(Int64 initial, Int64 final, Int64 mailAccountId = 0)
        {
            DateTime initialDate = AsyncMailsController.ConvertFromJS(initial);
            DateTime finalDate = AsyncMailsController.ConvertFromJS(final);

            ISession session = NHibernateManager.OpenSession();
            try
            {
                MailCollection mails;
                List<Object> mailsToReturn = new List<object>();
                MailAccount currentMailAccount = this.GetMailAccount(mailAccountId);
                mails = new MailCollection(currentMailAccount.GetMailsByDate(initialDate, finalDate, session));
                mailsToReturn = this.PrepareHomeMails(mails);

                JsonResult result = Json(new { success = true, mails = mailsToReturn }, JsonRequestBehavior.AllowGet);
                return result;
            }
            catch (Exception exc)
            {
                Log.LogException(exc, "Fechas del metodo: initialDate(" + initialDate.ToString() + "), finalDate(" + finalDate.ToString() + ").");
                return Json(new { success = false, message = "Error al obtener los mails." }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                session.Close();
            }
        }
        public ActionResult GetMailsByAmount(Int32 amountOfMails, Int64 mailAccountId = 0)
        {
            ISession session = NHibernateManager.OpenSession();
            try
            {
                MailCollection mails;
                List<Object> mailsToReturn = new List<object>();
                MailAccount currentMailAccount = this.GetMailAccount(mailAccountId);
                mails = currentMailAccount.GetMailsByAmount(amountOfMails, session);
                mailsToReturn = this.PrepareHomeMails(mails);

                JsonResult result = Json(new { success = true, mails = mailsToReturn }, JsonRequestBehavior.AllowGet);
                return result;
            }
            catch (Exception exc)
            {
                Log.LogException(exc, "Parametros de la llamada: amountOfMails(" + amountOfMails + ").");
                return Json(new { success = false, message = "Error al obtener los mails." }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                session.Close();
            }
        }

        [HttpPost]
        public ActionResult TrashMail(Int64 id, Int64 mailAccountId = 0)
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                MailAccount mailAccount = this.GetMailAccount(mailAccountId);
                Mail mail = new Mail(id, session);
                if (mail == null)
                    throw new Exception("Mail inexistente: " + id.ToString() + ".");
                String systemFolder = mail.GetSystemFolderName();
                mail.Delete(session); //DB
                mailAccount.TrashMail(mail, systemFolder); //IMAP
                if(systemFolder != "Trash")
                    MailsTasksHandler.SynchronizeTrash(mailAccount.Entity.Address);
                tran.Commit();

                JsonResult result = Json(new { success = true }, JsonRequestBehavior.AllowGet);
                return result;
            }
            catch (Exception exc)
            {
                tran.Rollback();
                Log.LogException(exc, "Parametros de la llamada: mailID(" + id.ToString() + ").");
                return Json(new { success = false, message = exc.Message }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                session.Close();
            }
        }
        [HttpPost]
        public ActionResult sendEmail(MailSentViewModel sendInfo, Int64 mailAccountId = 0)
        {
            try
            {
                MailAccount mailAccount = this.GetMailAccount(mailAccountId);
                mailAccount.SendMail(sendInfo.ToAddress, sendInfo.Body, sendInfo.Subject);
                return Json(new { success = true, address = sendInfo.ToAddress }, JsonRequestBehavior.AllowGet);
            }
            catch (InvalidRecipientsException exc)
            {
                Log.LogException(exc, "Parametros del mail a enviar: subjectMail(" + sendInfo.Subject + "), addressMail(" + sendInfo.ToAddress + ").");
                return Json(new { success = false, address = sendInfo.ToAddress }, JsonRequestBehavior.AllowGet);
            }
            catch (SmtpException exc)
            {
                Log.LogException(exc, "Parametros del mail a enviar: subjectMail(" + sendInfo.Subject + "), addressMail(" + sendInfo.ToAddress + ").");
                return Json(new { success = false, address = sendInfo.ToAddress }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exc)
            {
                Log.LogException(exc, "Parametros de la llamada: subjectMail(" + sendInfo.Subject + "), addressMail(" + sendInfo.ToAddress + ").");
                return Json(new { success = false, address = sendInfo.ToAddress }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public ActionResult AddLabel(String labelName, Int64 mailId, Int64 mailAccountId = 0)
        {
            bool success;
            try
            {
                MailAccount mailAccount = this.GetMailAccount(mailAccountId);

                using (ISession session = NHibernateManager.OpenSession())
                {
                    ITransaction tran = session.BeginTransaction();

                    Mail theMail = new Mail(mailId, session);
                    Label theLabel = Label.FindByName(mailAccount, labelName, session);
                    theMail.AddLabel(theLabel, session); //DB
                    mailAccount.AddLabelIMAP(theMail, theLabel); //IMAP
                    tran.Commit();

                    success = true;
                }
            }
            catch (Exception exc)
            {
                success = false;
                Log.LogException(exc, "Parametros de la llamada: label(" + labelName + "), mailId(" + mailId.ToString() + ").");
            }
            return Json(new { success = success }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult RemoveLabel(String label, Int64 mailId, Int64 mailAccountId = 0)
        {
            ISession session = NHibernateManager.OpenSession();
            try
            {
                MailAccount currentMailAccount = this.GetMailAccount(mailAccountId);
                Mail mail = new Mail(mailId, session);
                Boolean success;

                if (mail.Entity.MailAccountEntity.Id == currentMailAccount.Entity.Id)
                {
                    mail.RemoveLabel(label, session); //DB
                    currentMailAccount.RemoveMailLabel(label, mail.Entity.Gm_mid); //IMAP
                    success = true;
                }
                else
                {
                    success = false;
                }

                JsonResult result = Json(new { success = success }, JsonRequestBehavior.AllowGet);
                return result;
            }
            catch (Exception exc)
            {
                Log.LogException(exc, "Parametros de la llamada: label(" + label + "), gmID(" + mailId.ToString() + ").");
                return Json(new { success = false, message = "Error al remover label." }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                session.Close();
            }
        }
        [HttpPost]
        public ActionResult RenameLabel(String oldLabelName, String newLabelName, Int64 mailAccountId = 0)
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                MailAccount currentMailAccount = this.GetMailAccount(mailAccountId);
                IList<LabelEntity> labels = Label.FindByAccount(currentMailAccount.Entity, session);
                labels = labels.Where(x => x.Name.Contains(oldLabelName) && x.SystemName == null).ToList();
                foreach (LabelEntity label in labels)
                {
                    new Label(label).Rename(oldLabelName, newLabelName, session); //BD
                }
                currentMailAccount.RenameLabel(oldLabelName, newLabelName); //IMAP
                tran.Commit();

                JsonResult result = Json(new { success = true }, JsonRequestBehavior.AllowGet);
                return result;
            }
            catch (Exception exc)
            {
                tran.Rollback();
                Log.LogException(exc, "Parametros del metodo: oldLabelName(" + oldLabelName +
                                      "), newLabelName(" + newLabelName + "), mailAccountId(" + mailAccountId.ToString() + ").");
                return Json(new { success = false, message = "Error al renombrar label." }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                session.Close();
            }
        }
        [HttpPost]
        public ActionResult DeleteLabel(String labelName, Int64 mailAccountId = 0)
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                MailAccount currentMailAccount = this.GetMailAccount(mailAccountId);
                Label labelToDelete = Label.FindByName(currentMailAccount, labelName, session);
                labelToDelete.Delete(session); //BD
                currentMailAccount.DeleteLabel(labelName); //IMAP
                tran.Commit();

                JsonResult result = Json(new { success = true }, JsonRequestBehavior.AllowGet);
                return result;
            }
            catch (Exception exc)
            {
                tran.Rollback();
                Log.LogException(exc, "Parametros del metodo: labelName(" + labelName + "), mailAccountId(" + mailAccountId.ToString() + ").");
                return Json(new { success = false, message = "Error al eliminar label." }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                session.Close();
            }
        }
        [HttpPost] //Hay que testearlo y pasarlo al AccountController
        public ActionResult ResetPassword(String username)
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                String newPassword = this.GenerateRandomPassword(16);
                String newPasswordEnc = CryptoHelper.EncryptDefaultKey(newPassword);

                tran = session.BeginTransaction();

                User user = Glimpse.Models.User.FindByUsername(username, session);
                if (user == null)
                    throw new Exception("Usuario inexistente: " + username + ".");
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
        [HttpPost] //Hay que testearlo y pasarlo al AccountController
        public ActionResult ChangePassword(String username, String oldPassword, String newPassword)
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                String oldPasswordEnc = CryptoHelper.EncryptDefaultKey(oldPassword);
                String newPasswordEnc = CryptoHelper.EncryptDefaultKey(newPassword);

                User user = Glimpse.Models.User.FindByUsername(username, session);
                if (user == null)
                    throw new Exception("Usuario inexistente.");
                user.ChangePassword(oldPasswordEnc, newPasswordEnc, session);

                JsonResult result = Json(new { success = true, message = "La contraseña ha sido reinicializada." }, JsonRequestBehavior.AllowGet);
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
        #endregion
        #region Private Helpers
        private static DateTime ConvertFromJS(Int64 JSDate)
        {
            DateTime date = new DateTime(1970, 1, 1) + new TimeSpan(JSDate * 10000);
            return date;
        }
        private List<Object> PrepareHomeMails(MailCollection mails)
        {
            List<Object> preparedMails = new List<Object>();

            foreach (MailEntity mail in mails)
            {
                Int64 currentAge = DateTime.Now.Ticks - mail.Date.Ticks;
                List<Object> currentLabels = PrepareLabels(mail.Labels);
                Object anEmail = new
                {
                    id = mail.Id,
                    subject = mail.Subject,
                    date = mail.Date,
                    age = currentAge,
                    from = new
                    {
                        address = mail.From.MailAddress,
                        name = mail.From.Name
                    },

                    to = mail.ToAddress,
                    cc = mail.CC,
                    bcc = mail.BCC,
                    bodypeek = mail.BodyPeek,
                    tid = mail.Gm_tid,
                    seen = mail.Seen,
                    flagged = mail.Flagged,
                    labels = currentLabels,
                    importance = mail.Importance
                };
                preparedMails.Add(anEmail);
            }
            return preparedMails;
        }
        private List<Object> PrepareLabels(IList<LabelEntity> labels)
        {
            List<Object> returnLabels = new List<object>();
            foreach (LabelEntity label in labels)
            {
                if (label.SystemName == null)
                {
                    returnLabels.Add(new
                    {
                        name = label.Name,
                        system_name = label.SystemName,
                        mail_account = label.MailAccountEntity.Id
                    });
                }
            }
            return returnLabels;
        }
        private Object PrepareBodyMail(String body, IList<ExtraEntity> extras)
        {
            List<Object> extrasMetadata = new List<object>();
            foreach (ExtraEntity extra in extras)
            {
                Object anExtra = new
                {
                    id = extra.Id,
                    name = extra.Name,
                    size = extra.Size
                };
                extrasMetadata.Add(anExtra);
            }
            Object returnInfo = new 
            {
                body = body,
                extras = extrasMetadata
            };
            return returnInfo;
        }
        private MailAccount GetMailAccount(Int64 id)
        {
            User user = (User)Session[AccountController.USER_NAME];
            IList<MailAccount> mailAccounts = user.GetAccounts();
            //return mailAccounts.Where<MailAccount>(x => x.Entity.Id == id).Single<MailAccount>();
            return mailAccounts[0]; //harcodeado para que funcione hasta que la vista mande los ids
        }
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
        private void InsertEmbeddedExtraUrl(ref String body, Int64 mailID, ISession session)
        {
            MailEntity mailEntity = session.CreateCriteria<MailEntity>()
                                           .Add(Restrictions.Eq("Id", mailID))
                                           .UniqueResult<MailEntity>();
            IList<ExtraEntity> embeddedExtras = session.CreateCriteria<ExtraEntity>()
                                                  .Add(Restrictions.Eq("MailEntity", mailEntity))
                                                  .Add(Expression.Disjunction()
                                                        .Add(Restrictions.Eq("ExtraType", Convert.ToInt16(2)))
                                                        .Add(Restrictions.Eq("ExtraType", Convert.ToInt16(1))))
                                                  .List<ExtraEntity>();
            foreach (ExtraEntity embeddedExtra in embeddedExtras)
            {
                body = body.Replace("cid:" +  embeddedExtra.EmbObjectContentId, Url.Action("GetImage", "AsyncMails", new { id = embeddedExtra.Id }, this.Request.Url.Scheme));
            }
        }
        #endregion
    }
}
