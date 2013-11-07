using ActiveUp.Net.Mail;
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Glimpse.Controllers
{
    [Authorize]
    public class AsyncMailsController : Controller
    {
        public const String FILES = "Attachments";

        #region Action Methods
        public ActionResult GetFile(Int64 id, Int64 mailAccountId = 0)
        {
            ISession session = NHibernateManager.OpenSession();
            try
            {
                User sessionUser = (User)Session[AccountController.USER_NAME];
                if (sessionUser == null)
                    throw new GlimpseException("No se encontró el usuario.");

                Extra extra = Extra.FindByID(id, session);

                if (extra != null && extra.BelongsToUser(sessionUser))
                    return File(extra.Entity.Data, extra.Entity.FileType, extra.Entity.Name);
                else
                    return new HttpNotFoundResult();
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
        [AjaxOnly]
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
        [AjaxOnly]
        public ActionResult GetMailsByDate(Int64 initial, Int64 final, Int64 mailAccountId = 0)
        {
            DateTime initialDate = AsyncMailsController.ConvertFromJS(initial);
            DateTime finalDate = AsyncMailsController.ConvertFromJS(final);

            ISession session = NHibernateManager.OpenSession();
            try
            {
                MailCollection accountMails;
                List<Object> mailsToReturn = new List<object>();
                User currentUser = (User)Session[AccountController.USER_NAME];
                foreach (MailAccount mailAccount in currentUser.GetAccounts())
                {
                    accountMails = new MailCollection(mailAccount.GetMailsByDate(initialDate, finalDate, session));
                    mailsToReturn.AddRange(this.PrepareHomeMails(accountMails));
                }

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
        [AjaxOnly]
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

                return Json(new { success = true, mails = mailsToReturn }, JsonRequestBehavior.AllowGet);
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
        [AjaxOnly]
        public ActionResult GetUsedDirections()
        {
            ISession session = NHibernateManager.OpenSession();
            try
            {
                User currentUser = (User)Session[AccountController.USER_NAME];
                if (currentUser == null)
                    throw new GlimpseException("No se encontró el usuario.");
                IList<String> directions = new List<string>();
                String accountIds = "";

                foreach (MailAccount mailAccount in currentUser.GetAccounts())
                    accountIds += mailAccount.Entity.Id + ',';

                accountIds = accountIds.Trim(',');

                directions = session.CreateQuery(
                                        "SELECT distinct(A.MailAddress) " +
                                        "FROM ADDRESS A " +
                                        "INNER JOIN MAIL M ON M.fromid = A.id " +
                                        "WHERE M.mailaccountid IN (" + accountIds + ")")
                                        .List<String>();

                return Json(new { directions = directions }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exc)
            {
                Log.LogException(exc);
                return Json(new { directions = new String[0] }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                session.Close();
            }
        }

        #region HttpPost
        [HttpPost]
        [AjaxOnly]
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
        [AjaxOnly]
        public ActionResult UntrashMail(Int64 id, Int64 mailAccountId = 0)
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                MailAccount mailAccount = this.GetMailAccount(mailAccountId);
                Mail mail = new Mail(id, session);
                if (mail == null)
                    throw new GlimpseException("Mail inexistente: " + id.ToString() + ".");
                mailAccount.UntrashMail(mail, session); //IMAP y BD
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
        [AjaxOnly]
        public ActionResult SendEmail(MailSentViewModel sendInfo, Int64 mailAccountId = 0)
        {
            try
            {
                if (sendInfo.ToAddress == null)
                    throw new InvalidRecipientsException("No se escribieron destinatarios");

                MailAccount mailAccount = this.GetMailAccount(mailAccountId);
                List<ExtraFile> uploadedFiles = new List<ExtraFile>();
                if (sendInfo.AttachmentsIds != null && sendInfo.AttachmentsIds.Count > 0 && Session[AsyncMailsController.FILES] != null &&
                    ((List<ExtraFile>)Session[AsyncMailsController.FILES]).Count > 0)
                {
                    foreach (String attachmentId in sendInfo.AttachmentsIds)
                    {
                        ExtraFile file = ((List<ExtraFile>)Session[AsyncMailsController.FILES]).First(x => x.Id == attachmentId);
                        try
                        {
                            using (FileStream fileStream = System.IO.File.Open(file.Path, FileMode.Open, FileAccess.Read))
                                fileStream.Read(file.Content, 0, (int)file.Size);
                            uploadedFiles.Add(file);
                        }
                        catch (Exception exc)
                        {
                            if (!(exc is UnauthorizedAccessException) && !(exc is DirectoryNotFoundException))
                                throw;
                        }
                    }
                }

                mailAccount.SendMail(sendInfo.ToAddress, sendInfo.Body, sendInfo.Subject, uploadedFiles);

                Session.Remove(AsyncMailsController.FILES);
                this.ClearTempDirectory();

                return Json(new { success = true, address = sendInfo.ToAddress }, JsonRequestBehavior.AllowGet);
            }
            catch (InvalidRecipientsException exc)
            {
                return Json(new
                {
                    success = false,
                    message = "No se pudo enviar el email porque alguna de las direcciones de los destinatarios es inexistente. Por favor corríjalos e intentelo de nuevo.",
                    address = sendInfo.ToAddress
                }, JsonRequestBehavior.AllowGet);
            }
            catch (SmtpException exc)
            {
                Log.LogException(exc, "Parametros del mail a enviar: subjectMail(" + sendInfo.Subject + "), addressMail(" + sendInfo.ToAddress + ").");
                return Json(new { success = false, message = "Actualmente tenemos problemas para enviar el email, por favor intételo de nuevo más tarde", address = sendInfo.ToAddress }, JsonRequestBehavior.AllowGet);
            }
            catch (GlimpseException exc)
            {
                return Json(new { success = false, message = exc.GlimpseMessage, address = exc.GlimpseMessage }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exc)
            {
                Session.Remove(AsyncMailsController.FILES);
                Log.LogException(exc, "Parametros de la llamada: subjectMail(" + sendInfo.Subject + "), addressMail(" + sendInfo.ToAddress + ").");
                return Json(new { success = false, message = "Actualmente tenemos problemas para enviar el email, por favor inténtelo de nuevo más tarde", address = sendInfo.ToAddress }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        [AjaxOnly]
        public ActionResult AddLabel(String labelName, Int64 mailId, Int64 mailAccountId = 0)
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                MailAccount mailAccount = this.GetMailAccount(mailAccountId);
                Label theLabel = this.GetAccountLabel(labelName, mailAccount, session);
                if (theLabel == null)
                    return Json(new { success = false, message = "No se ha podido encontrar la etiqueta con el nombre:" + labelName + "." }, JsonRequestBehavior.AllowGet);
                Mail theMail = new Mail(mailId, session);

                if (theMail.Entity.MailAccountEntity.Id != theLabel.Entity.MailAccountEntity.Id) //si el mail no es del mismo MailAccount de la etiqueta
                    this.CreateLabel(labelName, theMail.Entity.MailAccountEntity.Id); //DB e IMAP

                theMail.AddLabel(theLabel, session); //DB
                tran.Commit();
                mailAccount.AddLabelFolder(theMail, theLabel); //IMAP

                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exc)
            {
                tran.Rollback();
                Log.LogException(exc, "Parametros de la llamada: label(" + labelName + "), mailId(" +
                                       mailId.ToString() + "), mailAccountId(" + mailAccountId + ").");
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                session.Close();
            }
        }
        [HttpPost]
        [AjaxOnly]
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
                tran.Commit();
                currentMailAccount.RemoveMailLabel(labelName, mail.Entity.Gm_mid); //IMAP

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
        [AjaxOnly]
        public ActionResult RenameLabel(String oldLabelName, String newLabelName)
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                User sessionUser = (User)Session[AccountController.USER_NAME];
                if (sessionUser == null)
                    throw new GlimpseException("No se encontró el usuario.");

                foreach (MailAccount userMailAccount in sessionUser.GetAccounts())
                {
                    IList<LabelEntity> labels = Label.FindByAccount(userMailAccount.Entity, session);
                    labels = labels.Where(x => x.Name.Contains(oldLabelName) && x.SystemName == null).ToList();
                    foreach (LabelEntity label in labels)
                        new Label(label).Rename(oldLabelName, newLabelName, session); //BD
                    if (labels.Count > 0)
                        userMailAccount.RenameLabel(oldLabelName, newLabelName); //IMAP
                }
                tran.Commit();

                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exc)
            {
                tran.Rollback();
                Log.LogException(exc, "Parametros del metodo: oldLabelName(" + oldLabelName +
                                      "), newLabelName(" + newLabelName + ").");
                return Json(new { success = false, message = "Error al renombrar label." }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                session.Close();
            }
        }
        [HttpPost]
        [AjaxOnly]
        public ActionResult RecolorLabel(String labelName, String color)
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                User sessionUser = (User)Session[AccountController.USER_NAME];
                if (sessionUser == null)
                    throw new GlimpseException("No se encontró el usuario.");

                foreach (MailAccount currentMailAccount in sessionUser.GetAccounts())
                {
                    IList<LabelEntity> labels = Label.FindByAccount(currentMailAccount.Entity, session);
                    if (labels.Any(x => x.Name == labelName))
                    {
                        LabelEntity labelToRecolor = labels.Single(x => x.Name == labelName);
                        labelToRecolor.Color = color;
                        session.SaveOrUpdate(labelToRecolor);
                    }
                }
                tran.Commit();

                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exc)
            {
                tran.Rollback();
                Log.LogException(exc, "Parametros del metodo: labelName(" + labelName + "), color(" + color + ").");
                return Json(new { success = false, message = "Error al cambiar color." }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                session.Close();
            }
        }
        [HttpPost]
        [AjaxOnly]
        public ActionResult CreateLabel(String labelName, Int64 mailAccountId = 0)
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                User sessionUser = (User)Session[AccountController.USER_NAME];
                if (sessionUser == null)
                    throw new GlimpseException("No se encontró el usuario.");

                MailAccount labelAccount;

                if (mailAccountId != 0)
                    labelAccount = this.GetMailAccount(mailAccountId);
                else
                    labelAccount = this.GetMainMailAccount();

                Label existingLabel = Label.FindByName(labelAccount, labelName, session);
                if (existingLabel != null)
                    return Json(new { success = false, message = "Ya existe una etiqueta con ese nombre." }, JsonRequestBehavior.AllowGet);
                Label newLabel = new Label(new LabelEntity());
                newLabel.Entity.SystemName = null;
                newLabel.Entity.Name = labelName;
                newLabel.Entity.Active = true;
                newLabel.Entity.MailAccountEntity = labelAccount.Entity;
                Label.ColorLabel(newLabel.Entity, labelAccount, sessionUser, session);
                newLabel.SaveOrUpdate(session); //BD
                tran.Commit();
                labelAccount.CreateLabel(labelName); //IMAP

                return Json(new { success = true, color = newLabel.Entity.Color }, JsonRequestBehavior.AllowGet);
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
        [AjaxOnly]
        public ActionResult DeleteLabel(String labelName, Int64 mailAccountId = 0)
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                User sessionUser = (User)Session[AccountController.USER_NAME];
                if (sessionUser == null)
                    throw new GlimpseException("No se encontró el usuario.");

                foreach (MailAccount currentMailAccount in sessionUser.GetAccounts())
                {
                    Label labelToDelete = Label.FindByName(currentMailAccount, labelName, session);
                    if (labelToDelete != null)
                    {
                        labelToDelete.Delete(session); //BD
                        currentMailAccount.DeleteLabel(labelName); //IMAP
                    }
                }
                tran.Commit();

                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
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
        [AjaxOnly]
        public ActionResult ChangeImportance(Int64 mailId, Boolean isIncrease, Int64 mailAccountId = 0)
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                MailAccount currentMailAccount = this.GetMailAccount(mailAccountId);
                Mail theMail = new Mail(mailId, session);

                if (isIncrease)
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
        [AjaxOnly]
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
        [AjaxOnly]
        public ActionResult ArchiveMail(Int64 mailId, Int64 mailAccountId = 0)
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                MailAccount currentMailAccount = this.GetMailAccount(mailAccountId);
                Mail mail = new Mail(mailId, session);
                mail.Archive(session); //DB
                tran.Commit();
                currentMailAccount.ArchiveMail(mail); //IMAP

                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
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
        [HttpPost]
        [AjaxOnly]
        public ActionResult UnarchiveMail(Int64 mailId, Int64 mailAccountId = 0)
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            try
            {
                MailAccount currentMailAccount = this.GetMailAccount(mailAccountId);
                Mail mail = new Mail(mailId, session);
                Label inboxLabel = Label.FindBySystemName(currentMailAccount, "Inbox", session);
                mail.Unarchive(inboxLabel, session); //DB
                tran.Commit();
                currentMailAccount.UnarchiveMail(mail); //IMAP

                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exc)
            {
                tran.Rollback();
                Log.LogException(exc, "Parametros del metodo: mailId(" + mailId.ToString() +
                                      "), mailAccountId(" + mailAccountId.ToString() + ").");
                return Json(new { success = false, message = "Error al desarchivar mail." }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                session.Close();
            }
        }
        [HttpPost]
        [AjaxOnly]
        public void SynchronizeAccount()
        {
            try
            {
                User sessionUser = (User)Session[AccountController.USER_NAME];
                if (sessionUser == null)
                    throw new GlimpseException("No se encontró el usuario.");
                foreach (MailAccount userMailAccount in sessionUser.GetAccounts())
                    Task.Factory.StartNew(() => MailsTasksHandler.StartSynchronization(userMailAccount.Entity.Address, false));
            }
            catch (Exception exc)
            {
                Log.LogException(exc);
            }
        }
        [HttpPost]
        public ActionResult UploadFile(HttpPostedFileBase file)
        {
            if (Extra.IsValidFile(file))
            {
                if (Session[AsyncMailsController.FILES] == null)
                    Session[AsyncMailsController.FILES] = new List<ExtraFile>();

                if (((List<ExtraFile>)Session[AsyncMailsController.FILES]).Any(x => x.Size == file.ContentLength && x.Name == file.FileName))
                    return Json(new { success = false, message = "El archivo seleccionado ya se encuentra adjuntado." }, JsonRequestBehavior.AllowGet);

                ExtraFile attachment = new ExtraFile();
                attachment.Name = file.FileName;
                attachment.Size = file.ContentLength;
                attachment.Type = file.ContentType;
                attachment.Path = Extra.SaveToFS(file); //el contenido va a disco
                attachment.Id = Path.GetFileName(attachment.Path).Substring(0, 16);
                attachment.Content = new byte[file.ContentLength];
                ((List<ExtraFile>)Session[AsyncMailsController.FILES]).Add(attachment);
                return Json(new { success = true, id = attachment.Id }, JsonRequestBehavior.AllowGet);
            }
            else
                return Json(new { success = false, message = "El archivo seleccionado es mayor a 5mb o es potencialmente danino." }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #endregion
        #region Private Helpers
        private static DateTime ConvertFromJS(Int64 JSDate)
        {
            DateTime date = new DateTime(1970, 1, 1) + new TimeSpan(JSDate * 10000);
            return date;
        }
        private void ClearTempDirectory()
        {
            DirectoryInfo tempDir = new DirectoryInfo(this.HttpContext.Server.MapPath("~") + "temp");
            foreach (FileInfo file in tempDir.GetFiles().Where(x => x.Name != "empty" && x.CreationTime < DateTime.Now.AddHours(-1)))
                file.Delete();
        }
        private List<Object> PrepareHomeMails(MailCollection mails)
        {
            List<Object> preparedMails = new List<Object>();

            foreach (MailEntity mail in mails)
            {
                DateTime mailDate = DateTimeHelper.ChangeToUtc(mail.Date);
                Int64 currentAge = DateTime.Now.Ticks - mailDate.ToLocalTime().Ticks;
                List<Object> currentLabels = PrepareLabels(mail.Labels);
                Object anEmail = new
                {
                    id = mail.Id,
                    subject = mail.Subject,
                    date = mailDate,
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
                    mailaccount = mail.MailAccountEntity.Id,
                    has_attachments = mail.HasExtras
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
            if (id != 0)
                return mailAccounts.Where<MailAccount>(x => x.Entity.Id == id).Single<MailAccount>();
            else
                return mailAccounts[0]; //harcodeado para que funcione hasta que la vista mande los ids
        }
        private MailAccount GetMainMailAccount()
        {
            User currentUser = (User)Session[AccountController.USER_NAME];
            IList<MailAccount> currentMailAccounts = currentUser.GetAccounts();
            if (currentMailAccounts.Count == 1)
                return currentMailAccounts[0];
            else
                return currentMailAccounts.First(x => x.Entity.IsMainAccount == true);
        }
        private Label GetAccountLabel(String labelName, MailAccount possibleLabelAccount, ISession session)
        {
            User user = (User)Session[AccountController.USER_NAME];
            IList<MailAccount> mailAccounts = user.GetAccounts();

            //busco en el mailAccount del mismo mail primero
            IList<LabelEntity> accountLabels = Label.FindByAccount(possibleLabelAccount.Entity, session);
            if (accountLabels.Any(x => x.Name == labelName))
                return new Label(accountLabels.Single(x => x.Name == labelName));

            //si no esta, busco en el resto
            foreach (MailAccount mailAccount in mailAccounts.Where(x => x.Entity != possibleLabelAccount.Entity))
            {
                accountLabels = Label.FindByAccount(mailAccount.Entity, session);
                if (accountLabels.Any(x => x.Name == labelName))
                    return new Label(accountLabels.Single(x => x.Name == labelName));
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
                body = body.Replace("cid:" + embeddedExtra.EmbObjectContentId, Url.Action("GetFile", "AsyncMails", new { id = embeddedExtra.Id }, this.Request.Url.Scheme));
            }
        }
        #endregion
    }
}
