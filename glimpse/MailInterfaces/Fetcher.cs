using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ActiveUp.Net.Imap4;
using ActiveUp.Net.Mail;

namespace glimpse.MailInterfaces
{
    public class Fetcher
    {
        private Imap4Client receiver;

        public Fetcher(String username, String password)
        {
            this.receiver = new Connector().Login(username, password);
        }

        public MessageCollection GetInboxMails()
        {
            return this.GetMails("INBOX", "ALL");
        }

        private MessageCollection GetMails(string mailBox, string searchPhrase)
        {
            Mailbox mails = this.receiver.SelectMailbox(mailBox);
            MessageCollection messages = mails.SearchParse(searchPhrase);
            return messages;
        }
    }
}