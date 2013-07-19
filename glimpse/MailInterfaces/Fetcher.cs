using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ActiveUp.Net.Imap4;
using ActiveUp.Net.Mail;
using glimpse.Exceptions.MailInterfacesExceptions;

namespace glimpse.MailInterfaces
{
    public class Fetcher
    {
        private Imap4Client receiver;
        private Mailbox currentOpenedMailbox;

        public Fetcher(String username, String password)
        {
            this.receiver = new Connector().Login(username, password);
            this.currentOpenedMailbox = null;
        }

        public MessageCollection getInboxMails()
        {
            return this.getMails("INBOX", "ALL");
        }
        private MessageCollection getMails(String mailBox, String searchPhrase)
        {
            Mailbox mails = this.receiver.SelectMailbox(mailBox);
            Int32[] localMailsID = mails.Search("ALL");
            MessageCollection messages = new MessageCollection();

            for (int i = 0; i <= (localMailsID.Length -1); i++)
            {
                //messages.Add(mails.Fetch.MessageObject(localMailsID[i]));
                System.Diagnostics.Debug.WriteLine(mails.Fetch.MessageObject(localMailsID[i]).DateString);
            }

            this.receiver.Close();
            return messages;
        }

        public MessageCollection getAllMailsFrom(String mailbox)
        {
            return this.getLastXMailsFrom(mailbox, this.getAmountOfMailsFrom(mailbox));
        }
        public MessageCollection getLastXMailsFrom(String mailbox, Int32 amountToRetrieve)
        {
            return this.getMiddleMailsFrom(mailbox,amountToRetrieve, 0);
        }
        public MessageCollection getMiddleMailsFrom(String mailbox, Int32 amountToRetrieve, Int32 startingMailOrdinal)
        {
            Mailbox targetMailbox = this.getMailbox(mailbox);
            Int32 amountOfMails = targetMailbox.MessageCount;

            if (amountToRetrieve > amountOfMails - startingMailOrdinal)
            {
                throw new MailReadingOverflowException("Se está pidiendo obtener más mails de los que posee el mailbox o leer más allá del último mail.");
            }

            MessageCollection mailsRetrieved = new MessageCollection();

            //startingMailOrdinal: 0 representa el más reciente, a mayor valor, más antigüedad
            for (Int32 currentMail = amountOfMails - startingMailOrdinal;
                       currentMail > amountOfMails - startingMailOrdinal - amountToRetrieve;
                       currentMail--)
            {
                mailsRetrieved.Add(targetMailbox.Fetch.MessageObject(currentMail));
            }

            return mailsRetrieved;
        }
        public Int32 getAmountOfMailsFrom(String mailbox)
        {
            Mailbox targetMailbox = this.getMailbox(mailbox);
            return targetMailbox.MessageCount;
        }

        public String getBodyFromMail(String mailbox, Int32 uniqueMailID)
        {
            Mailbox targetMailbox = this.getMailbox(mailbox);
            //Devuelve el texto con los tags HTML del mail
            return targetMailbox.Fetch.UidMessageObject(uniqueMailID).BodyHtml.Text;
        }
        public byte[] getAttachmentFromMail(String mailbox, Int32 uniqueMailID, String attachmentName)
        {
            Mailbox targetMailbox = this.getMailbox(mailbox);
            AttachmentCollection attachmentsInMail = targetMailbox.Fetch.UidMessageObject(uniqueMailID).Attachments;
            Attachment desiredAttachment;
            try
            {
                desiredAttachment = attachmentsInMail.Cast<Attachment>()
                                              .Where<Attachment>(x => x.Filename == attachmentName)
                                              .Single<Attachment>();
            }
            catch (System.InvalidOperationException systemException)
            {
                throw new InvalidAttachmentException(systemException.Message ,"Se pide un archivo adjunto que no existe, o más de un adjunto posee el mismo nombre.");
            }
            return desiredAttachment.BinaryContent;
        }

        private Mailbox getMailbox(String targetMailboxName)
        {
            if (this.currentOpenedMailbox == null)
            {
                return this.openMailbox(targetMailboxName);
            }
            else
            {
                if (this.currentOpenedMailbox.ShortName == targetMailboxName)
                {
                    return this.currentOpenedMailbox;
                }
                else
                {
                    this.closeMailbox();
                    return this.openMailbox(targetMailboxName);
                }
            }
        }
        private Mailbox openMailbox(String mailbox)
        {
            this.currentOpenedMailbox = this.receiver.SelectMailbox(mailbox);
            return this.currentOpenedMailbox;
        }
        private void closeMailbox()
        {
            this.receiver.Close();
            this.currentOpenedMailbox = null;
        }

        
    }
}