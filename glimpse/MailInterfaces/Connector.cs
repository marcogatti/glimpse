using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ActiveUp.Net.Mail;
using ActiveUp.Net.Imap4;
using Glimpse.Exceptions.MailInterfacesExceptions;

namespace Glimpse.MailInterfaces
{
    public class Connector
    {
        private Imap4Client ImapClient { get; set; }
        private SmtpClient SmtpClient { get; set; }

        public Imap4Client ImapLogin(String username, String password)
        {
            this.ImapClient = new Imap4Client();
            this.ImapConnect(true);
            this.ImapAttemptLogin(username, password);
            return this.ImapClient;
        }
        public SmtpClient SmtpLogin(String username, String password)
        {
            this.SmtpClient = new SmtpClient();
            this.SmtpConnect(true);
            this.SmtpAttemptLogin(username, password);
            return this.SmtpClient;
        }

        private void ImapConnect(Boolean requiresSSL)
        {
            if (requiresSSL)
            {
                this.ImapClient.ConnectSsl("imap.gmail.com", 993);
            }
            else
            {
                throw new InvalidConnectionException("No se puede realizar conexión a servidor IMAP de Gmail sin SSL.");
            }
        }
        private void ImapAttemptLogin(String username, String password)
        {
            try
            {
                this.ImapClient.Login(username, password);
            }
            catch(Imap4Exception imapException)
            {
                throw new InvalidAuthenticationException(imapException.Message, "El usuario o la contraseña son inválidos.");
            }
        }
        private void SmtpConnect(bool requiresSSL)
        {
            if (requiresSSL)
            {
                this.SmtpClient.ConnectSsl("smtp.gmail.com", 465);
                this.SmtpClient.Helo("gmail.com");
            }
            else
            {
                throw new InvalidConnectionException("No se puede realizar conexión a servidor SMTP de Gmail sin SSL.");
            }
        }
        private void SmtpAttemptLogin(string username, string password)
        {
            try
            {
                //SaslMechanism.CramMd5 no está soportado, sólo SaslMechanism.Login
                this.SmtpClient.Authenticate(username, password, SaslMechanism.Login);
            }
            catch (SmtpException smtpException)
            {
                throw new InvalidAuthenticationException(smtpException.Message, "El usuario o la contraseña son inválidos.");
            }
        }

    }
}