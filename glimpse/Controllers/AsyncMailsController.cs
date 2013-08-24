using ActiveUp.Net.Mail;
using Glimpse.MailInterfaces;
using Glimpse.Models;
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
        public ActionResult InboxMails(int amountOfEmails = 0)
        {

            MailAccount account = (MailAccount)Session[AccountController.MAIL_INTERFACE];

            if (account != null)
            {
                return Json(account.GetInboxMessages());
            }
            else
            {
                return Json(new { success = false, message = "Uuups, something went wrong!" });
            }

        }

    }
}
