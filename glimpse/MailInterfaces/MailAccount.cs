using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ActiveUp.Net.Mail;

namespace Glimpse.MailInterfaces
{
    public class MailAccount
    {
        private Fetcher fetcher { get; set; }

        public String Username { get; set; }
        public String Password { get; set; }

        public MailAccount(String username, String password)
        {
            this.Username = username;
            this.Password = password;
            this.fetcher = new Fetcher(username, password);            
        }

        public MessageCollection GetInboxMessages()
        {
            return this.fetcher.GetInboxMails();
        }

    }
}