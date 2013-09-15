﻿using System;
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
            newMail.SendSsl("smtp.gmail.com", 465, this.senderAddress, this.password, SaslMechanism.Login);
        }
        public void sendMail(AddressCollection recipients, String bodyHTML, String subject = "",
                             AddressCollection CC = null, AddressCollection BCC = null,
                             AttachmentCollection attachments = null)
        {
            this.checkRecipients(recipients);
            SmtpMessage newMail = new SmtpMessage();

            SetMailSender(newMail);

            SetMailBody(bodyHTML, newMail);

            SetMailRecipients(recipients, subject, CC, BCC, newMail);

            SetMailAttachments(attachments, newMail);

            newMail.SendSsl("smtp.gmail.com", 465, this.senderAddress, this.password, SaslMechanism.Login);
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
    }
}