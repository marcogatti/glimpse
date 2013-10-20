using ActiveUp.Net.Mail;
using Glimpse.Exceptions.MailInterfacesExceptions;
using Glimpse.Helpers;
using Glimpse.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;

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
            this.CheckRecipients(newMail.To);
            //mail.SendSsl crea la conexión, manda los 3 parámetros de SMPT (MAIL, RCPT y DATA) y cierra la conexión
            newMail.SendSsl("smtp.gmail.com", 465, this.senderAddress, CryptoHelper.DecryptDefaultKey(this.password), SaslMechanism.Login);
        }
        public void sendMail(AddressCollection recipients, String bodyHTML, String subject = "",
                             AddressCollection CC = null, AddressCollection BCC = null,
                             List<HttpPostedFile> attachments = null)
        {
            this.CheckRecipients(recipients);
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
                    throw new InvalidRecipientsException("La direccion " + this.ParseWrongReceipt(exc.Message) +
                                                         " no es valida.", exc);
                else
                    throw exc;
            }
        }

        public static void SendResetPasswordMail(String username, String mailAddress, String newPassword)
        {
            SmtpMessage resetMail = new SmtpMessage();
            AddressCollection to = new AddressCollection();

            resetMail.From = new ActiveUp.Net.Mail.Address("GlimpseInnovationSystems@gmail.com");
            resetMail.BodyText.Text = "Usted ha olvidado la contraseña de su usuario Glimpse: " + username +
                                      ".\nLa nueva contraseña autogenerada es: \"" + newPassword + "\"." + 
                                      "\nDeberá ingresar con ésta clave la próxima vez que ingrese a Glimpse.";
            resetMail.BodyText.ContentTransferEncoding = ContentTransferEncoding.QuotedPrintable;
            resetMail.BodyText.Charset = "ISO-8859-1";
            resetMail.BodyText.Format = BodyFormat.Text;

            String encodedSubject = "=?ISO-8859-1?B?";
            Encoding iso = Encoding.GetEncoding("ISO-8859-1");
            encodedSubject += Convert.ToBase64String(iso.GetBytes("Nueva Contraseña Glimpse"), Base64FormattingOptions.InsertLineBreaks);
            encodedSubject += "?=";
            resetMail.Subject = encodedSubject;

            to.Add(new ActiveUp.Net.Mail.Address(mailAddress));
            resetMail.To = to;
            resetMail.SendSsl("smtp.gmail.com", 465, "glimpseinnovationsystems@gmail.com", "poiewq123890", SaslMechanism.Login);
        }
        public static void SendGreetingsPassword(User newUser, String mailAddress)
        {
            SmtpMessage greetingsMail = new SmtpMessage();
            AddressCollection to = new AddressCollection();
            IList<MailAccount> userMailAccounts = newUser.GetAccounts();

            #region Build Mail Body
            String mailBody = "";
            mailBody += "¡Bienvenido a Glimpse!\nSu nuevo usuario es: " + newUser.Entity.Username + " (" +
                        newUser.Entity.Firstname + " " + newUser.Entity.Lastname + ").\n" +
                        "Las cuentas asociadas al mismo son:\n\n";
            foreach (MailAccount userMailAccount in userMailAccounts)
            {
                mailBody += "\t- " + userMailAccount.Entity.Address;
                if (userMailAccount.Entity.IsMainAccount)
                    mailBody += " (cuenta principal)";
                mailBody += ".\n";
            }
            mailBody += "\nPara editar la información de su usuario, ingrese a Glimpse y vaya al panel de Configuración.\n\n" +
                        "Si no se ha registrado en Glimpse, por favor ignore este mensaje o responda al mismo indicando el inconveniente originado.";
            #endregion

            greetingsMail.From = new ActiveUp.Net.Mail.Address("GlimpseInnovationSystems@gmail.com");
            greetingsMail.BodyText.Text = mailBody;
            greetingsMail.BodyText.ContentTransferEncoding = ContentTransferEncoding.QuotedPrintable;
            greetingsMail.BodyText.Charset = "ISO-8859-1";
            greetingsMail.BodyText.Format = BodyFormat.Text;

            String encodedSubject = "=?ISO-8859-1?B?";
            Encoding iso = Encoding.GetEncoding("ISO-8859-1");
            encodedSubject += Convert.ToBase64String(iso.GetBytes("¡Bienvenido a Glimpse!"), Base64FormattingOptions.InsertLineBreaks);
            encodedSubject += "?=";
            greetingsMail.Subject = encodedSubject;

            to.Add(new ActiveUp.Net.Mail.Address(mailAddress));
            greetingsMail.To = to;
            greetingsMail.SendSsl("smtp.gmail.com", 465, "glimpseinnovationsystems@gmail.com", "poiewq123890", SaslMechanism.Login);
        }

        private static void SetMailAttachments(List<HttpPostedFile> attachments, SmtpMessage newMail)
        {
            if (attachments != null)
                foreach (HttpPostedFile file in attachments)
                {
                    Attachment attachment = new Attachment();
                    using (BinaryReader reader = new BinaryReader(file.InputStream))
                        attachment.BinaryContent = reader.ReadBytes(file.ContentLength);
                    attachment.Filename = file.FileName;
                    attachment.ContentType.Type = file.ContentType;
                    attachment.ParentMessage = newMail;
                    newMail.Attachments.Add(attachment);
                }
        }
        private static void SetMailRecipients(AddressCollection recipients, String subject, AddressCollection CC, AddressCollection BCC, SmtpMessage newMail)
        {
            //=?ISO-8859-1?B?0fHR8dHx0fG0tLS0b/PztLRv8w==?=
            String encodedSubject = "=?ISO-8859-1?B?";
            Encoding iso = Encoding.GetEncoding("ISO-8859-1");
            encodedSubject += Convert.ToBase64String(iso.GetBytes(subject), Base64FormattingOptions.InsertLineBreaks);
            encodedSubject += "?=";
            newMail.Subject = encodedSubject;
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
        
        private void SetMailSender(SmtpMessage newMail)
        {
            if (this.senderName != null)
                newMail.From = new ActiveUp.Net.Mail.Address(this.senderAddress, this.senderName);
            else
                newMail.From = new ActiveUp.Net.Mail.Address(this.senderAddress);
        }
        private void CheckRecipients(AddressCollection recipients)
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