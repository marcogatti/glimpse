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
            catch (Exception)
            {
                return Json(new { success = false, message = "Error al obtener los mails" }, JsonRequestBehavior.AllowGet);
            }
        }

        private IList<Object> FetchMails(int amountOfEmails)
        {
            AccountInterface accountInterface = (AccountInterface)Session[AccountController.MAIL_INTERFACE];

            if (accountInterface != null)
            {
                MailAccount mailAccount = MailAccount.FindByAddress(new CookieHelper().getMailAddressFromCookie());

                MailManager manager = new MailManager(accountInterface, mailAccount);

                IList<MailEntity> mails = manager.FetchFromMailbox("INBOX");

                return PrepareToSend(mails);
            }
            else
            {
                throw new GlimpseException("El MailInterface no estaba inicializado.");
            }
        }

        private IList<Object> PrepareToSend(IList<MailEntity> mails)
        {
            IList<Object> preparedMails = new List<Object>();

            foreach (MailEntity mail in mails)
            {
                Object anEmail = new
                {
                    id = mail.Id,
                    subject = mail.Subject,
                    date = mail.Date,
                    from = new
                    {
                        address = mail.From.MailAddress,
                        name = mail.From.Name
                    },
                    to = mail.ToAddr,
                    cc = mail.CC,
                    bcc = mail.BCC,
                    body = mail.Body,
                    tid = mail.Gm_tid,
                    seen = mail.Seen,
                    flagged = mail.Flagged
                };

                preparedMails.Add(anEmail);
            }

            return preparedMails;
        }

    }
}
