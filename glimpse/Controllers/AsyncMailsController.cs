﻿using ActiveUp.Net.Mail;
using Glimpse.DataAccessLayer;
using Glimpse.DataAccessLayer.Entities;
using Glimpse.Models;
using Glimpse.ViewModels;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Web.Mvc;

namespace Glimpse.Controllers
{
    [Authorize]
    public class AsyncMailsController : Controller
    {
        #region Action Methods
        //public ActionResult InboxMails(int id = 0)
        //{
        //    ISession session = NHibernateManager.OpenSession();

        //    try
        //    {
        //        IList<Object> mailsToSend = this.FetchMails(id, session);
        //        JsonResult result = Json(new { success = true, mails = mailsToSend }, JsonRequestBehavior.AllowGet);
        //        return result;
        //    }
        //    catch (Exception exc)
        //    {
        //        Log.LogException(exc, "Error generico InboxMails. Parametros del mail: idMail(" + id.ToString() + ").");

        //        return Json(new { success = false, message = "Error al obtener los mails" }, JsonRequestBehavior.AllowGet);
        //    }
        //    finally
        //    {
        //        session.Flush();
        //        session.Close();
        //    }
        //}
        public ActionResult GetMailBody(Int64 id = 0)
        {
            ISession session = NHibernateManager.OpenSession();
            try
            {
                MailAccount currentMailAccount = this.GetCurrentMailAccount();
                String body = currentMailAccount.ReadMail(id, session);

                JsonResult result = Json(new { success = true, body = body }, JsonRequestBehavior.AllowGet);
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
        public ActionResult GetMailsByDate(Int64 initial, Int64 final)
        {
            DateTime initialDate = AsyncMailsController.ConvertFromJS(initial);
            DateTime finalDate = AsyncMailsController.ConvertFromJS(final);

            ISession session = NHibernateManager.OpenSession();
            try
            {
                MailCollection mails;
                List<Object> mailsToReturn = new List<object>();
                MailAccount currentMailAccount = this.GetCurrentMailAccount();
                mails = new MailCollection(currentMailAccount.GetMailsByDate(initialDate, finalDate, session));
                mailsToReturn = this.PrepareToSend(mails);

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
        public ActionResult GetMailsByAmount(Int32 amountOfMails)
        {
            ISession session = NHibernateManager.OpenSession();
            try
            {
                MailCollection mails;
                List<Object> mailsToReturn = new List<object>();
                MailAccount currentMailAccount = this.GetCurrentMailAccount();
                mails = currentMailAccount.GetMailsByAmount(amountOfMails, session);
                mailsToReturn = this.PrepareToSend(mails);

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
        public ActionResult RemoveLabel(String label, Int64 mailId)
        {
            ISession session = NHibernateManager.OpenSession();
            try
            {
                MailAccount currentMailAccount = this.GetCurrentMailAccount();
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
        public ActionResult sendEmail(MailSentViewModel sendInfo)
        {
            try
            {
                MailAccount mailAccount = this.GetCurrentMailAccount();
                mailAccount.SendMail(sendInfo.ToAddress, sendInfo.Body, sendInfo.Subject);
                return Json(new { success = true, address = sendInfo.ToAddress }, JsonRequestBehavior.AllowGet);
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
        public ActionResult AddLabel(String labelName, Int64 mailId)
        {
            bool success;
            try
            {
                MailAccount mailAccount = this.GetCurrentMailAccount();

                using (ISession session = NHibernateManager.OpenSession())
                {
                    ITransaction tran = session.BeginTransaction();

                    Mail theMail = new Mail(mailId, session);
                    Label theLabel = Label.FindByName(mailAccount, labelName, session);
                    theMail.AddLabel(theLabel, session); //DB
                    mailAccount.AddLabelIMAP(theMail, theLabel); //IMAP

                    tran.Commit();
                    session.Flush();
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
        #endregion
        #region Private Helpers
        private static DateTime ConvertFromJS(Int64 JSDate)
        {
            DateTime date = new DateTime(1970, 1, 1) + new TimeSpan(JSDate * 10000);
            return date;
        }
        private List<Object> FetchMails(int amountOfEmails, ISession session)
        {
            MailAccount mailAccount = GetCurrentMailAccount();

            MailManager manager = new MailManager(mailAccount);

            MailCollection mails = manager.GetMailsFrom("INBOX", amountOfEmails, session);

            return this.PrepareToSend(mails);
        }
        private List<Object> PrepareToSend(MailCollection mails)
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
                    labels = currentLabels
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
        private MailAccount GetCurrentMailAccount()
        {
            MailAccount mailAccount = (MailAccount)Session[HomeController.MAIL_ACCOUNTS];

            if (!mailAccount.isConnected())
            {
                try
                {
                    mailAccount.connectFull();
                }
                catch (SocketException exc)
                {
                    Log.LogException(exc, "Error al conectar con IMAP");
                }
            }
            return mailAccount;
        }
        #endregion
    }
}
