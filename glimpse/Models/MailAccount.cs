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

        public Int32 getLastUIDExternalFrom(String mailbox)
        {
            return this.myFetcher.GetLastUIDFrom(mailbox);
        }

        public List<Mail> getMailsFromHigherThan(string mailbox, Int64 lastUID)
        {
            ISession session = NHibernateManager.OpenSession();
            IList<LabelEntity> labels = Label.FindByAccount(this.Entity, session);
            this.myFetcher.setLabels(labels);
            List<Mail> mails = this.myFetcher.GetMailDataFromHigherThan(mailbox, lastUID);
            session.Close();
            return mails;
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

            this.RegisterLabel(labelsByProperty["INBOX"], session, databaseLabels, "INBOX");
            this.RegisterLabel(labelsByProperty["All"], session, databaseLabels, "All");
            this.RegisterLabel(labelsByProperty["Trash"], session, databaseLabels, "Trash");
            this.RegisterLabel(labelsByProperty["Junk"], session, databaseLabels, "Junk");
            this.RegisterLabel(labelsByProperty["Important"], session, databaseLabels, "Important");
            this.RegisterLabel(labelsByProperty["Sent"], session, databaseLabels, "Sent");
            this.RegisterLabel(labelsByProperty["Flagged"], session, databaseLabels, "Flagged");
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

        public void sendMail(String toAddress, String body, String subject)
        {
            AddressCollection recipients = new AddressCollection();
            ActiveUp.Net.Mail.Address address = new ActiveUp.Net.Mail.Address(toAddress);
            recipients.Add(address);

            this.mySender.sendMail(recipients, body, subject);
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

        public Mail ReadMail(Int64 id, ISession session)
        {
            ITransaction tran = session.BeginTransaction();

            MailEntity mailEntity = session.CreateCriteria<MailEntity>()
                                 .Add(Restrictions.Eq("MailAccountEntity", this.Entity))
                                 .Add(Restrictions.Eq("Id", id))
                                 .UniqueResult<MailEntity>();
            mailEntity.Seen = true;

            Mail mail = new Mail(mailEntity);
            mail.Save(session);

            this.myFetcher.setSeenFlag("[Gmail]/Todos",mail.Entity.Gm_mid, true);

            tran.Commit();

            return mail;
        }

        public Int64 GetLastUIDLocalFromALL(ISession session)
        {
            Int64 lastDatabaseUID = session.CreateCriteria<MailEntity>()
                                      .Add(Restrictions.Eq("MailAccountEntity", this.Entity))
                                      .SetProjection(Projections.Max("UidInbox"))
                                      .UniqueResult<Int64>();

            return lastDatabaseUID;
        }

        public void FetchAndSaveMails(Label label, Int64 fromUid, Int64 toUid)
        {
            List<Mail> mails = this.myFetcher.GetMailsBetweenUID(label.Entity.Name, (int)fromUid, (int)toUid);

            foreach (Mail mail in mails)
            {
                mail.Entity.MailAccountEntity = this.Entity;
            }

            MailAccount.Save(mails);
        }

        private static void Save(List<Mail> mails)
        {
            ISession session = NHibernateManager.OpenSession();

            ITransaction tran = session.BeginTransaction();

            foreach (Mail mailToSave in mails)
            {
                Address foundAddress = Address.FindByAddress(mailToSave.Entity.From.MailAddress, session);

                if (foundAddress.Entity == null)
                {
                    session.SaveOrUpdate(mailToSave.Entity.From);
                }
                else
                {
                    mailToSave.setFrom(foundAddress.Entity);
                }

                session.SaveOrUpdate(mailToSave.Entity);
            }

            tran.Commit();

            session.Flush();
            session.Close();
        }
    }
}