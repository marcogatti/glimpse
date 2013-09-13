using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Glimpse.ViewModels
{
    public class MailSentViewModel
    {
        public String ToAddress { get; set; }

        [AllowHtml]
        public String Body { get; set; }

        public String Subject { get; set; }

        public MailSentViewModel() { }
    }
}