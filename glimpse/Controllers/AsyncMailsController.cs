﻿using ActiveUp.Net.Mail;
using Glimpse.Attributes;
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
using System.Web.Mvc;

namespace Glimpse.Controllers
{
    [AjaxOnly]
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
        public FileResult GetFile(Int64 id, Int64 mailAccountId = 0)
        {
            ISession session = NHibernateManager.OpenSession();
            try
            {
                this.GetMailAccount(mailAccountId); //valida que el mailAccoun pertenezca al User
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
                    throw new GlimpseException("Mail inexistente: " + id.ToString() + ".");
                mailAccount.TrashMail(mail, session); //IMAP y BD
                //if(systemFolder != "Trash")
                //    MailsTasksHandler.SynchronizeTrash(mailAccount.Entity.Address);
                tran.Commit();

                JsonResult result = Json(new { success = true }, JsonRequestBehavior.AllowGet);
                return result;
            }
            catch (GlimpseException exc)
            {
                tran.Rollback();
                Log.LogException(exc, "Parametros de la llamada: mailID(" + id.ToString() + ").");
                return Json(new { success = false, message = exc.GlimpseMessage }, JsonRequestBehavior.AllowGet);
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
        public ActionResult SendEmail(MailSentViewModel sendInfo, Int64 mailAccountId = 0)
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
        public ActionResult AddLabel(String labelName, Int64 mailId, Int64 mailMailAccountId = 0)
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                MailAccount mailAccount = this.GetMailAccount(mailMailAccountId);
                Label theLabel = this.GetAccountLabel(labelName, mailAccount, session);
                if(theLabel == null)
                    return Json(new { success = false, message = "No se ha podido encontrar la etiqueta con el nombre:" + labelName + "." }, JsonRequestBehavior.AllowGet);
                Mail theMail = new Mail(mailId, session);

                if (theMail.Entity.MailAccountEntity.Id != theLabel.Entity.MailAccountEntity.Id) //si el mail no es del mismo MailAccount de la etiqueta
                    this.CreateLabel(labelName, mailMailAccountId); //DB e IMAP

                theMail.AddLabel(theLabel, session); //DB
                mailAccount.AddLabelIMAP(theMail, theLabel); //IMAP

                tran.Commit();
                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exc)
            {
                tran.Rollback();
                Log.LogException(exc, "Parametros de la llamada: label(" + labelName + "), mailId(" +
                                       mailId.ToString() + "), mailAccountId(" + mailMailAccountId + ").");
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                session.Close();
            }
        }
        [HttpPost]
        public ActionResult RemoveLabel(String labelName, Int64 mailId, Boolean isSystemLabel, Int64 mailAccountId = 0)
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                MailAccount currentMailAccount = this.GetMailAccount(mailAccountId);
                Mail mail = new Mail(mailId, session);

                if (mail.Entity.MailAccountEntity.Id != currentMailAccount.Entity.Id)
                    return Json(new { success = false, message = "El mail indicado no pertenece a la cuenta indicada." }, JsonRequestBehavior.AllowGet);

