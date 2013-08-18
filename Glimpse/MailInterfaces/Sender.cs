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
        public void sendMail(SmtpMessage mail, String username, String password)
        {
            if (mail.Recipients.Count == 0) throw new NoRecipientsException("Debe existir por lo menos un destinatario");
            //mail.SendSsl crea la conexión, manda los 3 parámetros de SMPT (MAIL, RCPT y DATA) y cierra la conexión
            mail.SendSsl("smtp.gmail.com", 465, username, password, SaslMechanism.Login);
        }
    }
}