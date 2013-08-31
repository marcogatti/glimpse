using ActiveUp.Net.Mail;
using Glimpse.DataAccessLayer;
using Glimpse.DataAccessLayer.Entities;
using Glimpse.Exceptions;
using Glimpse.Helpers;
using Glimpse.MailInterfaces;
using Glimpse.Models;
using Newtonsoft.Json;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Glimpse.Controllers
{
    //[Authorize]
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

        private List<Object> FetchMails(int amountOfEmails, ISession session)
        {
            MailAccount mailAccount = (MailAccount)Session[AccountController.MAIL_INTERFACE];

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
            String[] labels = new String[4] { "google", "facultad", "trabajo", "boludeces" };

            foreach (Mail mail in mails)
            {
                Random ran = new Random();
                String selected = labels[ran.Next(labels.Length)];

                Int64 currentAge = DateTime.Now.Ticks - mail.Entity.Date.Ticks;

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
                    body = mail.Entity.Body,
                    tid = mail.Entity.Gm_tid,
                    seen = mail.Entity.Seen,
                    flagged = mail.Entity.Flagged,
                    label = selected
                };

                preparedMails.Add(anEmail);
            }

            return preparedMails;
        }

    }
}
