using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ActiveUp.Net.Imap4;
using ActiveUp.Net.Mail;
using Glimpse.Exceptions.MailInterfacesExceptions;

namespace Glimpse.MailInterfaces
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
                messages.Add(targetMail.Fetch.MessageObject(i));
            }

            return messages;
        }

        public Int32 GetAmountOfMailsFrom(String mailbox)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            return targetMailbox.MessageCount;
        }

        public HeaderCollection GetAllHeadersFrom(String mailbox)
        {
            return this.GetLastXHeadersFrom(mailbox, this.GetAmountOfMailsFrom(mailbox));
        }

        public HeaderCollection GetLastXHeadersFrom(String mailbox, Int32 amountToRetrieve)
        {
            return this.GetMiddleHeadersFrom(mailbox,amountToRetrieve, 0);
        }

        public HeaderCollection GetMiddleHeadersFrom(String mailbox, Int32 amountToRetrieve, Int32 startingMailOrdinal)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
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

        public Int32[] GetAllUIDsFrom(String mailbox)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            Int32 amountOfMails = targetMailbox.MessageCount;
            Int32[] UIDList = new Int32[amountOfMails];

            for (Int32 currentMail = amountOfMails; currentMail > 0; currentMail--)
            {
                UIDList[amountOfMails-currentMail] = targetMailbox.Fetch.Uid(currentMail);
            }

            return UIDList; //UIDList[0] representa el mail más reciente. A mayor valor, mayor antigüedad.
        }

        public String GetBodyFromMail(String mailbox, Int32 uniqueMailID)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            //Devuelve el texto con los tags HTML del mail
            return targetMailbox.Fetch.UidMessageObject(uniqueMailID).BodyHtml.Text;
        }

        public Header GetHeaderFromMail(String mailbox, Int32 uniqueMailID)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            return targetMailbox.Fetch.UidHeaderObject(uniqueMailID);
        }

        public Message GetSpecificMail(String mailbox, Int32 uniqueMailID)
        {
            Mailbox targetMailbox = this.GetMailbox(mailbox);
            return targetMailbox.Fetch.UidMessageObject(uniqueMailID);
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

        private Mailbox GetMailbox(String targetMailboxName)
        {
            if (this.currentOpenedMailbox == null)
            {
                return this.OpenMailbox(targetMailboxName);
            }
            else
            {
                if (this.currentOpenedMailbox.ShortName == targetMailboxName)
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
    }
}