using System;
using NUnit.Framework;
using ActiveUp.Net.Imap4;
using Glimpse.MailInterfaces;
using Glimpse.Exceptions.MailInterfacesExceptions;


namespace Glimpse.Tests.MailInterfaces
{
    [TestFixture]
    public class ConnectorTest
    {
        private Connector myConnector;

        [SetUp]
        public void Set_Up()
        {
            myConnector = new Connector();
        }

        [Test]
        public void Login_Returns_A_Connected_Imap_Client()
        {
            Assert.AreEqual(true, myConnector.ImapLogin("test.imap.506@gmail.com", "EAAAAJEOi8buiitbWq6IHRQ/+WwSmQhmymzpaiMrjI/s6k9/").IsConnected);
        }

        [Test]
        [ExpectedException(typeof(InvalidAuthenticationException))]
        public void Login_With_Wrong_Password_Throws_Authentication_Exception()
        {
            myConnector.ImapLogin("test.imap.506@gmail.com", "EAAAAFZ7NXAzAUM70Ltujc3mSPjAmKRppTtLiz8XAN8DoHIu");            
        }

        [Test]
        [ExpectedException(typeof(InvalidAuthenticationException))]
        public void Login_With_Wrong_Username_Throws_Authentication_Exception()
        {
            myConnector.ImapLogin("wrongLogin", "EAAAAJEOi8buiitbWq6IHRQ/+WwSmQhmymzpaiMrjI/s6k9/");
        }
    }
}
