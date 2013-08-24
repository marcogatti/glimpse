using ActiveUp.Net.Mail;
using Glimpse.DataAccessLayer.Entities;
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

            AccountInterface accountInterface = (AccountInterface)Session[AccountController.MAIL_INTERFACE];

            if (accountInterface != null)
            {
                MailAccount mailAccount = MailAccount.FindByAddress(new CookieHelper().getMailAddressFromCookie());

                MailManager manager = new MailManager(accountInterface, mailAccount);

                IList<MailEntity> mails = manager.FetchFromMailbox("INBOX");

                JsonResult response = Json(new { success = true, mails = PrepareToSend(mails) }, JsonRequestBehavior.AllowGet);
                return response;
            }
            else
            {
                return Json(new { success = false, message = "Uuups, something went wrong!" }, JsonRequestBehavior.AllowGet);
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
