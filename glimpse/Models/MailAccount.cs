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
using System.Collections.Specialized;

namespace Glimpse.Models
{
    public class MailAccount
    {
        private Fetcher myFetcher { get; set; }
        private Sender mySender { get; set; }

        public MailAccountEntity Entity { get; set; }

        public MailAccount(MailAccountEntity accountEntity)
        {
            this.Entity = accountEntity;
            this.LoginExternal();
            this.mySender = new Sender(this.Entity.Address, this.Entity.Password);
        }
        public MailAccount(String address, String password)
        {
            this.Entity = new MailAccountEntity(address, password);
            this.LoginExternal();
            this.mySender = new Sender(address, password);
        }

        public static MailAccount FindByAddress(String emailAddress, ISession session)
        {
            MailAccountEntity account = session.CreateCriteria<MailAccountEntity>()
                                          .Add(Restrictions.Eq("Address", emailAddress))
                                          .UniqueResult<MailAccountEntity>();

            if (account == null)
                return null;
            else
                return new MailAccount(account);
        }

        public Int32 getLastUIDFrom(String mailbox)
        {
            return this.myFetcher.GetLastUIDFrom(mailbox);
        }

        public List<Mail> getMailsFromHigherThan(string mailbox, Int64 lastUID)
        {
            return this.myFetcher.GetMailDataFromHigherThan(mailbox, lastUID);
        }

        public virtual void SaveOrUpdate()
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();

            MailAccount oldAccount = FindByAddress(this.Entity.Address, session);
            if (oldAccount != null)
            {
                oldAccount.Clone(this);
                this.Entity = oldAccount.Entity;
            }

            session.SaveOrUpdate(this.Entity);

            tran.Commit();
            session.Close();
        }

        public MailAccount LoginExternal()
        {
            this.myFetcher = new Fetcher(this.Entity.Address, this.Entity.Password);
            return this;
        }

        public void UpdateLabels()
        {
            String tagsNames;
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            NameValueCollection labelsByProperty = this.myFetcher.getLabels();

            IList<LabelEntity> databaseLabels = session.CreateCriteria<LabelEntity>()
                                               .Add(Restrictions.Eq("MailAccountEntity", this.Entity))
                                               .List<LabelEntity>();

            this.RegisterLabel(labelsByProperty["Inbox"], session, databaseLabels, "Inbox");
            this.RegisterLabel(labelsByProperty["All"], session, databaseLabels, "All");
            this.RegisterLabel(labelsByProperty["Deleted"], session, databaseLabels, "Deleted");
            this.RegisterLabel(labelsByProperty["Spam"], session, databaseLabels, "Spam");
            this.RegisterLabel(labelsByProperty["Important"], session, databaseLabels, "Important");
            this.RegisterLabel(labelsByProperty["Sent"], session, databaseLabels, "Sent");
            this.RegisterLabel(labelsByProperty["Starred"], session, databaseLabels, "Starred");
            this.RegisterLabel(labelsByProperty["Drafts"], session, databaseLabels, "Drafts");

            tagsNames = labelsByProperty["Tags"];
            if (tagsNames != null)
            {
                String[] labelsName = tagsNames.Split(new String[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                foreach (String label in labelsName)
                {
                    this.RegisterLabel(label, session, databaseLabels);
                }
            }

            tran.Commit();
            session.Flush();
            session.Close();
        }

        private void RegisterLabel(String labelName, ISession session, IList<LabelEntity> databaseLabels, String systemName = null)
        {
            if (labelName == null)
                return;
            LabelEntity labelEntity = new LabelEntity();

            foreach (LabelEntity databaseLabel in databaseLabels)
            {
                if (databaseLabel.Name == labelName)
                {
                    return;
                }
            }

            labelEntity.Name = labelName;
            labelEntity.MailAccountEntity = this.Entity;
            labelEntity.SystemName = systemName;
            Label label = new Label(labelEntity);
            label.SaveOrUpdate(session);

        }
        private void Clone(MailAccount fromAccount)
        {
            this.Entity.Address = fromAccount.Entity.Address;
            this.Entity.Password = fromAccount.Entity.Password;
        }
    }
}