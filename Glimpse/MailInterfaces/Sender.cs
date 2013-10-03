using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ActiveUp.Net.Mail;
using Glimpse.MailInterfaces;
using Glimpse.Exceptions.MailInterfacesExceptions;
using Glimpse.Helpers;

namespace Glimpse.MailInterfaces
{
    public class Sender
    {
        private String senderAddress;
        private String senderName;
        private String password;

        public Sender(String senderAddress, String password, String senderName = null)
        {
            this.senderAddress = senderAddress;
            this.password = password;
            this.senderName = senderName;
        }

        public void sendMail(SmtpMessage newMail)
        {
            this.checkRecipients(newMail.To);
            //mail.SendSsl crea la conexión, manda los 3 parámetros de SMPT (MAIL, RCPT y DATA) y cierra la conexión
            newMail.SendSsl("smtp.gmail.com", 465, this.senderAddress, CryptoHelper.DecryptDefaultKey(this.password), SaslMechanism.Login);
        }
        public void sendMail(AddressCollection recipients, String bodyHTML, String subject = "",
                             AddressCollection CC = null, AddressCollection BCC = null,
                             AttachmentCollection attachments = null)
        {
            this.checkRecipients(recipients);
            SmtpMessage newMail = new SmtpMessage();

            this.SetMailSender(newMail);
            Sender.SetMailBody(bodyHTML, newMail);
            Sender.SetMailRecipients(recipients, subject, CC, BCC, newMail);
            Sender.SetMailAttachments(attachments, newMail);

            try
            {
                newMail.SendSsl("smtp.gmail.com", 465, this.senderAddress, CryptoHelper.DecryptDefaultKey(this.password), SaslMechanism.Login);
            }
            catch (SmtpException exc)
            {
                if (exc.Message.Contains("Command \"rcpt to: ") && exc.Message.Contains(" failed"))
                {
                    throw new InvalidRecipientsException("La direccion " + this.ParseWrongReceipt(exc.Message) +
                                                         " no es valida.", exc);
                }
                else
                {
                    throw exc;
                }
            }
        }

        private static void SetMailAttachments(AttachmentCollection attachments, SmtpMessage newMail)
        {
            if (attachments != null)
                foreach (Attachment attachment in attachments)
                    newMail.Attachments.Add(attachment);
        }
        
        private void SetMailSender(SmtpMessage newMail)
        {
            if (this.senderName != null)
                newMail.From = new Address(this.senderAddress, this.senderName);
            else
                newMail.From = new Address(this.senderAddress);
        }

        private static void SetMailRecipients(AddressCollection recipients, String subject, AddressCollection CC, AddressCollection BCC, SmtpMessage newMail)
        {
            newMail.Subject = subject;
            newMail.To = recipients;
            newMail.Cc = CC ?? new AddressCollection();
            newMail.Bcc = BCC ?? new AddressCollection();
        }

        private static void SetMailBody(String bodyHTML, SmtpMessage newMail)
        {
            if (bodyHTML != null)
            {
                newMail.BodyHtml.Text = bodyHTML;
                newMail.BodyText.Text = newMail.BodyHtml.TextStripped;
            }
            else
            {
                newMail.BodyHtml.Text = newMail.BodyText.Text = " ";
            }

            newMail.BodyHtml.ContentTransferEncoding = ContentTransferEncoding.QuotedPrintable;
            newMail.BodyHtml.Charset = "ISO-8859-1";
            newMail.BodyHtml.Format = BodyFormat.Html;

            newMail.BodyText.ContentTransferEncoding = ContentTransferEncoding.QuotedPrintable;
            newMail.BodyText.Charset = "ISO-8859-1";
            newMail.BodyText.Format = BodyFormat.Text;
        }

        private void checkRecipients(AddressCollection recipients)
        {
            if (recipients.Count == 0)
                throw new NoRecipientsException("Debe existir por lo menos un destinatario.");
        }
        private String ParseWrongReceipt(String smtpResponse)
        {
            //respuesta del tipo: "Command "rcpt to: <qweqwe>" failed : 553-5.1.2 We weren't able to find the recipient domain. Please check for any"
            smtpResponse = smtpResponse.Remove(smtpResponse.IndexOf(" failed") - 2);
            smtpResponse = smtpResponse.Remove(0, smtpResponse.IndexOf("<")+ 1);
            return smtpResponse;
        }
    }
}