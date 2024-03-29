﻿using ActiveUp.Net.Imap4;
using ActiveUp.Net.Mail;
using Glimpse.DataAccessLayer.Entities;
using Glimpse.Exceptions.MailInterfacesExceptions;
using Glimpse.MailInterfaces;
using Glimpse.Models;
using Glimpse.Tests.global;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;

namespace Glimpse.Tests
{
    [TestFixture]
    public class FetcherTest
    {
        Fetcher MyFetcher;

        [TestFixtureSetUp]
        public void SetUp()
        {
            this.MyFetcher = new Fetcher("imap.sealed@gmail.com", "EAAAAFZ7NXAzAUM70Ltujc3mSPjAmKRppTtLiz8XAN8DoHIu");
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            this.MyFetcher.CloseClient();
        }

        [Test]
        public void Get_Amount_Of_Mails_Returns_Correct_Amount()
        {
            Assert.AreEqual(14, this.MyFetcher.GetAmountOfMailsFrom("INBOX"));
            Assert.AreEqual(11, this.MyFetcher.GetAmountOfMailsFrom("[Gmail]/Importantes"));
        }

        [Test]
        public void Fetcher_Loads_Data_Correctly()
        {
            List<Mail> retrievedMails = new List<Mail>();
            retrievedMails = this.MyFetcher.GetMailsDataFrom("INBOX", 3);

            Assert.AreEqual("Re: Email11 c/ texto formateado", retrievedMails[10].Entity.Subject);
            Assert.AreEqual(DateTime.Parse("Sat, 20 Jul 2013 00:52:44"), retrievedMails[9].Entity.Date);
            Assert.AreEqual("Martin Hoomer", retrievedMails[8].Entity.From.Name);
            Assert.IsTrue(retrievedMails[7].Entity.Body.Contains("</span><span class=\"\">Estrategia de entrenamiento.</span></p>"));
            Assert.IsTrue(retrievedMails[6].Entity.HasExtras);
            Assert.IsTrue(retrievedMails[1].Entity.UidSpam <= 0);
            Assert.IsTrue(retrievedMails[3].Entity.ToAddress.Contains("imap.sealed@gmail.com") && retrievedMails[3].Entity.ToAddress.Contains("test.imap.505@gmail.com"));
            Assert.IsTrue(retrievedMails[10].Entity.Flagged && retrievedMails[7].Entity.Seen && !retrievedMails[10].Entity.Seen);
        }

        [Test]
        public void Mail_Tagging_Operations_Work_Correctly()
        {
            Int32 startingMailsAmount = this.MyFetcher.GetAmountOfMailsFrom("MyLabel");

            this.MyFetcher.RemoveMailTag("MyLabel", 1444284171273707784);
            Int32 actualLabelAmountOfMails = this.MyFetcher.GetAmountOfMailsFrom("MyLabel");
            Assert.AreEqual(startingMailsAmount - 1, actualLabelAmountOfMails);

            this.MyFetcher.AddMailTag("[Gmail]/Todos", "MyLabel", 1444284171273707784);
            actualLabelAmountOfMails = this.MyFetcher.GetAmountOfMailsFrom("MyLabel");
            Assert.AreEqual(startingMailsAmount, actualLabelAmountOfMails);
        }

        [Test]
        public void Mail_Deleting_Operations_Work_Correctly()
        {
            Int32 startingAllMailsAmount = this.MyFetcher.GetAmountOfMailsFrom("[Gmail]/Todos");
            Int32 startingLabelMailsAmount = this.MyFetcher.GetAmountOfMailsFrom("MyLabel");
            Int32 startingTrashMailsAmount = this.MyFetcher.GetAmountOfMailsFrom("[Gmail]/Papelera");

            this.MyFetcher.MoveToTrash("MyLabel", 1444287040399823000);
            Int32 actualAllMailsAmount = this.MyFetcher.GetAmountOfMailsFrom("[Gmail]/Todos");
            Int32 actualLabelMailAmount = this.MyFetcher.GetAmountOfMailsFrom("MyLabel");
            Int32 actualTrashMailAmount = this.MyFetcher.GetAmountOfMailsFrom("[Gmail]/Papelera");
            
            Assert.AreEqual(startingTrashMailsAmount + 1, actualTrashMailAmount);
            Assert.AreEqual(startingLabelMailsAmount - 1, actualLabelMailAmount);
            Assert.AreEqual(startingAllMailsAmount - 1, actualAllMailsAmount);

            this.MyFetcher.RemoveFromTrash("MyLabel", 1444287040399823000);
            actualAllMailsAmount = this.MyFetcher.GetAmountOfMailsFrom("[Gmail]/Todos");
            actualLabelMailAmount = this.MyFetcher.GetAmountOfMailsFrom("MyLabel");
            actualTrashMailAmount = this.MyFetcher.GetAmountOfMailsFrom("[Gmail]/Papelera");

            Assert.AreEqual(startingTrashMailsAmount, actualTrashMailAmount);
            Assert.AreEqual(startingLabelMailsAmount, actualLabelMailAmount);
            Assert.AreEqual(startingAllMailsAmount, actualAllMailsAmount);
        }

        [Test]
        public void Mail_Flagging_Operations_Work_Correctly()
        {
            List<Mail> labelMails = this.MyFetcher.GetMailsDataFrom("MyLabel2", 1);

            if (labelMails[0].Entity.Subject != "Email para MyLabel, Test de Flags")
                Assert.Fail("El email levantado no es el correcto. El email debe tener subject: Email para MyLabel, Test de Flags.");

            this.MyFetcher.SetAnsweredFlag("MyLabel2", 1444291302611131331, true);
            this.MyFetcher.SetSeenFlag("MyLabel2", 1444291302611131331, true);
            this.MyFetcher.SetFlaggedFlag("MyLabel2", 1444291302611131331, true);

            labelMails = this.MyFetcher.GetMailsDataFrom("MyLabel2", 1);

            Assert.True(labelMails[0].Entity.Answered);
            Assert.True(labelMails[0].Entity.Seen);
            Assert.True(labelMails[0].Entity.Flagged);

            this.MyFetcher.SetAnsweredFlag("MyLabel2", 1444291302611131331, false);
            this.MyFetcher.SetSeenFlag("MyLabel2", 1444291302611131331, false);
            this.MyFetcher.SetFlaggedFlag("MyLabel2", 1444291302611131331, false);

            labelMails = this.MyFetcher.GetMailsDataFrom("MyLabel2", 1);

            Assert.False(labelMails[0].Entity.Answered);
            Assert.False(labelMails[0].Entity.Seen);
            Assert.False(labelMails[0].Entity.Flagged);
        }

        [Test]
        public void Get_Mails_Between_UID_Works_Correctly()
        {
            List<Mail> retrievedMails = this.MyFetcher.GetMailsBetweenUID("[Gmail]/Todos", 5, 20);

            Assert.AreEqual(5, retrievedMails.Count);
            Assert.AreEqual("Empieza a utilizar Google+", retrievedMails[0].Entity.Subject);
            Assert.True(retrievedMails[4].Entity.Body.Contains("class=\"\">En este apartado"));
        }
    }
}
