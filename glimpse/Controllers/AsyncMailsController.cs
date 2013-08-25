using ActiveUp.Net.Mail;
using Glimpse.DataAccessLayer.Entities;
using Glimpse.Exceptions;
using Glimpse.Helpers;
using Glimpse.MailInterfaces;
using Glimpse.Models;
using Newtonsoft.Json;
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
        public ActionResult InboxMails(int amountOfEmails = 0)
        {
            try
            {
                IList<Object> mailsToSend = this.FetchMails(amountOfEmails);
                return Json(new { success = true, mails = mailsToSend }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exp)
            {
                //Log exception
                return Json(new { success = false, message = "Error al obtener los mails" }, JsonRequestBehavior.AllowGet);
            }
        }

        private List<Object> FetchMails(int amountOfEmails)
        {
            MailAccount mailAccount = (MailAccount)Session[AccountController.MAIL_INTERFACE];

            if (mailAccount != null)
            {
                MailManager manager = new MailManager(mailAccount);

                List<Mail> mails = manager.FetchFromMailbox("INBOX");

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
                    flagged = mail.Entity.Flagged
                };

                preparedMails.Add(anEmail);
            }

            return preparedMails;
        }

    }
}
