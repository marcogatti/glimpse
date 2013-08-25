﻿using Glimpse.DataAccessLayer;
using Glimpse.DataAccessLayer.Entities;
using Glimpse.MailInterfaces;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ActiveUp.Net.Mail;

namespace Glimpse.Models
{
    public class MailAccount
    {
        private Fetcher myFetcher { get; set; }
        private Sender mySender { get; set; }

        public MailAccountEntity Entity { get; set; }        

        public MailAccount(String address, String password)
        {
            this.Entity = new MailAccountEntity(address, password);
            this.LoginExternal();
            this.mySender = new Sender(address, password);
        }

        public MessageCollection GetInboxMessages()
        {
            return this.myFetcher.GetInboxMails();
        }

        public Int32 getLastUIDFrom(String mailbox)
        {
            return this.myFetcher.GetLastUIDFrom(mailbox);
        }

        public MailCollection getMailsFromHigherThan(string mailbox, Int64 lastUID)
        {
            return this.myFetcher.GetMailDataFromHigherThan(mailbox, lastUID);
        }

        public virtual void SaveOrUpdate()
        {
            ISession currentSession = NHibernateManager.OpenSession();
            ITransaction tran = currentSession.BeginTransaction();

            MailAccount persistAccount;

            MailAccount oldAccount = FindByAddress(this.Entity.Address);
            if (oldAccount.Entity == null)
            {
                persistAccount = this;
            }
            else
            {
                persistAccount = oldAccount;
                persistAccount.Clone(this);
            }

            currentSession.SaveOrUpdate(persistAccount.Entity);
   
            tran.Commit();
        }

        public static MailAccount FindByAddress(String emailAddress)
        {
            ISession session = NHibernateManager.OpenSession();
            
            MailAccountEntity account = session.CreateCriteria<MailAccountEntity>()
                                          .Add(Restrictions.Eq("Address", emailAddress))
                                          .UniqueResult<MailAccountEntity>();

            return new MailAccount(account.Address, account.Password);
        }
                
        public MailAccount LoginExternal()
        {
            this.myFetcher = new Fetcher(this.Entity.Address, this.Entity.Password);
            return this;
        }

        private void Clone(MailAccount fromAccount)
        {
            this.Entity.Address = fromAccount.Entity.Address;
            this.Entity.Password = fromAccount.Entity.Password;
        }
    }
}