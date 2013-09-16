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
        private NameValueCollection accountMailboxesBySpecialProperty;
        private IList<LabelEntity> accountLabels;

        public Fetcher(String username, String password)
        {
            this.receiver = new Connector().ImapLogin(username, password);
            this.accountMailboxesBySpecialProperty = new NameValueCollection();
            this.loadMailboxesAndSpecialProperties(this.receiver.Command("LIST \"\" \"*\""));
            this.currentOpenedMailbox = null;
        }
        public NameValueCollection getLabels()
        {
            return this.accountMailboxesBySpecialProperty;
        }
        public void setLabels(IList<LabelEntity> labels)
        {
            this.accountLabels = labels;
        }

        public Int32 GetLastUIDFrom(String mailbox)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 amountOfMails = targetMailbox.MessageCount;
            return targetMailbox.Fetch.Uid(amountOfMails);
        }
        public Int32 GetFirstUIDFrom(String mailbox)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            return targetMailbox.Fetch.Uid(1);
        }

        public Int32 GetAmountOfMailsFrom(String mailbox)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            return targetMailbox.MessageCount;
        }
        public Int32 getMailUID(String mailbox, UInt64 gmMailID)
        {
            this.GetMailbox(mailbox); //Se asegura que se encuentra seleccionado el mailbox en IMAP
            return Int32.Parse(this.CleanIMAPResponse(this.receiver.Command("UID SEARCH X-GM-MSGID " + gmMailID.ToString()), "SEARCH", false));
        }
        public byte[] GetAttachmentFromMail(String mailbox, UInt64 gmMailID, String attachmentName)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 mailUID = this.getMailUID(mailbox, gmMailID);
            AttachmentCollection attachmentsInMail = targetMailbox.Fetch.UidMessageObject(mailUID).Attachments;
            MimePart desiredAttachment;
            try
            {
                desiredAttachment = attachmentsInMail.Cast<MimePart>()
                                              .Where<MimePart>(x => x.Filename == attachmentName)
                                              .Single<MimePart>();
            }
            catch (System.InvalidOperationException systemException)
            {
                throw new InvalidAttachmentException(systemException.Message, "Se pide un archivo adjunto que no existe, o más de un adjunto posee el mismo nombre.");
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
            Int32 mailOrdinalOfMinimumUID;

            Mailbox targetMailbox = this.GetMailbox(mailbox);

            if (minimumUID != 0)
                mailOrdinalOfMinimumUID = Int32.Parse(this.CleanOrdinalResponse(this.receiver.Command("UID FETCH " + minimumUID + " UID")));
            else
                mailOrdinalOfMinimumUID = 0;

            //siempre trae al menos uno, excepto si el mailbox está vacío
            Int32[] mailsUIDs = targetMailbox.Search("UID " + (minimumUID + 1).ToString() + ":*");

            //si el mailbox está vacío o si IMAP trajo sólo el último y es el mismo que minimumUID
            if (mailsUIDs == null || mailsUIDs[0] == mailOrdinalOfMinimumUID)
                return new List<Mail>();
            else
                return this.GetMailsDataFrom(mailbox, mailsUIDs[0]);
        }
        public List<Mail> GetMailsDataFrom(String mailbox, Int32 firstOrdinalToRetrieve)
        {
            //Trae los mails desde firstOrdinalToRetrieve hasta el último del mailbox
            if (firstOrdinalToRetrieve <= 0)
                throw new MailReadingOverflowException("No se puede leer mails con ordinal menor a 1.");

            Mailbox targetMailbox;
            Mail retrievedMail;
            targetMailbox = this.GetMailbox(mailbox);

            List<Mail> mailsFromMailbox = new List<Mail>();

            for (int currentMail = firstOrdinalToRetrieve; currentMail <= targetMailbox.MessageCount; currentMail++)
            {
                retrievedMail = this.FetchMail(targetMailbox, currentMail);
                if (retrievedMail != null)
                    mailsFromMailbox.Add(retrievedMail);
            }
            return mailsFromMailbox;
        }
        public List<Mail> GetMailsBetweenUID(String mailbox, Int32 firstUID, Int32 lastUID)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);

            Int32[] mailsUIDs = targetMailbox.Search("UID " + firstUID + ":" + lastUID);

            List<Mail> retrievedMails = new List<Mail>();
            Mail retrievedMail;

            foreach (Int32 currentMailOrdinal in mailsUIDs)
            {
                retrievedMail = this.FetchMail(targetMailbox, currentMailOrdinal);
                if (retrievedMail != null)
                    retrievedMails.Add(retrievedMail);
            }
            return retrievedMails;
        }

        public void archiveMail(UInt64 gmMailID)
        {
            this.removeMailTag("INBOX", gmMailID);
        }
        public void removeMailTag(String mailbox, UInt64 gmMailID)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 mailUID = this.getMailUID(mailbox, gmMailID);
            targetMailbox.UidMoveMessage(mailUID, this.accountMailboxesBySpecialProperty["All"]);
            this.currentOpenedMailbox.MessageCount--;
        }
        public void addMailTag(String mailbox, String tagToAdd, UInt64 gmMailID)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 mailUID = this.getMailUID(mailbox, gmMailID);
            targetMailbox.UidCopyMessage(mailUID, tagToAdd);
        }
        public void moveToTrash(String mailbox, UInt64 gmMailID)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 mailUID = this.getMailUID(mailbox, gmMailID);
            targetMailbox.UidMoveMessage(mailUID, this.accountMailboxesBySpecialProperty["Trash"]);
            this.currentOpenedMailbox.MessageCount--;
        }
        public void removeFromTrash(String destinyMailbox, UInt64 gmMailID)
        {
            Mailbox targetMailbox = this.GetMailbox(this.accountMailboxesBySpecialProperty["Trash"]);
            Int32 mailUID = this.getMailUID(this.accountMailboxesBySpecialProperty["Trash"], gmMailID);
            targetMailbox.UidMoveMessage(mailUID, destinyMailbox);
            this.currentOpenedMailbox.MessageCount--;
        }
        public void deleteFromTrash(UInt64 gmMailID)
        {
            Mailbox targetMailbox = this.GetMailbox(this.accountMailboxesBySpecialProperty["Trash"]);
            Int32 mailUID = this.getMailUID(this.accountMailboxesBySpecialProperty["Trash"], gmMailID);
            targetMailbox.UidDeleteMessage(mailUID, true);
            this.currentOpenedMailbox.MessageCount--;
        }

        public void setAnsweredFlag(String mailbox, UInt64 gmMailID, Boolean isAnswered)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 mailUID = this.getMailUID(mailbox, gmMailID);
            if (isAnswered)
                targetMailbox.UidAddFlagsSilent(mailUID, new FlagCollection { "Answered" });
            else
                targetMailbox.UidRemoveFlagsSilent(mailUID, new FlagCollection { "Answered" });
        }
        public void setFlaggedFlag(String mailbox, UInt64 gmMailID, Boolean isFlagged)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 mailUID = this.getMailUID(mailbox, gmMailID);
            if (isFlagged)
                targetMailbox.UidAddFlagsSilent(mailUID, new FlagCollection { "Flagged" });
            else
                targetMailbox.UidRemoveFlagsSilent(mailUID, new FlagCollection { "Flagged" });
        }
        public void setDraftFlag(String mailbox, UInt64 gmMailID, Boolean isDraft)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 mailUID = this.getMailUID(mailbox, gmMailID);

            if (isDraft)
                targetMailbox.UidAddFlagsSilent(mailUID, new FlagCollection { "Draft" });
            else
                targetMailbox.UidRemoveFlagsSilent(mailUID, new FlagCollection { "Draft" });
        }
        public void setSeenFlag(String mailbox, UInt64 gmMailID, Boolean isSeen)
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
            try
            {
                this.currentOpenedMailbox = this.receiver.SelectMailbox(mailbox);
            }
            catch (Imap4Exception imapException)
            {
                throw new CouldNotSelectMailboxException("Nombre de mailbox incorrecto: " + mailbox + ".", imapException);
            }
            return this.currentOpenedMailbox;
        }
        private Mail FetchMail(Mailbox targetMailbox, Int32 mailOrdinal)
        {
            MailEntity retrievedMail = new MailEntity();
            Message retrievedMessage = new Message();
            int thisUid;
            String thisFlags;

            try
            {
                retrievedMessage = targetMailbox.Fetch.MessageObjectPeekWithGMailExtensions(mailOrdinal);
                retrievedMail.Gm_tid = UInt64.Parse(this.CleanIMAPResponse(receiver.Command("FETCH " + mailOrdinal + " (X-GM-THRID)"), "X-GM-THRID"));
                retrievedMail.Gm_mid = UInt64.Parse(this.CleanIMAPResponse(receiver.Command("FETCH " + mailOrdinal + " (X-GM-MSGID)"), "X-GM-MSGID"));
                thisUid = targetMailbox.Fetch.Uid(mailOrdinal);
                thisFlags = targetMailbox.Fetch.Flags(mailOrdinal).Merged;

            }
            catch (Imap4Exception exc)
            {
                //Excepcion de MailSystem contra IMAP
                Log logger = new Log(new LogEntity(001, "Error de MailSystem contra IMAP. Parametros del mail: mailbox("+targetMailbox.Name+"), ordinalMail("+mailOrdinal+").", exc.StackTrace));
                logger.Save();
                return null;
            }
            catch (FormatException exc)
            {
                //Excepcion intentado parsear la respuesta de imap a Int64
                Log logger = new Log(new LogEntity(001, "Error parseando la respuesta de imap a Int64. Parametros del mail: mailbox(" + targetMailbox.Name + "), ordinalMail(" + mailOrdinal + ").", exc.StackTrace));
                logger.Save();
                return null;
            }
            catch (Exception exc)
            {
                //Cualquier otra excepcion
                Log logger = new Log(new LogEntity(001, "Error generico. Parametros del mail: mailbox(" + targetMailbox.Name + "), ordinalMail(" + mailOrdinal + ").", exc.StackTrace));
                logger.Save();
                return null;
            }

            AddressEntity fromAddress = new AddressEntity();
            fromAddress.MailAddress = retrievedMessage.From.Email;
            fromAddress.Name = retrievedMessage.From.Name;

            retrievedMail.Subject = retrievedMessage.Subject;
            retrievedMail.Date = retrievedMessage.Date;
            retrievedMail.Body = retrievedMessage.BodyHtml.Text;
            if (retrievedMessage.BodyText.Text.Length >= 125)
                retrievedMail.BodyPeek = retrievedMessage.BodyText.Text.Substring(0, 125);
            else
                retrievedMail.BodyPeek = retrievedMessage.BodyText.Text.Substring(0, retrievedMessage.BodyText.Text.Length);
            retrievedMail.BodyPeek = System.Text.RegularExpressions.Regex.Replace(retrievedMail.BodyPeek, @"\s+", " ");
            retrievedMail.BodyPeek = retrievedMail.BodyPeek.Replace("\r", String.Empty);
            retrievedMail.From = fromAddress;

            Boolean unknownPartsHaveAttachments = false;
            //check if UnknownDispositionMimeParts has real attachments
            foreach (MimePart unknownPart in retrievedMessage.UnknownDispositionMimeParts)
            {
                if (unknownPart.Filename != "" && unknownPart.IsBinary)
                {
                    unknownPartsHaveAttachments = true;
                    break;
                }
            }
            retrievedMail.HasExtras = (retrievedMessage.Attachments.Count != 0
                                         || retrievedMessage.EmbeddedObjects.Count != 0
                                         || unknownPartsHaveAttachments);

            retrievedMail.ToAddr = this.GetAddressNameAndMail(retrievedMessage.To);
            retrievedMail.BCC = this.GetAddressNameAndMail(retrievedMessage.Bcc);
            retrievedMail.CC = this.GetAddressNameAndMail(retrievedMessage.Cc);

            this.AddUIDToMail(targetMailbox.Name, thisUid, retrievedMail);
            this.AddLabelsToMail(retrievedMessage.HeaderFields["x-gm-labels"], retrievedMail);
            this.AddFlagsToMail(targetMailbox.Fetch.Flags(mailOrdinal).Merged, retrievedMail);
            if (retrievedMail.HasExtras)
                this.loadAttachments(retrievedMail, retrievedMessage);
            return new Mail(retrievedMail);
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
                    this.accountMailboxesBySpecialProperty.Add("INBOX", "INBOX"); //nombre INBOX fijo por IMAP
                    continue;
                }
                if (mailbox.Contains("\\Trash"))
                {
                    this.accountMailboxesBySpecialProperty.Add("Trash", this.stripMailboxName(mailbox));
                    continue;
                }
                if (mailbox.Contains("\\Junk"))
                {
                    this.accountMailboxesBySpecialProperty.Add("Junk", this.stripMailboxName(mailbox));
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
                    this.accountMailboxesBySpecialProperty.Add("Flagged", this.stripMailboxName(mailbox));
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
                    this.accountMailboxesBySpecialProperty.Add("Tags", this.stripMailboxName(mailbox));
            }
        }
        private void loadAttachments(MailEntity mail, Message message)
        {
            ExtraEntity extra; //ExtraType: (0 = attachment) (1 = embbed object) (2 = unknown)

            foreach (MimePart attachment in message.Attachments)
            {
                extra = new ExtraEntity();
                extra.ExtraType = 0;
                extra.FileType = attachment.MimeType;
                extra.Name = attachment.Filename;
                extra.Size = (UInt32)attachment.Size;
                extra.Data = attachment.BinaryContent;
                extra.MailEntity = mail;
                mail.Extras.Add(extra);
            }

            foreach (MimePart embeddedPart in message.EmbeddedObjects)
            {
                if (embeddedPart.Filename != "")
                {
                    extra = new ExtraEntity();
                    extra.ExtraType = 1;
                    extra.FileType = embeddedPart.MimeType;
                    extra.Name = embeddedPart.Filename;
                    extra.Size = (UInt32)embeddedPart.Size;
                    extra.Data = embeddedPart.BinaryContent;
                    extra.MailEntity = mail;
                    extra.EmbObjectContentId = embeddedPart.ContentId;
                    mail.Extras.Add(extra);
                }
            }

            foreach (MimePart unknownPart in message.UnknownDispositionMimeParts)
            {
                if (unknownPart.Filename != "")
                {
                    extra = new ExtraEntity();
                    extra.ExtraType = 2;
                    extra.FileType = unknownPart.MimeType;
                    extra.Name = unknownPart.Filename;
                    extra.Size = (UInt32)unknownPart.Size;
                    extra.Data = unknownPart.BinaryContent;
                    extra.MailEntity = mail;
                    mail.Extras.Add(extra);
                }
            }
        }
        private void AddUIDToMail(String mailbox, Int64 UID, MailEntity mail)
        {
            //UIDs no cargados son completados por GlimpseDB como -1
            if (this.accountMailboxesBySpecialProperty["INBOX"] == mailbox)
            { mail.UidInbox = UID; return; }
            if (this.accountMailboxesBySpecialProperty["All"] == mailbox)
            { mail.UidAll = UID; return; }
            if (this.accountMailboxesBySpecialProperty["Trash"] == mailbox)
            { mail.UidTrash = UID; return; }
            if (this.accountMailboxesBySpecialProperty["Junk"] == mailbox)
            { mail.UidSpam = UID; return; }
            if (this.accountMailboxesBySpecialProperty["Sent"] == mailbox)
            { mail.UidSent = UID; return; }
            if (this.accountMailboxesBySpecialProperty["Drafts"] == mailbox)
            { mail.UidDraft = UID; return; }
        }
        private void AddFlagsToMail(String flags, MailEntity mail)
        {
            if (flags.Contains("answered"))
                mail.Answered = true;
            if (flags.Contains("flagged"))
                mail.Flagged = true;
            if (flags.Contains("seen"))
                mail.Seen = true;
            //if (flags.Contains("draft")) mail.Draft = true;
        }
        private void AddLabelsToMail(String gmLabels, MailEntity mail)
        {
            if (this.accountLabels == null) //Si no fueron updateados los labels del fetcher (con método setLabels())
                return;                     //no se puede marcar los mails con los labels (se necesita el LabelEntity)

            LabelEntity mailLabel = new LabelEntity();

            gmLabels = gmLabels.Replace("\\", String.Empty);
            gmLabels = gmLabels.Replace("\"", String.Empty);
            //Gmail no pone el label de la carpeta actual donde se encuentra el mail
            gmLabels += " " + this.currentOpenedMailbox.Name;

            String[] labelsNames = gmLabels.Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (String labelName in labelsNames)
            {
                mailLabel = (from accountLabel in this.accountLabels
                             where (accountLabel.Name == labelName || accountLabel.SystemName == labelName)
                             select accountLabel).SingleOrDefault<LabelEntity>();
                
                if (mailLabel != null)
                    mail.Labels.Add(mailLabel);
            }
        }
        private String GetAddressNameAndMail(AddressCollection addresses)
        {
            String addressesNamesAndMail = "";
            if (addresses.Count == 0)
                return addressesNamesAndMail;
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
        private String CleanOrdinalResponse(String imapResponse)
        {
            imapResponse = imapResponse.Remove(imapResponse.IndexOf("FETCH"));
            imapResponse = imapResponse.Substring(2, imapResponse.Length - 2);
            imapResponse = imapResponse.Trim();
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