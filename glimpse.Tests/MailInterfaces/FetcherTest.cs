using System;
using NUnit.Framework;
using ActiveUp.Net.Imap4;
using Glimpse.MailInterfaces;
using Glimpse.Exceptions.MailInterfacesExceptions;
using ActiveUp.Net.Mail;
using System.IO;
using Glimpse.Tests.global;
using Glimpse.DataAccessLayer.Entities;
using Glimpse.Models;

namespace Glimpse.Tests
{
    [TestFixture]
    public class FetcherTest
    {
        Fetcher myFetcher;

        [SetUp]
        public void SetUp()
        {
            this.myFetcher = new Fetcher("imap.sealed@gmail.com", "imapsealed");
        }

        [TearDown]
        public void TearDown()
        {
            this.myFetcher.CloseClient();
        }

        [Test]
        public void Get_Inbox_Retrieves_Mails_From_Account()
        {
            Assert.IsNotEmpty(myFetcher.GetInboxMails());
        }

        [Test]
        public void Get_Amount_Of_Mails_Returns_Correct_Amount()
        {
            Assert.AreEqual(13, this.myFetcher.GetAmountOfMailsFrom("INBOX"));
            Assert.AreEqual(11, this.myFetcher.GetAmountOfMailsFrom("[Gmail]/Importantes"));
        }

        [Test]
        public void Get_Last_UID_Returns_Correct_Number()
        {
            Assert.AreEqual(46, this.myFetcher.GetLastUIDFrom("[Gmail]/Borradores"));
            Assert.AreEqual(13, this.myFetcher.GetLastUIDFrom("[Gmail]/Enviados"));
        }

        [Test]
        public void Fetcher_Loads_Data_Correctly()
        {
            MailCollection retrievedMails = new MailCollection();
            retrievedMails = this.myFetcher.GetMailsDataFrom("INBOX", 3);

            Assert.AreEqual("Re: Email11 c/ texto formateado", retrievedMails[0].Subject);
            Assert.AreEqual(DateTime.Parse("Sat, 20 Jul 2013 00:52:44"), retrievedMails[1].Date);
            Assert.AreEqual("Martin Hoomer", retrievedMails[2].From.Name);
            Assert.IsTrue(retrievedMails[3].Body.Contains("</span><span class=\"\">Estrategia de entrenamiento.</span></p>"));
            Assert.IsTrue(retrievedMails[4].HasExtras);
            Assert.AreEqual(11, retrievedMails[5].UidInbox);
            Assert.IsTrue(retrievedMails[6].UidDraft <= 0 && retrievedMails[6].UidSent <= 0 && retrievedMails[6].UidSpam <= 0);
            Assert.IsTrue(retrievedMails[7].ToAddr.Contains("imap.sealed@gmail.com") && retrievedMails[7].ToAddr.Contains("test.imap.505@gmail.com"));
            Assert.IsTrue(retrievedMails[0].Flagged && retrievedMails[3].Seen && !retrievedMails[0].Seen);
        }
    }
}
