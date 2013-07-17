using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ActiveUp.Net.Mail;

namespace glimpse.MailInterfaces
{
    public class MailAccount
    {
        public String Username { get; set; }
        public String Password { get; set; }

        private Fetcher Fetcher { get; set; }           

        public MailAccount(String username, String password)
        {
            this.Username = username;
            this.Password = password;
            this.Fetcher = new Fetcher(username, password);            
        }

        public MessageCollection getInboxMessages()
        {
            return this.Fetcher.getInboxMails();
        }

    }
}