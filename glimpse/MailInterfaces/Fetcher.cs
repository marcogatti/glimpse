using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ActiveUp.Net.Imap4;
using ActiveUp.Net.Mail;
using System.Collections.Specialized;
using Glimpse.Exceptions.MailInterfacesExceptions;
using Glimpse.DataAccessLayer.Entities;

namespace Glimpse.MailInterfaces
{
    public class Fetcher
    {
        private Imap4Client receiver;
        private Mailbox currentOpenedMailbox;
        const int MAIL_DATA_FIELDS_AMOUNT = 13;

        public Fetcher(String username, String password)
        {
            this.receiver = new Connector().ImapLogin(username, password);
            this.currentOpenedMailbox = null;
        }

        public MessageCollection GetInboxMails()
        {
            return this.GetAllMailsFrom("INBOX");
        }
        public MessageCollection GetAllMailsFrom(String mailbox)
        {
            Mailbox targetMail = this.GetMailbox(mailbox);
            Int32 amountOfMails = targetMail.MessageCount;
            MessageCollection messages = new MessageCollection();

            for (int i = amountOfMails; i > 0; i--)
            {
                messages.Add(targetMail.Fetch.MessageObjectPeekWithGMailExtensions(i));
            }

            return messages;
        }

        public Int32 GetLastUIDFrom(String mailbox)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 amountOfMails = targetMailbox.MessageCount;
            return targetMailbox.Fetch.Uid(amountOfMails);
        }

