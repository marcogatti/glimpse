﻿using Glimpse.DataAccessLayer;
using Glimpse.DataAccessLayer.Entities;
using Glimpse.Models;
using NHibernate;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glimpse.Tests.DAL
{
    [TestFixture]
    public class MailTest
    {
        private MailAccount anAccount;
        private Mail aMail;
        private ISession session = NHibernateManager.OpenSession();


        [TestFixtureSetUp]
        public void TextFixtureSetUp()
        {
            ITransaction tran = session.BeginTransaction();

            anAccount = new MailAccount("test.imap.505@gmail.com", "ytrewq123");
            MailAccount existingAccount = MailAccount.FindByAddress("test.imap.505@gmail.com", this.session);
            if (existingAccount == null)
                this.session.SaveOrUpdate(anAccount.Entity);
            else
                this.anAccount = existingAccount;

            this.aMail = new Mail(new MailEntity());
            this.aMail.Entity.Subject = "Mail de prueba";
            this.aMail.Entity.MailAccountEntity = anAccount.Entity;
            this.session.SaveOrUpdate(aMail.Entity);

            tran.Commit();
        }

        [Test]
        public void GetOneMailFromInbox()
        {
            Mail theMail = new Mail(Mail.FindByMailAccount(anAccount, session).First<MailEntity>());

            Assert.NotNull(theMail.Entity);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            ITransaction tran = session.BeginTransaction();

            session.Delete(aMail.Entity);
            session.Delete(anAccount.Entity);

            tran.Commit();
            session.Close();
        }
    }
}
