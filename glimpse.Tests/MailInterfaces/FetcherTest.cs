using ActiveUp.Net.Imap4;
using ActiveUp.Net.Mail;
using Glimpse.DataAccessLayer.Entities;
using Glimpse.Exceptions.MailInterfacesExceptions;
using Glimpse.MailInterfaces;
using Glimpse.Models;
using Glimpse.Tests.global;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace Glimpse.Tests
{
    [TestFixture]
    public class FetcherTest
    {
        Fetcher myFetcher;

        [TestFixtureSetUp]
        public void SetUp()
        {
            this.myFetcher = new Fetcher("imap.sealed@gmail.com", "imapsealed");
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            this.myFetcher.CloseClient();
        }

        [Test]
        public void Get_Amount_Of_Mails_Returns_Correct_Amount()
        {
            Assert.AreEqual(13, this.myFetcher.GetAmountOfMailsFrom("INBOX"));
            Assert.AreEqual(11, this.myFetcher.GetAmountOfMailsFrom("[Gmail]/Importantes"));
        }

        [Test]
        public void Fetcher_Loads_Data_Correctly()
        {
            List<Mail> retrievedMails = new List<Mail>();
            retrievedMails = this.myFetcher.GetMailsDataFrom("INBOX", 3);

            Assert.AreEqual("Re: Email11 c/ texto formateado", retrievedMails[0].Entity.Subject);
            Assert.AreEqual(DateTime.Parse("Sat, 20 Jul 2013 00:52:44"), retrievedMails[1].Entity.Date);
            Assert.AreEqual("Martin Hoomer", retrievedMails[2].Entity.From.Name);
            Assert.IsTrue(retrievedMails[3].Entity.Body.Contains("</span><span class=\"\">Estrategia de entrenamiento.</span></p>"));
            Assert.IsTrue(retrievedMails[4].Entity.HasExtras);
            Assert.AreEqual(11, retrievedMails[5].Entity.UidInbox);
            Assert.IsTrue(retrievedMails[6].Entity.UidDraft <= 0 && retrievedMails[6].Entity.UidSent <= 0 && retrievedMails[6].Entity.UidSpam <= 0);
            Assert.IsTrue(retrievedMails[7].Entity.ToAddr.Contains("imap.sealed@gmail.com") && retrievedMails[7].Entity.ToAddr.Contains("test.imap.505@gmail.com"));
            Assert.IsTrue(retrievedMails[0].Entity.Flagged && retrievedMails[3].Entity.Seen && !retrievedMails[0].Entity.Seen);
        }

        [Test]
        public void Mail_Tagging_Operations_Work_Correctly()
        {
            Int32 startingMailsAmount = this.myFetcher.GetAmountOfMailsFrom("MyLabel");

            this.myFetcher.removeMailTag("MyLabel", 1444284171273707784);
            Int32 actualLabelAmountOfMails = this.myFetcher.GetAmountOfMailsFrom("MyLabel");
            Assert.AreEqual(startingMailsAmount - 1, actualLabelAmountOfMails);

            this.myFetcher.addMailTag("[Gmail]/Todos", "MyLabel", 1444284171273707784);
            actualLabelAmountOfMails = this.myFetcher.GetAmountOfMailsFrom("MyLabel");
            Assert.AreEqual(startingMailsAmount, actualLabelAmountOfMails);
        }

        [Test]
        public void Mail_Deleting_Operations_Work_Correctly()
        {
            Int32 startingAllMailsAmount = this.myFetcher.GetAmountOfMailsFrom("[Gmail]/Todos");
            Int32 startingLabelMailsAmount = this.myFetcher.GetAmountOfMailsFrom("MyLabel");
            Int32 startingTrashMailsAmount = this.myFetcher.GetAmountOfMailsFrom("[Gmail]/Papelera");

            this.myFetcher.moveToTrash("MyLabel", 1444287040399823000);
            Int32 actualAllMailsAmount = this.myFetcher.GetAmountOfMailsFrom("[Gmail]/Todos");
            Int32 actualLabelMailAmount = this.myFetcher.GetAmountOfMailsFrom("MyLabel");
            Int32 actualTrashMailAmount = this.myFetcher.GetAmountOfMailsFrom("[Gmail]/Papelera");
            
            Assert.AreEqual(startingTrashMailsAmount + 1, actualTrashMailAmount);
            Assert.AreEqual(startingLabelMailsAmount - 1, actualLabelMailAmount);
            Assert.AreEqual(startingAllMailsAmount - 1, actualAllMailsAmount);

            this.myFetcher.removeFromTrash("MyLabel", 1444287040399823000);
            actualAllMailsAmount = this.myFetcher.GetAmountOfMailsFrom("[Gmail]/Todos");
            actualLabelMailAmount = this.myFetcher.GetAmountOfMailsFrom("MyLabel");
            actualTrashMailAmount = this.myFetcher.GetAmountOfMailsFrom("[Gmail]/Papelera");

            Assert.AreEqual(startingTrashMailsAmount, actualTrashMailAmount);
            Assert.AreEqual(startingLabelMailsAmount, actualLabelMailAmount);
            Assert.AreEqual(startingAllMailsAmount, actualAllMailsAmount);
        }

        [Test]
        public void Mail_Flagging_Operations_Work_Correctly()
        {
            List<Mail> labelMails = this.myFetcher.GetMailsDataFrom("MyLabel2", 1);

            if (labelMails[0].Entity.Subject != "Email para MyLabel, Test de Flags")
                Assert.Fail("El email levantado no es el correcto. El email debe tener subject: Email para MyLabel, Test de Flags.");

            this.myFetcher.setAnsweredFlag("MyLabel2", 1444291302611131331, true);
            this.myFetcher.setSeenFlag("MyLabel2", 1444291302611131331, true);
            this.myFetcher.setFlaggedFlag("MyLabel2", 1444291302611131331, true);

            labelMails = this.myFetcher.GetMailsDataFrom("MyLabel2", 1);

            Assert.True(labelMails[0].Entity.Answered);
            Assert.True(labelMails[0].Entity.Seen);
            Assert.True(labelMails[0].Entity.Flagged);

            this.myFetcher.setAnsweredFlag("MyLabel2", 1444291302611131331, false);
            this.myFetcher.setSeenFlag("MyLabel2", 1444291302611131331, false);
            this.myFetcher.setFlaggedFlag("MyLabel2", 1444291302611131331, false);

            labelMails = this.myFetcher.GetMailsDataFrom("MyLabel2", 1);

            Assert.False(labelMails[0].Entity.Answered);
            Assert.False(labelMails[0].Entity.Seen);
            Assert.False(labelMails[0].Entity.Flagged);
        }
    }
}
