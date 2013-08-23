using Glimpse.DataAccessLayer;
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
            this.myFetcher = new Fetcher(address, password);
            this.mySender = new Sender();
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

            MailAccount oldAccount = FindByAddress(this.Entity.Address, currentSession);
            if (oldAccount.Entity == null)
            {
                persistAccount = this;
            }
            else
            {
                persistAccount = oldAccount;
                persistAccount.CopyEntityDataFrom(this);
            }

            currentSession.SaveOrUpdate(persistAccount.Entity);
   
            tran.Commit();
        }

        private void CopyEntityDataFrom(MailAccount fromAccount)
        {
            this.Entity.Address = fromAccount.Entity.Address;
            this.Entity.Password = fromAccount.Entity.Password;
        }

        public static MailAccount FindByAddress(String emailAddress, ISession session)
        {
            MailAccountEntity mae = session.CreateCriteria<MailAccountEntity>()
                                          .Add(Restrictions.Eq("Address", emailAddress))
                                          .UniqueResult<MailAccountEntity>();

            return new MailAccount(mae.Address, mae.Password);
        }

        public static MailAccount FindByAddress(String emailAddress)
        {
            ISession session = NHibernateManager.OpenSession();
            return FindByAddress(emailAddress, session);
        }

        public virtual AccountInterface LoginExternal()
        {
            return new AccountInterface(this.Entity.Address, this.Entity.Password);
        }

    }
}