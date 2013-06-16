using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ActiveUp.Net.Mail;
using System.Collections;

namespace glimpse.Models
{

    public class Mail
    {
        public String Subject { get; set; }
        public String Sender { get; set; }
        public String Recipients { get; set; }
        public String SentDate { get; set; }
        public String Body { get; set; }


        public Mail(String subject, String sender, String recipients, String sentDate, String body)
        {
            this.Subject = subject;
            this.Sender = sender;
            this.Recipients = recipients;
            this.SentDate = sentDate;
            this.Body = body;
        }

    }

    public class MailRepository
    {
        private Imap4Client _client = null;

        protected Imap4Client Client
        {
            get
            {
                if (_client == null)
                    _client = new Imap4Client();
                return _client;
            }
        }


        public MailRepository(string mailServer, int port, bool ssl, string login, string password)
        {
            if (ssl)
                Client.ConnectSsl(mailServer, port);
            else
                Client.Connect(mailServer, port);

            Client.Login(login, password);
        }

        public IEnumerable<Mail> GetAllMails(string mailBox)
        {
            return GetMails(mailBox, "ALL");
        }

        public IEnumerable<Mail> GetUnreadMails(string mailBox)
        {
            return GetMails(mailBox, "UNSEEN");
        }


        private IEnumerable<Mail> GetMails(string mailBox, string searchPhrase)
        {
            List<Mail> mails = new List<Mail>();

            Mailbox mailbox = Client.SelectMailbox(mailBox);
            foreach (Message message in mailbox.SearchParse(searchPhrase))
            {

                mails.Add(new Mail(message.Subject,
                                   message.From.Name + " - " + message.From.Email,
                                   message.To.First().Name + " - " + message.To.First().Email,
                                   message.ReceivedDate.ToString(),
                                   message.BodyText.Text));

            }

            return mails;
        }
    }

}