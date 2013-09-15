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
            try
            {
                ISession session = NHibernateManager.OpenSession();

                IList<Object> mailsToSend = this.FetchMails(id, session);
                JsonResult result = Json(new { success = true, mails = mailsToSend }, JsonRequestBehavior.AllowGet);

                session.Flush();
                session.Close();

                return result;
            }
            catch (Exception)
            {
                //Log exception
                return Json(new { success = false, message = "Error al obtener los mails" }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetMailBody(Int64 id = 0)
        {
            try
            {
                ISession session = NHibernateManager.OpenSession();

                MailAccount currentMailAccount = GetCurrentMailAccount();

                Mail mail = currentMailAccount.ReadMail(id, session);

                JsonResult result = Json(new { success = true, body = mail.Entity.Body }, JsonRequestBehavior.AllowGet);

                session.Flush();
                session.Close();

                return result;
            }
            catch (Exception e)
            {
                //Log exception
                return Json(new { success = false, message = "Error al obtener el cuerpo del mail." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult sendEmail(MailSentViewModel sendInfo)
        {
            try
            {
                MailAccount mailAccount = GetCurrentMailAccount();
                mailAccount.sendMail(sendInfo.ToAddress, sendInfo.Body, sendInfo.Subject);
            }
            catch (Exception e)
            {
                return Json(new { success = false, address = sendInfo.ToAddress }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, address = sendInfo.ToAddress }, JsonRequestBehavior.AllowGet);
        }


        private List<Object> FetchMails(int amountOfEmails, ISession session)
        {
            MailAccount mailAccount = GetCurrentMailAccount();

            if (mailAccount != null)
            {
                MailManager manager = new MailManager(mailAccount);

                List<MailEntity> mails = manager.GetMailsFrom("INBOX", amountOfEmails, session);

                return this.PrepareToSend(mails);
            }
            else
            {
                throw new GlimpseException("El MailAccount no estaba inicializado.");
            }
        }

        private List<Object> PrepareToSend(List<MailEntity> mails)
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

        private List<object> PrepareLabels(IList<LabelEntity> labels)
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
