using System;
using NUnit.Framework;
using ActiveUp.Net.Imap4;
using glimpse.MailInterfaces;
using glimpse.Exceptions.MailInterfacesExceptions;


namespace glimpse.Tests.MailInterfaces
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
            
            Assert.AreEqual(true, myConnector.Login("test.imap.506@gmail.com", "ytrewq123").IsConnected);
        }

        [Test]
        [ExpectedException(typeof(InvalidAuthenticationException))]
        public void Login_With_Wrong_Password_Throws_Authentication_Exception()
        {
            myConnector.Login("test.imap.506@gmail.com", "wrongPassword");            
        }

        [Test]
        [ExpectedException(typeof(InvalidAuthenticationException))]
        public void Login_With_Wrong_Username_Throws_Authentication_Exception()
        {
            myConnector.Login("wrongLogin", "ytrewq123");
        }
    }
}
