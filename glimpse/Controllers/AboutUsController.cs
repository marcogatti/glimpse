using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Glimpse.Controllers
{
    public class AboutUsController : Controller
    {
        //
        // GET: /AboutUs/
        [AllowAnonymous]
        public ActionResult Index()
        {
            return View();
        }

    }
}
