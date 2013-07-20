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
            return this.getAllMailsFrom("INBOX");
        }
        public MessageCollection getAllMailsFrom(String mailbox)
        {
            Mailbox targetMail = this.getMailbox(mailbox);
            Int32 amountOfMails = targetMail.MessageCount;
            MessageCollection messages = new MessageCollection();

            for (int i = amountOfMails; i > 0; i--)
            {
                messages.Add(targetMail.Fetch.MessageObject(i));
            }

            return messages;
        }

        public Int32 getAmountOfMailsFrom(String mailbox)
        {
            Mailbox targetMailbox = this.getMailbox(mailbox);
            return targetMailbox.MessageCount;
        }
        public HeaderCollection getAllHeadersFrom(String mailbox)
        {
            return this.getLastXHeadersFrom(mailbox, this.getAmountOfMailsFrom(mailbox));
        }
        public HeaderCollection getLastXHeadersFrom(String mailbox, Int32 amountToRetrieve)
        {
            return this.getMiddleHeadersFrom(mailbox,amountToRetrieve, 0);
        }
        public HeaderCollection getMiddleHeadersFrom(String mailbox, Int32 amountToRetrieve, Int32 startingMailOrdinal)
        {
            Mailbox targetMailbox = this.getMailbox(mailbox);
            Int32 amountOfMails = targetMailbox.MessageCount;

            if (amountToRetrieve > amountOfMails - startingMailOrdinal)
            {
                throw new MailReadingOverflowException("Se está pidiendo obtener más mails de los que posee el mailbox o leer más allá del último mail.");
            }

            HeaderCollection headersRetrieved = new HeaderCollection();

            //startingMailOrdinal: 0 representa el más reciente. A mayor valor, más antigüedad.
            for (Int32 currentMail = amountOfMails - startingMailOrdinal;
                       currentMail > amountOfMails - startingMailOrdinal - amountToRetrieve;
                       currentMail--)
            {
                headersRetrieved.Add(targetMailbox.Fetch.HeaderObject(currentMail));
            }
            return headersRetrieved;
        }
        public Int32[] getAllUIDsFrom(String mailbox)
        {
            Mailbox targetMailbox = this.getMailbox(mailbox);
            Int32 amountOfMails = targetMailbox.MessageCount;
            Int32[] UIDList = new Int32[amountOfMails];

            for (Int32 currentMail = amountOfMails; currentMail > 0; currentMail--)
            {
                UIDList[amountOfMails-currentMail] = targetMailbox.Fetch.Uid(currentMail);
            }
            return UIDList; //UIDList[0] representa el mail más reciente. A mayor valor, mayor antigüedad.
        }

        public String getBodyFromMail(String mailbox, Int32 uniqueMailID)
        {
            Mailbox targetMailbox = this.getMailbox(mailbox);
            //Devuelve el texto con los tags HTML del mail
            return targetMailbox.Fetch.UidMessageObject(uniqueMailID).BodyHtml.Text;
        }
        public Header getHeaderFromMail(String mailbox, Int32 uniqueMailID)
        {
            Mailbox targetMailbox = this.getMailbox(mailbox);
            return targetMailbox.Fetch.UidHeaderObject(uniqueMailID);
        }
        public Message getSpecificMail(String mailbox, Int32 uniqueMailID)
        {
            Mailbox targetMailbox = this.getMailbox(mailbox);
            return targetMailbox.Fetch.UidMessageObject(uniqueMailID);
        }
        public byte[] getAttachmentFromMail(String mailbox, Int32 uniqueMailID, String attachmentName)
        {
            Mailbox targetMailbox = this.getMailbox(mailbox);
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