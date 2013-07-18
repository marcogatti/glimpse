using System;
using NUnit.Framework;
using ActiveUp.Net.Imap4;
using glimpse.MailInterfaces;

namespace glimpse.Tests
{
    [TestFixture]
    public class FetcherTest
    {
        Fetcher myFetcher;

        [SetUp]
        public void SetUp()
        {
            myFetcher = new Fetcher("test.imap.506@gmail.com", "ytrewq123");
        }

        [Test]
        public void Get_Inbox_Retrieves_Mails_From_Account()
        {
            Assert.IsNotEmpty(myFetcher.GetInboxMails());
        }
    }
}
