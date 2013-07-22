using System;
using NUnit.Framework;
using ActiveUp.Net.Imap4;
using glimpse.MailInterfaces;
using glimpse.Exceptions.MailInterfacesExceptions;
using ActiveUp.Net.Mail;
using System.IO;
using glimpse.Tests.global;

namespace glimpse.Tests
{
    [TestFixture]
    public class FetcherTest
    {
        Fetcher myFetcher;

        [SetUp]
        public void setUp()
        {
            myFetcher = new Fetcher("imap.sealed@gmail.com", "imapsealed");
        }

        [Test]
        public void get_Inbox_Retrieves_Mails_From_Account()
        {
            Assert.IsNotEmpty(myFetcher.getInboxMails());
        }

        [Test]
        public void get_Amount_Of_Mails_Returns_Correct_Amount()
        {
            Assert.AreEqual(13, this.myFetcher.getAmountOfMailsFrom("INBOX"));
            Assert.AreEqual(11, this.myFetcher.getAmountOfMailsFrom("[Gmail]/Importantes"));
        }
        [Test]
        public void get_Body_Returns_Full_Text()
        {
            Int32[] mailsUIDs = this.myFetcher.getAllUIDsFrom("INBOX");
            String mailNumber8Body = this.myFetcher.getBodyFromMail("INBOX", mailsUIDs[8]);

            //texto retornado en formato HTML
            Assert.True(mailNumber8Body.Contains("<div dir=\"ltr\"><p class=\"\" style"));
            Assert.True(mailNumber8Body.Contains("/span>Datos para"));
            Assert.True(mailNumber8Body.Contains("para la red."));
        }
        [Test]
        public void get_All_Headers_Returns_Correct_Headers()
        {
            HeaderCollection allHeaders = this.myFetcher.getAllHeadersFrom("INBOX");

            Assert.AreEqual(13, allHeaders.Count);
            Assert.AreEqual("test.imap.505@gmail.com", allHeaders[7].To[1].Email);
        }
        [Test]
        public void get_Middle_Headers_Returns_Correct_Headers()
        {
            HeaderCollection targetHeaders = this.myFetcher.getMiddleHeadersFrom("INBOX", 6, 3);

            Assert.AreEqual("Email9", targetHeaders[0].Subject);
            Assert.AreEqual("Email4", targetHeaders[5].Subject);
            Assert.AreEqual(6, targetHeaders.Count);
        }
        [Test]
        [ExpectedException(typeof(MailReadingOverflowException))]
        public void reading_More_Than_Possible_Throws_Overflow_Exception()
        {
            this.myFetcher.getMiddleHeadersFrom("INBOX", 4, 10);
        }
        [Test]
        [ExpectedException(typeof(InvalidAttachmentException))]
        public void geting_Non_Existant_Attachment_Throws_Invalid_Attachment_Exception()
        {
            Int32[] inboxUIDs = this.myFetcher.getAllUIDsFrom("INBOX");
            this.myFetcher.getAttachmentFromMail("INBOX", inboxUIDs[9], "wrongAttachmentName.jpg");
        }
        [Test]
        public void get_Attachment_Downloads_Full_File()
        {
            String tempFilePath = AutoTests.getProjectRootDirectory() + "/MailInterfaces/DownloadedEagles.bmp";
            Int32[] inboxUIDs = this.myFetcher.getAllUIDsFrom("INBOX");
            Message mail = this.myFetcher.getSpecificMail("INBOX", inboxUIDs[2]);
            Byte[] downloadedAttachment = this.myFetcher.getAttachmentFromMail("INBOX", inboxUIDs[2], "Eagles.bmp");
            File.WriteAllBytes(tempFilePath, downloadedAttachment);
            FileInfo fileOnDisk = new FileInfo(tempFilePath);
            Int64 sizeOfFile = fileOnDisk.Length;
            fileOnDisk.Delete();
            Assert.AreEqual(mail.Attachments[0].Size, sizeOfFile);
        }
        [Test]
        public void fetcher_Selects_Mailboxes_Correctly()
        {
            Header inboxLastMailHeader = this.myFetcher.getLastXHeadersFrom("INBOX", 1)[0];
            Header sentLastMailHeader = this.myFetcher.getLastXHeadersFrom("[Gmail]/Enviados", 1)[0];
            Header deletedLastMailHeader = this.myFetcher.getLastXHeadersFrom("[Gmail]/Papelera", 1)[0];

            Assert.AreEqual("Re: Email11 c/ texto formateado", inboxLastMailHeader.Subject);
            Assert.AreEqual(56, sentLastMailHeader.Date.Minute);
            Assert.AreEqual("Email8A para ser borrado", deletedLastMailHeader.Subject);
        }
    }
}
