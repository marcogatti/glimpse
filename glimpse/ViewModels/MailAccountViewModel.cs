using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.ViewModels
{
    public class MailAccountViewModel
    {
        public String Address { get; set; }
        public String Password { get; set; }
        public String ConfirmationPassword { get; set; }
        public Boolean IsMainAccount { get; set; }
    }
}