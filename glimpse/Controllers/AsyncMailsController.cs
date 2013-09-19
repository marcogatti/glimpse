using ActiveUp.Net.Mail;
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
                Log logger = new Log(new LogEntity(002, "Error generico InboxMails. Parametros del mail: idMail(" + id.ToString() + ").", exc.StackTrace));
                logger.Save();
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
                Log logger = new Log(new LogEntity(002, "Error generico GetMailBody. Parametros del mail: idMail(" + id.ToString() + ").", exc.StackTrace));
                logger.Save();
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
                Log logger = new Log(new LogEntity(002, "Error SMTP sendEmail. Parametros del mail: subjectMail(" + sendInfo.Subject + "), addressMail(" + sendInfo.ToAddress + ").", exc.StackTrace));
                logger.Save();
                return Json(new { success = false, address = sendInfo.ToAddress }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exc)
            {
                Log logger = new Log(new LogEntity(002, "Error generico sendEmail. Parametros del mail: subjectMail(" + sendInfo.Subject + "), addressMail(" + sendInfo.ToAddress + ").", exc.StackTrace));
                logger.Save();
                return Json(new { success = false, address = sendInfo.ToAddress }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, address = sendInfo.ToAddress }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetMailsByDate(DateTime initialDate, DateTime finalDate)
        {
            ISession session = NHibernateManager.OpenSession();

            try
            {
                List<Mail> mails = new List<Mail>();
                List<Object> mailsToReturn = new List<object>();
                
                MailAccount currentMailAccount = this.GetCurrentMailAccount();
                mails = currentMailAccount.GetMailsByDate(initialDate, finalDate, session);
                mailsToReturn = this.PrepareToSend(mails);

                JsonResult result = Json(new { success = true, mails = mailsToReturn }, JsonRequestBehavior.AllowGet);

                return result;
            }
            catch (Exception exc)
            {
                Log logger = new Log(new LogEntity(002, "Error generico GetMailBody. Parametros del mail: initialDate("
                    + initialDate.ToString() + "), finalDate(" + finalDate.ToString() + ").", exc.StackTrace));
                logger.Save();
                return Json(new { success = false, message = "Error al obtener el cuerpo del mail." }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                session.Close();
            }
        }

        private List<Object> FetchMails(int amountOfEmails, ISession session)
        {
            MailAccount mailAccount = GetCurrentMailAccount();

            if (mailAccount != null)
            {
                MailManager manager = new MailManager(mailAccount);

                List<Mail> mails = manager.FetchFromMailbox("INBOX", session, amountOfEmails);

                return this.PrepareToSend(mails);
            }
            else
            {
                throw new GlimpseException("El MailAccount no estaba inicializado.");
            }
        }
        private List<Object> PrepareToSend(List<Mail> mails)
        {
            List<Object> preparedMails = new List<Object>();

            foreach (Mail mail in mails)
            {

                Int64 currentAge = DateTime.Now.Ticks - mail.Entity.Date.Ticks;

                List<Object> currentLabels = PrepareLabels(mail.Entity.Labels);

                Object anEmail = new
                {
                    id = mail.Entity.Id,
                    subject = mail.Entity.Subject,
                    date = mail.Entity.Date,
                    age = currentAge,
                    from = new
                    {
                        address = mail.Entity.From.MailAddress,
                        name = mail.Entity.From.Name
                    },
                    to = mail.Entity.ToAddress,
                    cc = mail.Entity.CC,
                    bcc = mail.Entity.BCC,
                    bodypeek = mail.Entity.BodyPeek,
                    tid = mail.Entity.Gm_tid,
                    seen = mail.Entity.Seen,
                    flagged = mail.Entity.Flagged,
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
            return mailAccount;
        }

    }
}
