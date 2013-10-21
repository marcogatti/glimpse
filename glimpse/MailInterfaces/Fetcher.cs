using ActiveUp.Net.Mail;
using Glimpse.DataAccessLayer.Entities;
using Glimpse.Exceptions;
using Glimpse.Exceptions.MailInterfacesExceptions;
using Glimpse.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Glimpse.MailInterfaces
{
    public class Fetcher : IDisposable
    {
        private Imap4Client Receiver;
        private Mailbox CurrentOpenedMailbox;
        private NameValueCollection AccountMailboxesBySpecialProperty;
        private IList<LabelEntity> AccountLabels;

        public Fetcher(String username, String password)
        {
            this.Receiver = new Connector().ImapLogin(username, password);
            this.AccountMailboxesBySpecialProperty = new NameValueCollection();
            this.LoadMailboxesAndSpecialProperties(this.Receiver.Command("LIST \"\" \"*\""));
            this.CurrentOpenedMailbox = null;
        }
        public void SetLabels(IList<LabelEntity> labels)
        {
            this.AccountLabels = labels;
        }
        public bool IsConnected()
        {
            return this.Receiver.IsConnected;
        }
        public bool IsFullyConnected()
        {
            return this.AccountLabels != null && this.IsConnected();
        }
        public NameValueCollection GetLabels()
        {
            return this.AccountMailboxesBySpecialProperty;
        }

        public Int32 GetLimitUIDFrom(String mailbox, Boolean max)
        {
            if (max) //maximo
            {
                Mailbox targetMailbox = this.OpenMailbox(mailbox);
                Int32 amountOfMails = targetMailbox.MessageCount;
                if (amountOfMails == 0) return 0;
                return targetMailbox.Fetch.Uid(amountOfMails);
            }
            else //minimo
            {
                Mailbox targetMailbox = this.OpenMailbox(mailbox);
                if (this.CurrentOpenedMailbox.MessageCount == 0) return 0;
                return targetMailbox.Fetch.Uid(1);
            }
        }
        public Int32 GetAmountOfMailsFrom(String mailbox)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            return targetMailbox.MessageCount;
        }
        public Int32 GetMailUID(String mailbox, UInt64 gmMailID)
        {
            this.GetMailbox(mailbox); //Se asegura que se encuentra seleccionado el mailbox en IMAP
            String uidString = this.CleanIMAPResponse(this.Receiver.Command("UID SEARCH X-GM-MSGID " + gmMailID.ToString()), "SEARCH", false);
            Int32 uid;
            if (Int32.TryParse(uidString, out uid))
                return uid;
            else
                throw new GlimpseException("El email con gmID: " + gmMailID + " no se encuentra en la carpeta " + mailbox + ".");
        }
        public DateTime GetOldestMailDate(Int32 oldestMessageCount)
        {
            //int oldestOrdinal;
            //Mailbox targetMailbox = this.GetMailbox(this.AccountMailboxesBySpecialProperty["All"]);
            //if (this.CurrentOpenedMailbox.MessageCount == 0)
            //    return DateTime.Today;
            //if (this.CurrentOpenedMailbox.MessageCount >= oldestMessageCount)
            //    oldestOrdinal = this.CurrentOpenedMailbox.MessageCount - oldestMessageCount;
            //else
            //    oldestOrdinal = 1;
            //var nvCol = targetMailbox.Fetch.HeaderLines(oldestOrdinal, new String[] { "date" });
            //String dateString = nvCol["date"];
            //var formatStrings = new string[] { "ddd, d MMM yyyy HH:mm:ss zzz", "ddd, d MMM yyyy HH:mm:ss zzzz",
            //                                   "ddd, dd MMM yyyy HH:mm:ss zzz", "ddd, dd MMM yyyy HH:mm:ss zzzz" };
            //DateTime returnDateTime;
            //if (dateString.Contains("(PDT)"))
            //    dateString = dateString.Remove(dateString.IndexOf("(PDT)") - 1);
            //if (dateString.Contains("(GMT)"))
            //    dateString = dateString.Remove(dateString.IndexOf("(GMT)") - 1);
            //if (dateString.Contains("(CEST)"))
            //    dateString = dateString.Remove(dateString.IndexOf("(CEST)") - 1);
            //if (dateString.Contains("(UTC)"))
            //    dateString = dateString.Remove(dateString.IndexOf("(UTC)") - 1);
            //if (DateTime.TryParseExact(dateString, formatStrings, new CultureInfo("en-US"), DateTimeStyles.None, out returnDateTime))
            //    return returnDateTime;
            //else
                return DateTime.Today.AddYears(-1);
        }

        #region Mail Retrieving Methods
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
                mailOrdinalOfMinimumUID = Int32.Parse(this.CleanOrdinalResponse(this.Receiver.Command("UID FETCH " + minimumUID + " UID")));
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
            List<Mail> retrievedMails = new List<Mail>();
            Int32[] mailsUIDs;
            Mail retrievedMail;

            mailsUIDs = this.CleanSearchResponse(this.Receiver.Command("SEARCH UID " + firstUID + ":" + lastUID));
         
            foreach (Int32 currentMailOrdinal in mailsUIDs)
            {
                retrievedMail = this.FetchMail(targetMailbox, currentMailOrdinal);
                if (retrievedMail != null)
                    retrievedMails.Add(retrievedMail);
            }
            return retrievedMails;
        }
        public byte[] GetAttachmentFromMail(String mailbox, UInt64 gmMailID, String attachmentName)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 mailUID = this.GetMailUID(mailbox, gmMailID);
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
        #endregion

        #region Folder Methods
        public void ArchiveMail(UInt64 gmMailID)
        {
            this.RemoveMailTag("INBOX", gmMailID);
        }
        public void RemoveMailTag(String mailbox, UInt64 gmMailID)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 mailUID = this.GetMailUID(mailbox, gmMailID);
            targetMailbox.UidMoveMessage(mailUID, this.AccountMailboxesBySpecialProperty["All"]);
            this.CurrentOpenedMailbox.MessageCount--;
        }
        public void AddMailTag(String mailbox, String tagToAdd, UInt64 gmMailID)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 mailUID = this.GetMailUID(mailbox, gmMailID);
            targetMailbox.UidCopyMessage(mailUID, tagToAdd);
        }
        public void MoveToTrash(String mailbox, UInt64 gmMailID)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 mailUID = this.GetMailUID(mailbox, gmMailID);
            targetMailbox.UidMoveMessage(mailUID, this.AccountMailboxesBySpecialProperty["Trash"]);
            this.CurrentOpenedMailbox.MessageCount--;
        }
        public void RemoveFromTrash(String destinyMailbox, UInt64 gmMailID)
        {
            Mailbox targetMailbox = this.GetMailbox(this.AccountMailboxesBySpecialProperty["Trash"]);
            Int32 mailUID = this.GetMailUID(this.AccountMailboxesBySpecialProperty["Trash"], gmMailID);
            targetMailbox.UidMoveMessage(mailUID, destinyMailbox);
            this.CurrentOpenedMailbox.MessageCount--;
        }
        public void DeleteFromTrash(UInt64 gmMailID)
        {
            Mailbox targetMailbox = this.GetMailbox(this.AccountMailboxesBySpecialProperty["Trash"]);
            Int32 mailUID = this.GetMailUID(this.AccountMailboxesBySpecialProperty["Trash"], gmMailID);
            targetMailbox.UidDeleteMessage(mailUID, true);
            this.CurrentOpenedMailbox.MessageCount--;
        }
        public void CreateLabel(String labelName)
        {
            this.Receiver.CreateMailbox(labelName);
            this.AddNewLabel(labelName);
        }
        public void RenameLabel(String oldLabelName, String newLabelName)
        {
            Mailbox targetMailbox = this.GetMailbox(oldLabelName);
            targetMailbox.Rename(newLabelName);
            this.ReplaceLabelName(oldLabelName, newLabelName);
        }
        public void DeleteLabel(String labelName)
        {
            Mailbox targetMailbox = this.GetMailbox(labelName);
            targetMailbox.Delete();
            this.RemoveLabel(labelName);
        }
        #endregion

        #region Flag Methods
        public void SetAnsweredFlag(String mailbox, UInt64 gmMailID, Boolean isAnswered)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 mailUID = this.GetMailUID(mailbox, gmMailID);
            if (isAnswered)
                targetMailbox.UidAddFlagsSilent(mailUID, new FlagCollection { "Answered" });
            else
                targetMailbox.UidRemoveFlagsSilent(mailUID, new FlagCollection { "Answered" });
        }
        public void SetFlaggedFlag(String mailbox, UInt64 gmMailID, Boolean isFlagged)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 mailUID = this.GetMailUID(mailbox, gmMailID);
            if (isFlagged)
                targetMailbox.UidAddFlagsSilent(mailUID, new FlagCollection { "Flagged" });
            else
                targetMailbox.UidRemoveFlagsSilent(mailUID, new FlagCollection { "Flagged" });
        }
        public void SetDraftFlag(String mailbox, UInt64 gmMailID, Boolean isDraft)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 mailUID = this.GetMailUID(mailbox, gmMailID);

            if (isDraft)
                targetMailbox.UidAddFlagsSilent(mailUID, new FlagCollection { "Draft" });
            else
                targetMailbox.UidRemoveFlagsSilent(mailUID, new FlagCollection { "Draft" });
        }
        public void SetSeenFlag(String mailbox, UInt64 gmMailID, Boolean isSeen)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 mailUID = this.GetMailUID(mailbox, gmMailID);
            if (isSeen)
                targetMailbox.UidAddFlagsSilent(mailUID, new FlagCollection { "Seen" });
            else
                targetMailbox.UidRemoveFlagsSilent(mailUID, new FlagCollection { "Seen" });
        }
        #endregion

        public void CloseClient()
        {
            try
            {
                this.Receiver.Disconnect();
            }
            catch (IOException exc) //imap cerro la conexion por algun motivo
            {
                Log.LogException(exc);
            }
        }
        public void Dispose()
        {
            if (this.IsConnected()) this.CloseClient();
        }

        #region Private Methods
        private Mailbox GetMailbox(String targetMailboxName)
        {
            if (this.CurrentOpenedMailbox == null)
            {
                return this.OpenMailbox(targetMailboxName);
            }
            else
            {
                if (this.CurrentOpenedMailbox.Name == targetMailboxName)
                {
                    return this.CurrentOpenedMailbox;
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
                this.CurrentOpenedMailbox = this.Receiver.SelectMailbox(mailbox);
            }
            catch (Imap4Exception imapException)
            {
                throw new CouldNotSelectMailboxException("Nombre de mailbox incorrecto: " + mailbox + ".", imapException);
            }
            return this.CurrentOpenedMailbox;
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
                retrievedMail.Gm_tid = UInt64.Parse(this.CleanIMAPResponse(Receiver.Command("FETCH " + mailOrdinal + " (X-GM-THRID)"), "X-GM-THRID"));
                retrievedMail.Gm_mid = UInt64.Parse(this.CleanIMAPResponse(Receiver.Command("FETCH " + mailOrdinal + " (X-GM-MSGID)"), "X-GM-MSGID"));
                thisUid = targetMailbox.Fetch.Uid(mailOrdinal);
                thisFlags = targetMailbox.Fetch.Flags(mailOrdinal).Merged;
            }
            catch (Exception exc)
            {
                Log.LogException(exc, "Error al traer un mail con IMAP. Parametros: mailbox: " + targetMailbox.Name + ", mailOrdinal: " + mailOrdinal.ToString() + ".");
                return null;
            }

            AddressEntity fromAddress = new AddressEntity();
            fromAddress.MailAddress = retrievedMessage.From.Email;
            fromAddress.Name = retrievedMessage.From.Name;

            retrievedMail.Subject = retrievedMessage.Subject ?? "";
            retrievedMail.Date = retrievedMessage.Date;
            if (retrievedMessage.BodyHtml.Text != "")
                retrievedMail.Body = retrievedMessage.BodyHtml.Text;
            else
                retrievedMail.Body = retrievedMessage.BodyText.Text;

            string shortBody = retrievedMessage.BodyHtml.TextStripped;
            shortBody = Regex.Replace(shortBody, @"\s+", " ");

            if (shortBody.Length > 80)
                retrievedMail.BodyPeek = shortBody.Substring(0, 80);
            else
                retrievedMail.BodyPeek = shortBody;

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

            retrievedMail.ToAddress = this.GetAddressNameAndMail(retrievedMessage.To);
            retrievedMail.BCC = this.GetAddressNameAndMail(retrievedMessage.Bcc);
            retrievedMail.CC = this.GetAddressNameAndMail(retrievedMessage.Cc);

            this.AddUIDToMail(targetMailbox.Name, thisUid, retrievedMail);
            this.AddLabelsToMail(retrievedMessage.HeaderFields["x-gm-labels"], retrievedMail);
            this.AddFlagsToMail(targetMailbox.Fetch.Flags(mailOrdinal).Merged, retrievedMail);
            if (retrievedMail.Labels.Any(x => x.SystemName == "Important"))
                retrievedMail.Importance = 4;
            else
                retrievedMail.Importance = 2;
            if (retrievedMail.HasExtras)
                this.LoadAttachments(retrievedMail, retrievedMessage);
            return new Mail(retrievedMail);
        }
        private void CloseMailbox()
        {
            this.Receiver.Close();
            this.CurrentOpenedMailbox = null;
        }
        private void LoadMailboxesAndSpecialProperties(string imapLISTResponse)
        {
            /*imapResponse del tipo (incluyendo \r\n):
              (\HasNoChildren) "INBOX"
              (\Noselect \HasChildren) "[Gmail]"
              (\HasNoChildren \Drafts) "[Gmail]/Borradores"
              (\HasNoChildren \All) "[Gmail]/Todos"*/
            imapLISTResponse = imapLISTResponse.Replace("* LIST", String.Empty).Replace(" \"/\"", String.Empty);
            String[] mailboxes = imapLISTResponse.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (String mailbox in mailboxes)
            {
                if (mailbox.Contains("\\All"))
                {
                    this.AccountMailboxesBySpecialProperty.Add("All", this.StripMailboxName(mailbox));
                    continue;
                }
                if (mailbox.Contains("INBOX"))
                {
                    this.AccountMailboxesBySpecialProperty.Add("INBOX", "INBOX"); //nombre INBOX fijo por IMAP
                    continue;
                }
                if (mailbox.Contains("\\Trash"))
                {
                    this.AccountMailboxesBySpecialProperty.Add("Trash", this.StripMailboxName(mailbox));
                    continue;
                }
                if (mailbox.Contains("\\Junk"))
                {
                    this.AccountMailboxesBySpecialProperty.Add("Junk", this.StripMailboxName(mailbox));
                    continue;
                }
                if (mailbox.Contains("\\Important"))
                {
                    this.AccountMailboxesBySpecialProperty.Add("Important", this.StripMailboxName(mailbox));
                    continue;
                }
                if (mailbox.Contains("\\Sent"))
                {
                    this.AccountMailboxesBySpecialProperty.Add("Sent", this.StripMailboxName(mailbox));
                    continue;
                }
                if (mailbox.Contains("\\Flagged"))
                {
                    this.AccountMailboxesBySpecialProperty.Add("Flagged", this.StripMailboxName(mailbox));
                    continue;
                }
                if (mailbox.Contains("\\Drafts"))
                {
                    this.AccountMailboxesBySpecialProperty.Add("Drafts", this.StripMailboxName(mailbox));
                    continue;
                }
                if (mailbox.Contains("OK Success") || mailbox.Contains("Noselect"))
                    continue;
                else
                    this.AccountMailboxesBySpecialProperty.Add("Tags", this.StripMailboxName(mailbox));
            }
        }
        private void LoadAttachments(MailEntity mail, Message message)
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
                    extra.EmbObjectContentId = this.TrimAngularBrackets(embeddedPart.ContentId);
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
                    extra.EmbObjectContentId = this.TrimAngularBrackets(unknownPart.ContentId);
                    extra.MailEntity = mail;

                    mail.Extras.Add(extra);
                }
            }
        }
        private void AddNewLabel(String labelName)
        {
            if (this.AccountMailboxesBySpecialProperty["Tags"] == "")
                this.AccountMailboxesBySpecialProperty["Tags"] = labelName;
            else if(!this.AccountMailboxesBySpecialProperty["Tags"].Split(',').Any(x => x == labelName))
                this.AccountMailboxesBySpecialProperty["Tags"] += "," + labelName;

            if (!this.AccountLabels.Any(x => x.Name == labelName))
                this.AccountLabels.Add(new LabelEntity(labelName, null));
        }
        private void ReplaceLabelName(String oldLabel, String newLabel)
        {
            this.AccountMailboxesBySpecialProperty["Tags"] = this.AccountMailboxesBySpecialProperty["Tags"].Replace(oldLabel, newLabel);
            foreach (LabelEntity accountLabel in this.AccountLabels.Where(x => x.Name.Contains(oldLabel)))
                accountLabel.Name = accountLabel.Name.Replace(oldLabel, newLabel);
        }
        private void RemoveLabel(String labelName)
        {
            String tags = this.AccountMailboxesBySpecialProperty["Tags"];
            if (tags == null || tags == "")
                return;
            else if (tags == labelName)
                this.AccountMailboxesBySpecialProperty["Tags"] = String.Empty;
            else
            {
                String[] individualTags = tags.Split(',');
                String newTagString = "";
                foreach (String individualTag in individualTags)
                {
                    if (individualTag == labelName)
                        continue;
                    else
                        newTagString += individualTag + ',';
                }
                this.AccountMailboxesBySpecialProperty["Tags"] = newTagString.Trim(',');
            }

            if (this.AccountLabels != null)
                this.AccountLabels.Remove(this.AccountLabels.Where(x => x.Name == labelName).Single());
        }
        private void AddUIDToMail(String mailbox, Int64 UID, MailEntity mail)
        {
            //UIDs no cargados son completados por GlimpseDB como -1
            if (this.AccountMailboxesBySpecialProperty["All"] == mailbox)
            { mail.UidAll = UID; return; }
            if (this.AccountMailboxesBySpecialProperty["Trash"] == mailbox)
            { mail.UidTrash = UID; return; }
            if (this.AccountMailboxesBySpecialProperty["Junk"] == mailbox)
            { mail.UidSpam = UID; return; }
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
            if (this.AccountLabels == null) //Si no fueron updateados los labels del fetcher (con método setLabels())
                return;                     //no se puede marcar los mails con los labels (se necesita el LabelEntity)

            LabelEntity mailLabel = new LabelEntity();

            gmLabels = gmLabels.Replace("\\", String.Empty);
            gmLabels = gmLabels.Replace("\"", String.Empty);
            //Gmail no pone el label de la carpeta actual donde se encuentra el mail
            gmLabels += " " + this.CurrentOpenedMailbox.Name;

            String[] labelsNames = gmLabels.Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (String labelName in labelsNames)
            {
                mailLabel = (from accountLabel in this.AccountLabels
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
            labels += " " + this.CurrentOpenedMailbox.Name;

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
        private String StripMailboxName(String mailbox)
        {
            //Input: (\HasNoChildren \Drafts) "[Gmail]/Borradores"
            //Output: [Gmail]/Borradores
            String mailboxName = mailbox.Substring(mailbox.IndexOf('"') + 1, mailbox.LastIndexOf('"') - mailbox.IndexOf('"') - 1);
            return mailboxName;
        }
        private String TrimAngularBrackets(String phrase)
        {
            if (phrase == null)
                return null;
            phrase = phrase.Remove(0, 1);
            return phrase.Remove(phrase.Length - 1, 1);
        }
        private Int32[] CleanSearchResponse(String imapResponse)
        {
            //"130926123033355 OK SEARCH completed (Success)\r\n* SEARCH 5 6 19 151 251\r\n"
            imapResponse = imapResponse.Remove(0, imapResponse.LastIndexOf("SEARCH") + 7);
            imapResponse = imapResponse.Replace("\r\n", String.Empty);
            if(imapResponse == "\n") //si no devolvio numeros
                return new Int32[0];
            String[] numbersStrings = imapResponse.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            Int32[] returnNumbers =  numbersStrings.Select(x => Int32.Parse(x)).ToArray();
            return returnNumbers;
        }
        #endregion
    }
}