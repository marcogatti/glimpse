using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ActiveUp.Net.Mail;
using ActiveUp.Net.Imap4;
using glimpse.Exceptions.MailInterfacesExceptions;

namespace glimpse.MailInterfaces
{
    public class Connector
    {
        private Imap4Client Client { get; set; }

        public Connector()
        {
            this.Client = new Imap4Client();
        }

        public Imap4Client Login(String username, String password)
        {
            this.Connect(true);
            this.Client.Login(username, password);

            return this.Client;
        }

        private void Connect(Boolean requiresSSL)
        {
            if (requiresSSL)
            {
                Client.ConnectSsl("imap.gmail.com", 993);
            }
            else
            {
                throw new InvalidConnectionException("No se puede realizar conexion a Gmail sin SSL");
            }
        }

    }
}