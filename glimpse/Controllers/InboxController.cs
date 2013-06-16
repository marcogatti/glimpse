using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using glimpse.Models;

namespace glimpse.Controllers
{

    // Los controladores que vamos creando atienden los pedidos del browser. El nombre del controlador (sin "Controller" al final)
    // se usa como segunda parte de la URL, por ejemplo en este caso es: http://localhost:xxxxxx/inbox
    public class InboxController : Controller
    {
        //
        // GET: /Inbox/

        public ActionResult Index()
        {
            return View();
        }


        // GET: /Inbox/Main/ 

        public ActionResult Main()
        {
            MailRepository rep = new MailRepository("imap.gmail.com", 993, true, "test.imap.505@gmail.com", "ytrewq123");

            return View(rep.GetAllMails("Inbox"));
        }

    }
}
