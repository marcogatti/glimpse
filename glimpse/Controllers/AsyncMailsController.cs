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
                MailAccount mailAccount = GetCurrentMailAccount();
                MailEntity mail = session.CreateCriteria<MailEntity>()
                                     .Add(Restrictions.Eq("MailAccountEntity", mailAccount.Entity))
                                     .Add(Restrictions.Eq("Id", id))
                                     .UniqueResult<MailEntity>();

                JsonResult result = Json(new { success = true, body = mail.Body }, JsonRequestBehavior.AllowGet);

                session.Flush();
                session.Close();

                return result;
            }
            catch (Exception)
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
            catch (Exception)
            {
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
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
                    to = mail.Entity.ToAddr,
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
