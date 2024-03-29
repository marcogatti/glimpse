﻿using System;
using ActiveUp.Net.Mail;
using Glimpse.Exceptions.MailInterfacesExceptions;
using Glimpse.Helpers;

namespace Glimpse.MailInterfaces
{
    public class Connector
    {
        private Imap4Client ImapClient { get; set; }

        public Imap4Client ImapLogin(String username, String password)
        {
            this.ImapClient = new Imap4Client();
            this.ImapConnect(true);
            this.ImapAttemptLogin(username, CryptoHelper.DecryptDefaultKey(password));
            return this.ImapClient;
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
                this.ImapClient.LoginFast(username, password);
            }
            catch(Imap4Exception imapException)
            {
                throw new InvalidAuthenticationException(imapException.Message, "El usuario o la contraseña son inválidos.");
            }
        }
    }
}