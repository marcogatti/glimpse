using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ActiveUp.Net.Mail;
using Glimpse.MailInterfaces;
using Glimpse.Exceptions.MailInterfacesExceptions;

namespace Glimpse.MailInterfaces
{
    public class Sender
    {
        private SmtpClient forwarder;
        private String newMailDataFrom;

        public Sender(String username, String password)
        {
            this.forwarder = new Connector().SmtpLogin(username, password);
            this.newMailDataFrom = username;

            this.forwarder.Disconnect();
            this.forwarder.Close();
        }
        public void sendMail(String[] recipients, String mailData)
        {
            if (recipients.Length == 0) throw new NoRecipientsException("Debe existir por lo menos un destinatario");
            this.forwarder.MailFrom(this.newMailDataFrom);
            foreach (String recipient in recipients)
                this.forwarder.RcptTo(recipient);
            //Data carga el contenido del mail y lo envía
            this.forwarder.Data(mailData);
        }
        public void closeClient()
        {
            this.forwarder.Disconnect();
            this.forwarder.Close();
        }
    }
}