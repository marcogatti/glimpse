using ActiveUp.Net.Imap4;
using ActiveUp.Net.Mail;
using Glimpse.DataAccessLayer.Entities;
using Glimpse.Exceptions.MailInterfacesExceptions;
using Glimpse.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;

namespace Glimpse.MailInterfaces
{
    public class Fetcher
    {
        private Imap4Client receiver;
        private Mailbox currentOpenedMailbox;
        private NameValueCollection accountMailboxesBySpecialProperty = new NameValueCollection();
        const int MAIL_DATA_FIELDS_AMOUNT = 13;

        public Fetcher(String username, String password)
        {
            this.receiver = new Connector().ImapLogin(username, password);
            this.loadMailboxesAndSpecialProperties(this.receiver.Command("LIST \"\" \"*\""));
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

        public MailCollection GetAllMailsDataFrom(String mailbox)
        {
            return this.GetMailsDataFrom(mailbox, 1);
        }
        public MailCollection GetUnseenMailsDataFrom(String mailbox)
        {
            return this.GetMailsDataFrom(mailbox, this.GetMailbox(mailbox).FirstUnseen);
        }
        public MailCollection GetMailDataFromHigherThan(String mailbox, Int64 minimumUID)
        {
            //siempre trae al menos uno, excepto si el mailbox está vacío
            if (minimumUID <= 0) minimumUID = 1;
            return this.GetMailsDataFrom(mailbox, this.GetMailbox(mailbox).Search("UID " + minimumUID + ":*")[0]);
        }
        public MailCollection GetMailsDataFrom(String mailbox, Int32 reversedLastOrdinalToRetrieve)
        {
            //Trae los mails desde el mail más reciente (el ordinal mayor) hasta el mail con ordinal por parámetro reversedLastOrdinalToRetrieve
            if (reversedLastOrdinalToRetrieve <= 0)
                throw new MailReadingOverflowException("No se puede leer mails con ordinal menor a 1.");
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Message retrievedMessage;
            MailEntity retrievedMail;
            
            MailCollection mailsFromMailbox = new MailCollection();

            for (int currentMail = targetMailbox.MessageCount; currentMail >= reversedLastOrdinalToRetrieve; currentMail--)
            {
                retrievedMail = new MailEntity();

                retrievedMessage = targetMailbox.Fetch.MessageObjectPeekWithGMailExtensions(currentMail);

                retrievedMail.Gm_tid = UInt64.Parse(this.CleanIMAPResponse(receiver.Command("FETCH " + currentMail + " (X-GM-THRID)"), "X-GM-THRID"));
                retrievedMail.Gm_mid = UInt64.Parse(this.CleanIMAPResponse(receiver.Command("FETCH " + currentMail + " (X-GM-MSGID)"), "X-GM-MSGID"));
                //mailData["labels"] = this.CleanLabels(retrievedMessage.HeaderFields["x-gm-labels"]);

                DataAccessLayer.Entities.AddressEntity fromAddress = new DataAccessLayer.Entities.AddressEntity();
                fromAddress.MailAddress = retrievedMessage.From.Email;
                fromAddress.Name = retrievedMessage.From.Name;

                retrievedMail.Subject = retrievedMessage.Subject;
                retrievedMail.Date = retrievedMessage.Date;
                retrievedMail.Body = retrievedMessage.BodyHtml.Text;
                retrievedMail.From = fromAddress;
                retrievedMail.HasExtras = (retrievedMessage.Attachments.Count != 0 
                                             || retrievedMessage.EmbeddedObjects.Count != 0
                                             || retrievedMessage.UnknownDispositionMimeParts.Count != 0);

                retrievedMail.ToAddr = this.GetAddressNameAndMail(retrievedMessage.To);
                retrievedMail.BCC = this.GetAddressNameAndMail(retrievedMessage.Bcc);
                retrievedMail.CC = this.GetAddressNameAndMail(retrievedMessage.Cc);

                this.AddUIDToMail(mailbox, targetMailbox.Fetch.Uid(currentMail), ref retrievedMail);
                this.AddFlagsToMail(targetMailbox.Fetch.Flags(currentMail).Merged, ref retrievedMail);
                
                //mailsFromMailbox representa el mail más reciente mientras más bajo sea el índice
                mailsFromMailbox.Add(retrievedMail);
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
        private void loadMailboxesAndSpecialProperties(string imapResponse)
        {
            /*imapResponse del tipo (incluyendo \r\n):
              (\HasNoChildren) "INBOX"
              (\Noselect \HasChildren) "[Gmail]"
              (\HasNoChildren \Drafts) "[Gmail]/Borradores"
              (\HasNoChildren \All) "[Gmail]/Todos"*/
            String[] mailboxes = new String[imapResponse.Split(new string[] { "LIST" }, StringSplitOptions.RemoveEmptyEntries).Length];
            imapResponse = imapResponse.Replace("* LIST", String.Empty).Replace(" \"/\"", String.Empty);
            mailboxes = imapResponse.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (String mailbox in mailboxes)
            {
                if (mailbox.Contains("\\All"))
                {
                    this.accountMailboxesBySpecialProperty.Add("All", this.stripMailboxName(mailbox));
                    continue;
                }
                if (mailbox.Contains("INBOX"))
                {
                    this.accountMailboxesBySpecialProperty.Add("Inbox", "INBOX"); //nombre INBOX fijo por IMAP
                    continue;
                }
                if (mailbox.Contains("\\Trash"))
                {
                    this.accountMailboxesBySpecialProperty.Add("Deleted", this.stripMailboxName(mailbox));
                    continue;
                }
                if (mailbox.Contains("\\Junk"))
                {
                    this.accountMailboxesBySpecialProperty.Add("Spam", this.stripMailboxName(mailbox));
                    continue;
                }
                if (mailbox.Contains("\\Important"))
                {
                    this.accountMailboxesBySpecialProperty.Add("Important", this.stripMailboxName(mailbox));
                    continue;
                }
                if (mailbox.Contains("\\Sent"))
                {
                    this.accountMailboxesBySpecialProperty.Add("Sent", this.stripMailboxName(mailbox));
                    continue;
                }
                if (mailbox.Contains("\\Flagged"))
                {
                    this.accountMailboxesBySpecialProperty.Add("Starred", this.stripMailboxName(mailbox));
                    continue;
                }
                if (mailbox.Contains("\\Drafts"))
                {
                    this.accountMailboxesBySpecialProperty.Add("Drafts", this.stripMailboxName(mailbox));
                    continue;
                }
                if (mailbox.Contains("OK Success") || mailbox.Contains("Noselect"))
                    continue;
                else
                    this.accountMailboxesBySpecialProperty.Add("Labels", this.stripMailboxName(mailbox));
            }
        }
        private String GetAddressNameAndMail(AddressCollection addresses)
        {
            String addressesNamesAndMail = "";
            if (addresses.Count == 0) return addressesNamesAndMail;
            for (int currentAddress = 0; currentAddress < addresses.Count; currentAddress++)
            {
                addressesNamesAndMail += addresses[currentAddress].Merged + ", ";
            }
            addressesNamesAndMail.Remove(addressesNamesAndMail.Length - 2);
            return addressesNamesAndMail;
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
        private void AddUIDToMail(String mailbox, Int64 UID, ref MailEntity mail)
        {
            //UIDs no cargados son completados por GlimpseDB como -1
            if (this.accountMailboxesBySpecialProperty["Inbox"] == mailbox)
            {mail.UidInbox = UID; return;}
            if (this.accountMailboxesBySpecialProperty["All"] == mailbox)
            {mail.UidAll = UID; return;}
            if (this.accountMailboxesBySpecialProperty["Deleted"] == mailbox)
            {mail.UidTrash = UID; return;}
            if (this.accountMailboxesBySpecialProperty["Spam"] == mailbox)
            {mail.UidSpam = UID; return;}
            if (this.accountMailboxesBySpecialProperty["Sent"] == mailbox)
            {mail.UidSent = UID; return;}
            if (this.accountMailboxesBySpecialProperty["Drafts"] == mailbox)
            {mail.UidDraft = UID; return;}
        }
        private void AddFlagsToMail(String flags, ref MailEntity mail)
        {
            if (flags.Contains("answered")) mail.Answered = true;
            if (flags.Contains("flagged")) mail.Flagged = true;
            if (flags.Contains("seen")) mail.Seen = true;
            //if (flags.Contains("draft")) mail.Draft = true;
        }
        private String stripMailboxName(String mailbox)
        {
            //Input: (\HasNoChildren \Drafts) "[Gmail]/Borradores"
            //Output: [Gmail]/Borradores
            String mailboxName = mailbox.Substring(mailbox.IndexOf('"') + 1, mailbox.LastIndexOf('"') - mailbox.IndexOf('"') - 1);
            return mailboxName;
        }
    }
}