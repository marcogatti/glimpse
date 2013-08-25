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
        public void PersistAMail()
        {
            ITransaction tran = session.BeginTransaction();

            anAccount = new MailAccount("test.imap.505@gmail.com", "ytrewq123");
            MailAccount existingAccount = MailAccount.FindByAddress("test.imap.505@gmail.com");
            if (existingAccount == null)
                session.SaveOrUpdate(anAccount.Entity);

            anAccount.Entity = existingAccount.Entity;

            aMail = new Mail(new MailEntity());
            aMail.Entity.Subject = "Mail de prueba";
            aMail.Entity.MailAccount = anAccount.Entity;
            session.SaveOrUpdate(aMail.Entity);

            tran.Commit();
        }

        [Test]
        public void GetOneMailFromInboxAndDataIsOK()
        {
            Mail theMail = new Mail(Mail.FindByMailAccount(anAccount, 1).First<MailEntity>());

            Assert.AreEqual(aMail.Entity.Subject, theMail.Entity.Subject);
        }

        [TestFixtureTearDown]
        public void cleanPersistedMail()
        {
            ITransaction tran = session.BeginTransaction();

            session.Delete(aMail.Entity);
            session.Delete(anAccount.Entity);

            tran.Commit();
            session.Close();
        }
    }
}