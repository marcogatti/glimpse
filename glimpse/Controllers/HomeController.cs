using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Glimpse.Helpers;
using Glimpse.ViewModels;
using Glimpse.MailInterfaces;

namespace Glimpse.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        //
        // GET: /Home/
        public ActionResult Index()
        {
            UserViewModel user = new CookieHelper().getLoginCookie();
            ViewBag.Email = user.Email;
            ViewBag.Password= user.Password;

            MailAccount mailAccount = new MailAccount(user.Email, user.Password);
            ViewBag.InboxMessages = mailAccount.GetInboxMessages();

            return View();
        }

    }
}
