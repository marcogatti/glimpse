using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using glimpse.Helpers;
using glimpse.ViewModels;

namespace glimpse.Controllers
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
            ViewBag.PasswordEncrypted = user.Password;
            return View();
        }

    }
}
