using System;
using NUnit.Framework;
using ActiveUp.Net.Imap4;
using Glimpse.MailInterfaces;
using Glimpse.Exceptions.MailInterfacesExceptions;
using ActiveUp.Net.Mail;
using System.IO;


namespace Glimpse.Tests.MailInterfaces
{
    [TestFixture]
    public class SenderTest
    {
        private Sender mySender;

        [TestFixtureSetUp]
        public void SetUp()
        {
            this.mySender = new Sender("test.imap.performance@gmail.com", "EAAAANNt9jTRTqnFOymwLQ6Yy6PsecEBR2l10A677SIjAShr", "Simpson Homer");
        }

        [Test]
        [Explicit]
        public void EXP_Send_Mail_Loads_Data_And_Sends_Mail_Correctly()
        {
            AddressCollection recipients = new AddressCollection();
            AddressCollection BCC = new AddressCollection();
            AddressCollection CC = new AddressCollection();

            Address addressOne = new Address("test.imap.performance@gmail.com", "Simpson Marge");
            Address addressSecond = new Address("ezequiel.lopez.2009@hotmail.com", "Simpson Bart");
            Address addressThird = new Address("ezequiel.lopez.2010@hotmail.com", "Simpson Maggie");

            recipients.Add(addressOne);
            recipients.Add(addressSecond);
            BCC.Add(addressOne);
            BCC.Add(addressThird);
            CC.Add(addressSecond);
            CC.Add(addressThird);

            String body = "<b>Body text here is bold.</b><div><br></div><div style=\"text-align: center;\">Body text here is centered.</div><div><br></div><div><font color=\"#ac193d\">Body text here is red.</font></div>";
            String subject = "Glimpse Sender Test";

            this.mySender.sendMail(recipients, body, subject, CC, BCC);
            Assert.Pass();
        }

        [Test]
        [Explicit]
        public void EXP_Send_Mail_With_Attachments_Sends_Correctly()
        {
            SmtpMessage newMail = new SmtpMessage();
            String attachmentPath = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)
                                             .Parent.Parent.FullName + "\\MailInterfaces\\ReadSign.jpg";

            newMail.To.Add(new Address("test.imap.performance@gmail.com"));
            newMail.BodyText.Text = "Mail with attachment!";
            newMail.Attachments.Add(attachmentPath, false);

            this.mySender.sendMail(newMail);
            Assert.Pass();
        }

        [Test]
        [Explicit]
        public void EXP_Send_Reset_Mail_Sends_Correctly()
        {
            Sender.SendResetPasswordMail("myGlimpseUsername", "test.imap.506@gmail.com", "myNewPassword");
            Assert.Pass();
        }
    }
}
