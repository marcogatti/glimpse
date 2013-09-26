﻿using ActiveUp.Net.Mail;
using Glimpse.DataAccessLayer;
using Glimpse.DataAccessLayer.Entities;
using Glimpse.Exceptions;
using Glimpse.Helpers;
using Glimpse.MailInterfaces;
using Glimpse.Models;
using Glimpse.ViewModels;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Glimpse.Controllers
{
    [Authorize]
    public class AsyncMailsController : Controller
    {
        //
        // GET: /AsyncMails/InboxMails
        public ActionResult InboxMails(int id = 0)
        {
            ISession session = NHibernateManager.OpenSession();

            try
            {
                IList<Object> mailsToSend = this.FetchMails(id, session);
                JsonResult result = Json(new { success = true, mails = mailsToSend }, JsonRequestBehavior.AllowGet);
                return result;
            }
            catch (Exception exc)
            {
                Log.LogException(exc, "Error generico InboxMails. Parametros del mail: idMail(" + id.ToString() + ").");

                return Json(new { success = false, message = "Error al obtener los mails" }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                session.Flush();
                session.Close();
            }
        }

        public ActionResult GetMailBody(Int64 id = 0)
        {
            ISession session = NHibernateManager.OpenSession();

            try
            {
                MailAccount currentMailAccount = GetCurrentMailAccount();

                Mail mail = currentMailAccount.ReadMail(id, session);

                JsonResult result = Json(new { success = true, body = mail.Entity.Body }, JsonRequestBehavior.AllowGet);

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

        [HttpPost]
        public ActionResult sendEmail(MailSentViewModel sendInfo)
        {
            try
            {
                MailAccount mailAccount = GetCurrentMailAccount();
                mailAccount.SendMail(sendInfo.ToAddress, sendInfo.Body, sendInfo.Subject);
            }
            catch (SmtpException exc)
            {
                Log.LogException(exc, "Error SMTP sendEmail. Parametros del mail: subjectMail(" + sendInfo.Subject + "), addressMail(" + sendInfo.ToAddress + ").");

                return Json(new { success = false, address = sendInfo.ToAddress }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exc)
            {
                Log.LogException(exc, "Error generico sendEmail. Parametros del mail: subjectMail(" + sendInfo.Subject + "), addressMail(" + sendInfo.ToAddress + ").");

                return Json(new { success = false, address = sendInfo.ToAddress }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, address = sendInfo.ToAddress }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetMailsByDate(Int64 initial, Int64 final)
        {
            DateTime initialDate = ConvertFromJS(initial);
            DateTime finalDate = ConvertFromJS(final);

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
                Log.LogException(exc, "Error generico GetMailBody. Parametros del mail: initialDate("
                    + initialDate.ToString() + "), finalDate(" + finalDate.ToString() + ").");

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
                Log.LogException(exc, "Error generico GetMailsByAmount. Parametros del mail: amountOfMails("
                    + amountOfMails + ").");

                return Json(new { success = false, message = "Error al obtener los mails." }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                session.Close();
            }
        }

        public ActionResult RemoveLabel(String label, UInt64 gmID)
        {
            ISession session = NHibernateManager.OpenSession();

            try
            {
                MailAccount currentMailAccount = this.GetCurrentMailAccount();
                currentMailAccount.RemoveMailLabel(label, gmID); //IMAP
                Mail mail = new Mail(gmID, currentMailAccount, session);
                mail.RemoveLabel(label, session);

                JsonResult result = Json(new { success = true }, JsonRequestBehavior.AllowGet);

                return result;
            }
            catch (Exception exc)
            {
                Log.LogException(exc, "Error generico RemoveLabel. Parametros del mail: label("
                    + label + "), gmID(" + gmID.ToString() + ").");

                return Json(new { success = false, message = "Error al obtener el cuerpo del mail." }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                session.Close();
            }
        }

        private static DateTime ConvertFromJS(Int64 JSDate)
        {
            DateTime date = new DateTime(1970, 1, 1) + new TimeSpan(JSDate * 10000);
            return date;
        }
        private List<Object> FetchMails(int amountOfEmails, ISession session)
        {
            MailAccount mailAccount = GetCurrentMailAccount();

            if (mailAccount != null)
            {
                MailManager manager = new MailManager(mailAccount);

                MailCollection mails = manager.GetMailsFrom("INBOX", amountOfEmails, session);

                return this.PrepareToSend(mails);
            }
            else
            {
                throw new GlimpseException("El MailAccount no estaba inicializado.");
            }
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
                    to = mail.ToAddr,
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
            MailAccount mailAccount = (MailAccount)Session[AccountController.MAIL_INTERFACE];

            if (!mailAccount.isConnected())
                mailAccount.connectFull();

            return mailAccount;
        }

    }
}
