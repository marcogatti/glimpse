using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Glimpse.ViewModels
{
    public class MailSentViewModel
    {
        [AllowHtml]
        public String ToAddress { get; set; }

        [AllowHtml]
        public String Body { get; set; }

        public String Subject { get; set; }

        public HttpPostedFile Attachment { get; set; }

        public MailSentViewModel() { }
    }
}