        public Int32 GetAmountOfMailsFrom(String mailbox)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            return targetMailbox.MessageCount;
        }
        public byte[] GetAttachmentFromMail(String mailbox, Int32 uniqueMailID, String attachmentName)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            AttachmentCollection attachmentsInMail = targetMailbox.Fetch.UidMessageObject(uniqueMailID).Attachments;
            MimePart desiredAttachment;
            try
            {
                desiredAttachment = attachmentsInMail.Cast<MimePart>()
                                              .Where<MimePart>(x => x.Filename == attachmentName)
                                              .Single<MimePart>();
            }
            catch (System.InvalidOperationException systemException)
            {
                throw new InvalidAttachmentException(systemException.Message ,"Se pide un archivo adjunto que no existe, o más de un adjunto posee el mismo nombre.");
            }
            return desiredAttachment.BinaryContent;
        }

        public Mail[] GetAllMailsDataFrom(String mailbox)
        {
            return this.GetMailsDataFrom(mailbox, 1);
        }
        public Mail[] GetUnseenMailsDataFrom(String mailbox)
        {
            return this.GetMailsDataFrom(mailbox, this.GetMailbox(mailbox).FirstUnseen);
        }
        public Mail[] GetMailDataFromHigherThan(String mailbox, Int32 minimumUID)
        {
            //siempre trae al menos uno, excepto si el mailbox está vacío
            return this.GetMailsDataFrom(mailbox, this.GetMailbox(mailbox).Search("UID " + minimumUID)[0]);
        }
        public Mail[] GetMailsDataFrom(String mailbox, Int32 reversedLastOrdinalToRetrieve)
        {
            //Trae los mails desde el mail más reciente (el ordinal mayor) hasta el mail con ordinal por parámetro reversedLastOrdinalToRetrieve
            if (reversedLastOrdinalToRetrieve <= 0)
                throw new MailReadingOverflowException("No se puede leer mails con ordinal menor a 1.");
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Message retrievedMessage;
            Mail retrievedMail;
            DataAccessLayer.Entities.Address fromAddress = new DataAccessLayer.Entities.Address();
            Mail[] mailsFromMailbox = new Mail[targetMailbox.MessageCount - reversedLastOrdinalToRetrieve + 1];

            for (int currentMail = targetMailbox.MessageCount; currentMail >= reversedLastOrdinalToRetrieve; currentMail--)
            {
                retrievedMail = new Mail();

                retrievedMessage = targetMailbox.Fetch.MessageObjectPeekWithGMailExtensions(currentMail);

                //mailData["threadID"] = this.CleanIMAPResponse(receiver.Command("FETCH " + currentMail + " (X-GM-THRID)"), "X-GM-THRID");
                //mailData["gmID"] = this.CleanIMAPResponse(receiver.Command("FETCH " + currentMail + " (X-GM-MSGID)"), "X-GM-MSGID");
                //mailData["labels"] = this.CleanLabels(retrievedMessage.HeaderFields["x-gm-labels"]);

                fromAddress.MailAddress = retrievedMessage.From.Email;
                fromAddress.Name = retrievedMessage.From.Name;

                retrievedMail.Subject = retrievedMessage.Subject;
                retrievedMail.Date = retrievedMessage.Date;
                retrievedMail.Body = retrievedMessage.BodyHtml.Text;
                retrievedMail.From = fromAddress;
                retrievedMail.HasAttachments = (retrievedMessage.Attachments.Count != 0 
                                             || retrievedMessage.EmbeddedObjects.Count != 0
                                             || retrievedMessage.UnknownDispositionMimeParts.Count != 0);

                retrievedMail.To = this.GetAddressNames(retrievedMessage.To);
                retrievedMail.BCC = this.GetAddressNames(retrievedMessage.Bcc);
                retrievedMail.CC = this.GetAddressNames(retrievedMessage.Cc);

                this.AddUIDToMail(mailbox, targetMailbox.Fetch.Uid(currentMail), ref retrievedMail);
                this.AddFlagsToMail(targetMailbox.Fetch.Flags(currentMail).Merged, ref retrievedMail);
                
                //mailsFromMailbox representa el mail más reciente mientras más bajo sea el índice
                mailsFromMailbox[targetMailbox.MessageCount - currentMail] = retrievedMail;
            }
            return mailsFromMailbox;
        }
        public void CloseClient()
        {
            this.receiver.Disconnect();
        }

        private Mailbox GetMailbox(String targetMailboxName)
        {
            if (this.currentOpenedMailbox == null)
            {
                return this.OpenMailbox(targetMailboxName);
            }
            else
            {
                if (this.currentOpenedMailbox.Name == targetMailboxName)
                {
                    return this.currentOpenedMailbox;
                }
                else
                {
                    this.CloseMailbox();
                    return this.OpenMailbox(targetMailboxName);
                }
            }
        }
        private Mailbox OpenMailbox(String mailbox)
        {
            this.currentOpenedMailbox = this.receiver.SelectMailbox(mailbox);
            return this.currentOpenedMailbox;
        }
        private void CloseMailbox()
        {
            this.receiver.Close();
            this.currentOpenedMailbox = null;
        }
        private String GetFlagsNames (FlagCollection flags)
        {
            String flagsNames = "";
            if (flags.Count == 0) return flagsNames;
            for (int currentFlag = 0; currentFlag < flags.Count; currentFlag++)
            {
                flagsNames += flags[currentFlag].Name + ", ";
            }
            flagsNames.Remove(flagsNames.Length - 2);
            return flagsNames;
        }
        private String GetAddressNames(AddressCollection addresses)
        {
            String addressesNames = "";
            if (addresses.Count == 0) return addressesNames;
            for (int currentAddress = 0; currentAddress < addresses.Count; currentAddress++)
            {
                addressesNames += addresses[currentAddress].Name + " " + addresses[currentAddress].Email + ", ";
            }
            addressesNames.Remove(addressesNames.Length - 2);
            return addressesNames;
        }
        private String CleanLabels(String labels)
        {

            labels = labels.Replace("\\", string.Empty);
            labels = labels.Replace("\"", string.Empty);
            //Gmail no agrega el label del mailbox IMAP donde se encuentra el mail analizado
            labels += " " + this.currentOpenedMailbox.Name;

            return labels;
        }
        private String CleanIMAPResponse(String imapResponse, String imapParameter)
        {
            //Respuesta del tipo: "* 1 FETCH (X-GM-MSGID 1278455344230334865)\r\na006 OK FETCH (Success)"
            imapResponse = imapResponse.Remove(0, imapResponse.IndexOf(imapParameter) + imapParameter.Length + 1);
            imapResponse = imapResponse.Remove(imapResponse.IndexOf(")"));
            return imapResponse;
        }
        private void AddUIDToMail(String mailbox, Int64 UID, ref Mail mail)
        {
            switch (mailbox) { //UIDs no cargados son completados por GlimpseDB como -1
                case "INBOX":
                    mail.UidInbox = UID;
                    break;
                case "[Gmail]/Todos":
                case "[Gmail]/All":
                    mail.UidAll = UID;
                    break;
                case "[Gmail]/Papelera":
                case "[Gmail]/Trash":
                case "[Gmail]/Deleted":
                    mail.UidTrash = UID;
                    break;
                case "[Gmail]/Spam":
                    mail.UidSpam = UID;
                    break;
                case "[Gmail]/Borradores":
                case "[Gmail]/Drafts":
                    mail.UidDraft = UID;
                    break;
                case "[Gmail]/Enviados":
                case "[Gmail]/Sent":
                    mail.UidSent = UID;
                    break;
            }
        }
        private void AddFlagsToMail(String flags, ref Mail mail)
        {
            if (flags.Contains("answered")) mail.Answered = true;
            if (flags.Contains("flagged")) mail.Flagged = true;
            if (flags.Contains("seen")) mail.Seen = true;
            //if (flags.Contains("draft")) mail.Draft = true;
        }
    }
}