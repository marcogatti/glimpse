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
        public Int32 getMailUID(String mailbox, Int64 gmMailID)
        {
            this.GetMailbox(mailbox); //Se asegura que se encuentra seleccionado el mailbox en IMAP
            return Int32.Parse(this.CleanIMAPResponse(this.receiver.Command("UID SEARCH X-GM-MSGID " + gmMailID.ToString()), "SEARCH", false));
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

        public List<Mail> GetAllMailsDataFrom(String mailbox)
        {
            return this.GetMailsDataFrom(mailbox, 1);
        }
        public List<Mail> GetUnseenMailsDataFrom(String mailbox)
        {
            return this.GetMailsDataFrom(mailbox, this.GetMailbox(mailbox).FirstUnseen);
        }
        public List<Mail> GetMailDataFromHigherThan(String mailbox, Int64 minimumUID)
        {
            //siempre trae al menos uno, excepto si el mailbox está vacío
            if (minimumUID <= 0) minimumUID = 1;
            return this.GetMailsDataFrom(mailbox, this.GetMailbox(mailbox).Search("UID " + minimumUID + ":*")[0]);
        }
        public List<Mail> GetMailsDataFrom(String mailbox, Int32 reversedLastOrdinalToRetrieve)
        {
            //Trae los mails desde el mail más reciente (el ordinal mayor) hasta el mail con ordinal por parámetro reversedLastOrdinalToRetrieve
            if (reversedLastOrdinalToRetrieve <= 0)
                throw new MailReadingOverflowException("No se puede leer mails con ordinal menor a 1.");
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Message retrievedMessage;
            MailEntity retrievedMail;

            List<Mail> mailsFromMailbox = new List<Mail>();

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
                mailsFromMailbox.Add(new Mail(retrievedMail));
            }
            return mailsFromMailbox;
        }

        public void archiveMail(Int64 gmMailID)
        {
            this.removeMailTag("INBOX", gmMailID);
        }
        public void removeMailTag(String mailbox, Int64 gmMailID)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 mailUID = this.getMailUID(mailbox, gmMailID);
            targetMailbox.UidMoveMessage(mailUID, this.accountMailboxesBySpecialProperty["All"]);
            this.currentOpenedMailbox.MessageCount--;
        }
        public void addMailTag(String mailbox, String tagToAdd, Int64 gmMailID)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 mailUID = this.getMailUID(mailbox, gmMailID);
            targetMailbox.UidCopyMessage(mailUID, tagToAdd);
        }
        public void moveToTrash(String mailbox, Int64 gmMailID)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 mailUID = this.getMailUID(mailbox, gmMailID);
            targetMailbox.UidMoveMessage(mailUID, this.accountMailboxesBySpecialProperty["Deleted"]);
            this.currentOpenedMailbox.MessageCount--;
        }
        public void removeFromTrash(String destinyMailbox, Int64 gmMailID)
        {
            Mailbox targetMailbox = this.GetMailbox(this.accountMailboxesBySpecialProperty["Deleted"]);
            Int32 mailUID = this.getMailUID(this.accountMailboxesBySpecialProperty["Deleted"], gmMailID);
            targetMailbox.UidMoveMessage(mailUID, destinyMailbox);
            this.currentOpenedMailbox.MessageCount--;
        }
        public void deleteFromTrash(Int64 gmMailID)
        {
            Mailbox targetMailbox = this.GetMailbox(this.accountMailboxesBySpecialProperty["Deleted"]);
            Int32 mailUID = this.getMailUID(this.accountMailboxesBySpecialProperty["Deleted"], gmMailID);
            targetMailbox.UidDeleteMessage(mailUID, true);
            this.currentOpenedMailbox.MessageCount--;
        }

        public void setAnsweredFlag(String mailbox, Int64 gmMailID, Boolean isAnswered)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 mailUID = this.getMailUID(mailbox, gmMailID);
            if (isAnswered)
                targetMailbox.UidAddFlagsSilent(mailUID, new FlagCollection { "Answered" });
            else
                targetMailbox.UidRemoveFlagsSilent(mailUID, new FlagCollection { "Answered" });
        }
        public void setFlaggedFlag(String mailbox, Int64 gmMailID, Boolean isFlagged)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 mailUID = this.getMailUID(mailbox, gmMailID);
            if (isFlagged)
                targetMailbox.UidAddFlagsSilent(mailUID, new FlagCollection { "Flagged" });
            else
                targetMailbox.UidRemoveFlagsSilent(mailUID, new FlagCollection { "Flagged" });
        }
        public void setDraftFlag(String mailbox, Int64 gmMailID, Boolean isDraft)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 mailUID = this.getMailUID(mailbox, gmMailID);

            if (isDraft)
                targetMailbox.UidAddFlagsSilent(mailUID, new FlagCollection { "Draft" });
            else
                targetMailbox.UidRemoveFlagsSilent(mailUID, new FlagCollection { "Draft" });
        }
        public void setSeenFlag(String mailbox, Int64 gmMailID, Boolean isSeen)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 mailUID = this.getMailUID(mailbox, gmMailID);
            if (isSeen)
                targetMailbox.UidAddFlagsSilent(mailUID, new FlagCollection { "Seen" });
            else
                targetMailbox.UidRemoveFlagsSilent(mailUID, new FlagCollection { "Seen" });
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
        private void loadMailboxesAndSpecialProperties(string imapLISTResponse)
        {
            /*imapResponse del tipo (incluyendo \r\n):
              (\HasNoChildren) "INBOX"
              (\Noselect \HasChildren) "[Gmail]"
              (\HasNoChildren \Drafts) "[Gmail]/Borradores"
              (\HasNoChildren \All) "[Gmail]/Todos"*/
            String[] mailboxes = new String[imapLISTResponse.Split(new string[] { "LIST" }, StringSplitOptions.RemoveEmptyEntries).Length];
            imapLISTResponse = imapLISTResponse.Replace("* LIST", String.Empty).Replace(" \"/\"", String.Empty);
            mailboxes = imapLISTResponse.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

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
        private void AddUIDToMail(String mailbox, Int64 UID, ref MailEntity mail)
        {
            //UIDs no cargados son completados por GlimpseDB como -1
            if (this.accountMailboxesBySpecialProperty["Inbox"] == mailbox)
            { mail.UidInbox = UID; return; }
            if (this.accountMailboxesBySpecialProperty["All"] == mailbox)
            { mail.UidAll = UID; return; }
            if (this.accountMailboxesBySpecialProperty["Deleted"] == mailbox)
            { mail.UidTrash = UID; return; }
            if (this.accountMailboxesBySpecialProperty["Spam"] == mailbox)
            { mail.UidSpam = UID; return; }
            if (this.accountMailboxesBySpecialProperty["Sent"] == mailbox)
            { mail.UidSent = UID; return; }
            if (this.accountMailboxesBySpecialProperty["Drafts"] == mailbox)
            { mail.UidDraft = UID; return; }
        }
        private void AddFlagsToMail(String flags, ref MailEntity mail)
        {
            if (flags.Contains("answered"))
                mail.Answered = true;
            if (flags.Contains("flagged"))
                mail.Flagged = true;
            if (flags.Contains("seen"))
                mail.Seen = true;
            //if (flags.Contains("draft")) mail.Draft = true;
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
        private String CleanIMAPResponse(String imapResponse, String imapParameter, Boolean isGMSpecific = true)
        {
            //Respuesta del tipo: 
            //GMSpecific:    "* 1 FETCH (X-GM-MSGID 1278455344230334865)\r\na006 OK FETCH (Success)"
            //NotGMSpecific: "* SEARCH 15184\r\na007 OK SEARCH (Success)
            imapResponse = imapResponse.Remove(0, imapResponse.IndexOf(imapParameter) + imapParameter.Length + 1);
            if (isGMSpecific)
            {
                imapResponse = imapResponse.Remove(imapResponse.IndexOf(")"));
            }
            else
            {
                imapResponse = imapResponse.Remove(imapResponse.IndexOf("\r\n"));
            }
            return imapResponse;
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