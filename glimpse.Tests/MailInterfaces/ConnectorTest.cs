using System;
using ActiveUp.Net.Imap4;
using glimpse.MailInterfaces;
using glimpse.Exceptions.MailInterfacesExceptions;
using NUnit.Framework;

namespace glimpse.Tests.MailInterfaces
{
    [TestFixture]
    public class ConnectorTest
    {
        private Connector myConnector;

        [SetUp]
        public void SetUp()
        {
            myConnector = new Connector();
        }

        [Test]
        public void LoginReturnsAConnectedImapClient()
        {
            
            Assert.AreEqual(true, myConnector.Login("test.imap.506@gmail.com", "ytrewq123").IsConnected);
        }

        [Test]
        [ExpectedException(typeof(InvalidAuthenticationException))]
        public void LoginWithWrongPasswordThrowsAuthenticationException()
        {
            myConnector.Login("test.imap.506@gmail.com", "wrongPassword");            
        }

        [Test]
        [ExpectedException(typeof(InvalidAuthenticationException))]
        public void LoginWithWrongUsernameThrowsAuthenticationException()
        {
            myConnector.Login("test.imap.506@gmailcom", "ytrewq123");
        }
    }
}
