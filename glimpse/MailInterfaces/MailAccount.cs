using System;
using System.Collections.Generic;
using System.Web;
using ActiveUp.Net.Mail;

namespace Glimpse.MailInterfaces
{
    public class MailAccount
    {
        private Fetcher myFetcher { get; set; }
        private Sender mySender { get; set; }

        public String Username { get; set; }
        public String Password { get; set; }

        public MailAccount(String username, String password)
        {
            this.Username = username;
            this.Password = password;
            this.myFetcher = new Fetcher(username, password);
            this.mySender = new Sender();
        }

        public MessageCollection GetInboxMessages()
        {
            return this.myFetcher.GetInboxMails();
        }

    }
}