                mail.RemoveLabel(labelName, isSystemLabel, session); //DB
                currentMailAccount.RemoveMailLabel(labelName, mail.Entity.Gm_mid); //IMAP
                tran.Commit();

                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exc)
            {
                tran.Rollback();
                Log.LogException(exc, "Parametros de la llamada: labelName(" + labelName + "), gmID(" + mailId.ToString() + "), mailAccountId(" + mailAccountId + ").");
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
        public ActionResult CreateLabel(String labelName, Int64 mailAccountId = 0)
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                MailAccount currentMailAccount = this.GetMailAccount(mailAccountId);
                Label existingLabel = Label.FindByName(currentMailAccount, labelName, session);
                if (existingLabel != null)
                    return Json(new { success = false, message = "Ya existe una etiqueta con ese nombre." }, JsonRequestBehavior.AllowGet);
                Label newLabel = new Label(new LabelEntity());
                newLabel.Entity.SystemName = null;
                newLabel.Entity.Name = labelName;
                newLabel.Entity.Active = true;
                newLabel.SaveOrUpdate(session); //BD
                currentMailAccount.CreateLabel(labelName); //IMAP
                tran.Commit();

                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exc)
            {
                tran.Rollback();
                Log.LogException(exc, "Parametros del metodo: labelName(" + labelName + "), mailAccountId(" + mailAccountId.ToString() + ").");
                return Json(new { success = false, message = "Error al crear label." }, JsonRequestBehavior.AllowGet);
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
        [HttpPost]
        public ActionResult ChangeImportance(Int64 mailId, Boolean isIncrease, Int64 mailAccountId = 0)
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                MailAccount currentMailAccount = this.GetMailAccount(mailAccountId);
                Mail theMail = new Mail(mailId, session);;
                if(isIncrease)
                    theMail.SetImportance((UInt16)(theMail.Entity.Importance + 1), session);
                else
                    theMail.SetImportance((UInt16)(theMail.Entity.Importance - 1), session);
                tran.Commit();
                JsonResult result = Json(new { success = true }, JsonRequestBehavior.AllowGet);
                return result;
            }
            catch (Exception exc)
            {
                tran.Rollback();
                Log.LogException(exc, "Parametros del metodo: mailId(" + mailId.ToString() +
                                      "), isIncrease(" + isIncrease.ToString() + "), mailAccountId(" + mailAccountId.ToString() + ").");
                return Json(new { success = false, message = "Error al aumentar importancia." }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                session.Close();
            }
        }
        [HttpPost]
        public ActionResult SetReadFlag(Int64 mailId, Boolean seenFlag, Int64 mailAccountId = 0)
        {
            ISession session = NHibernateManager.OpenSession();
            try
            {
                MailAccount currentMailAccount = this.GetMailAccount(mailAccountId);
                Mail mail = new Mail(mailId, session);
                currentMailAccount.SetReadFlag(mail, seenFlag, session);

                JsonResult result = Json(new { success = true }, JsonRequestBehavior.AllowGet);
                return result;
            }
            catch (Exception exc)
            {
                Log.LogException(exc, "Parametros del metodo: mailId(" + mailId.ToString() +
                                      "), seenFlag(" + seenFlag.ToString() + "), mailAccountId(" + mailAccountId.ToString() + ").");
                return Json(new { success = false, message = "Error al marcar flag de leido." }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                session.Close();
            }
        }
        [HttpPost]
        public ActionResult ArchiveMail(Int64 mailId, Int64 mailAccountId = 0)
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                MailAccount currentMailAccount = this.GetMailAccount(mailAccountId);
                Mail mail = new Mail(mailId, session);
                mail.Archieve(session);
                currentMailAccount.ArchiveMail(mail);
                tran.Commit();

                JsonResult result = Json(new { success = true }, JsonRequestBehavior.AllowGet);
                return result;
            }
            catch (Exception exc)
            {
                tran.Rollback();
                Log.LogException(exc, "Parametros del metodo: mailId(" + mailId.ToString() +
                                      "), mailAccountId(" + mailAccountId.ToString() + ").");
                return Json(new { success = false, message = "Error al archivar mail." }, JsonRequestBehavior.AllowGet);
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
                    date = DateTimeHelper.changeToUtc(mail.Date),
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
                    importance = mail.Importance,
                    mailaccount = mail.MailAccountEntity.Id
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
                returnLabels.Add(new
                {
                    name = label.Name,
                    system_name = label.SystemName,
                    mail_account = label.MailAccountEntity.Id
                });

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
            if(id != 0)
                return mailAccounts.Where<MailAccount>(x => x.Entity.Id == id).Single<MailAccount>();
            else
                return mailAccounts[0]; //harcodeado para que funcione hasta que la vista mande los ids
        }
        private Label GetAccountLabel(String labelName, MailAccount possibleMainAccount, ISession session)
        {
            User user = (User)Session[AccountController.USER_NAME];
            IList<MailAccount> mailAccounts = user.GetAccounts();

            //busco en el mailAccount del mismo mail primero
            IList<LabelEntity> accountLabels = Label.FindByAccount(possibleMainAccount.Entity, session);
            if (accountLabels.Any(x => x.Name == labelName))
                return new Label(accountLabels.Where(x => x.Name == labelName).Single());

            //si no esta, busco en el resto
            foreach (MailAccount mailAccount in mailAccounts.Where(x => x.Entity != possibleMainAccount.Entity))
            {
                accountLabels = Label.FindByAccount(mailAccount.Entity, session);
                if (accountLabels.Any(x => x.Name == labelName))
                    return new Label(accountLabels.Where(x => x.Name == labelName).Single());
            }
            return null;
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
                body = body.Replace("cid:" +  embeddedExtra.EmbObjectContentId, Url.Action("GetFile", "AsyncMails", new { id = embeddedExtra.Id }, this.Request.Url.Scheme));
            }
        }
        #endregion
    }
}
