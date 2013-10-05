﻿using ActiveUp.Net.Mail;
using Glimpse.DataAccessLayer;
using Glimpse.DataAccessLayer.Entities;
using Glimpse.MailInterfaces;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Glimpse.Models
{
    public class MailAccount : IDisposable
    {
        private Fetcher MyFetcher { get; set; }
        private Sender MySender { get; set; }
        public MailAccountEntity Entity { get; set; }

        #region Public Methods
        public MailAccount(String address, String password) : this(new MailAccountEntity(address, password)) { }
        public MailAccount(MailAccountEntity accountEntity)
        {
            this.Entity = accountEntity;
            this.Entity.Active = true;
            this.MySender = new Sender(this.Entity.Address, this.Entity.Password);
        }
        public MailAccount Clone()
        {
            MailAccount mailAccountClone;
            MailAccountEntity entity;
            using (ISession session = NHibernateManager.OpenSession())
            {
                entity = MailAccount.FindByAddress(this.Entity.Address, session).Entity;
                mailAccountClone = new MailAccount(entity);

                if (this.IsConnected())
                    mailAccountClone.ConnectFull();
                session.Close();
            }
            return mailAccountClone;
        }

        public void SetAsMainAccount(ISession session)
        {
            IList<MailAccountEntity> notMainMailAccounts = session.CreateCriteria<MailAccountEntity>()
                                                                .Add(Restrictions.Eq("User", this.Entity.User))
                                                                .Add(Restrictions.Not(Restrictions.Eq("Id", this.Entity.Id)))
                                                                .List<MailAccountEntity>();
            foreach(MailAccountEntity notMainAccount in notMainMailAccounts)
            {
                notMainAccount.Active = false;
                new MailAccount(notMainAccount).SaveOrUpdate(session);
            }
            this.Entity.IsMainAccount = true;
        }
        public void SetUser(User user)
        {
            this.Entity.User = user.Entity;
        }
        public void Dispose()
        {
            this.MyFetcher.Dispose();
        }
        public void UpdateLabels(ISession session)
        {
            String tagsNames;
            NameValueCollection labelsByProperty = this.MyFetcher.getLabels();
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
            this.SetFetcherLabels(session);
        }
        public bool IsConnected()
        {
            if (this.MyFetcher == null)
                return false;
            return this.MyFetcher.IsConnected();
        }
        public bool IsFullyConnected()
        {
            if (this.MyFetcher == null)
                return false;
            return this.MyFetcher.IsFullyConnected();
        }

        public Int64 GetUIDLocal(ISession session, String labelSystemName, Boolean max)
        {
            String imapDatabaseField;
            Int64 lastDatabaseUID;
            switch (labelSystemName)
            {
                case "Trash":
                    imapDatabaseField = "UidTrash";
                    break;
                case "Junk":
                    imapDatabaseField = "UidSpam";
                    break;
                default:
                    imapDatabaseField = "UidAll";
                    break;
            }
            if (max) //maximo
            {
                lastDatabaseUID = session.CreateCriteria<MailEntity>()
                                      .Add(Restrictions.Eq("MailAccountEntity", this.Entity))
                                      .SetProjection(Projections.Max(imapDatabaseField))
                                      .UniqueResult<Int64>();
            }
            else //minimo
            {
                lastDatabaseUID = session.CreateCriteria<MailEntity>()
                                      .Add(Restrictions.Eq("MailAccountEntity", this.Entity))
                                      .SetProjection(Projections.Min(imapDatabaseField))
                                      .UniqueResult<Int64>();
            }
            return lastDatabaseUID; //devuelve 0 si no existen mails
        }
        public MailCollection GetMailsByDate(DateTime initialDate, DateTime finalDate, ISession session)
        {
            //TODO: Ver el tema de las fechas para diferenciar mails de las 3 carpetas
            IList<MailEntity> databaseMails = session.CreateCriteria<MailEntity>()
                                                  .Add(Restrictions.Eq("MailAccountEntity", this.Entity))
                                                  .Add(Restrictions.Between("Date", initialDate, finalDate))
                                                  .List<MailEntity>();
            return new MailCollection(databaseMails);
        }
        public MailCollection GetMailsByAmount(Int32 amountOfMails, ISession session)
        {
            //TODO: Ver el tema de las fechas para diferenciar mails de las 3 carpetas
            IList<MailEntity> databaseMails = session.CreateCriteria<MailEntity>()
                                                  .Add(Restrictions.Eq("MailAccountEntity", this.Entity))
                                                  .AddOrder(Order.Desc("Date"))
                                                  .SetMaxResults(amountOfMails)
                                                  .List<MailEntity>();
            return new MailCollection(databaseMails);
        }
        public String ReadMail(Int64 id, ISession session)
        {
            MailEntity mailEntity = session.CreateCriteria<MailEntity>()
                                 .Add(Restrictions.Eq("MailAccountEntity", this.Entity))
                                 .Add(Restrictions.Eq("Id", id))
                                 .UniqueResult<MailEntity>();
            Mail mail = new Mail(mailEntity);
            if (mailEntity.Seen == false)
            {
                ITransaction tran = session.BeginTransaction();
                mail.Entity.Seen = true;
                String imapFolderName = mail.GetImapFolderName();
                mail.Save(session); //DB
                this.MyFetcher.SetSeenFlag(imapFolderName, mail.Entity.Gm_mid, true); //IMAP
                tran.Commit();
            }
            return mail.Entity.Body;
        }

        #region External Email Interface Methods
        public void ConnectLight()
        {
            if (this.MyFetcher == null || !this.MyFetcher.IsConnected())
                this.MyFetcher = new Fetcher(this.Entity.Address, this.Entity.Password);
        }
        public void ConnectFull()
        {
            this.ConnectLight();
            using (ISession session = NHibernateManager.OpenSession())
            {
                this.SetFetcherLabels(session);
                session.Close();
            }
        }
        public void Disconnect()
        {
            if(this.MyFetcher != null)
                this.MyFetcher.CloseClient();
        }
        public void FetchAndSaveMails(Label label, Int64 fromUid, Int64 toUid, ref Int32 amountOfMails)
        {
            List<Mail> mails = this.MyFetcher.GetMailsBetweenUID(label.Entity.Name, (int)fromUid, (int)toUid);
            foreach (Mail mail in mails)
            {
                mail.Entity.MailAccountEntity = this.Entity;
            }
            MailAccount.Save(mails);
            amountOfMails = mails.Count;
        }
        public void SendMail(String toAddresses, String body, String subject)
        {
            AddressCollection recipients = Glimpse.Models.Address.ParseAddresses(toAddresses);
            this.MySender.sendMail(recipients, body, subject);
        }
        public void AddLabelIMAP(Mail theMail, Label theLabel)
        {
            this.MyFetcher.AddMailTag(theMail.GetImapFolderName(), theLabel.Entity.Name, theMail.Entity.Gm_mid);
        }
        public void RemoveMailLabel(String label, UInt64 gmID)
        {
            this.MyFetcher.RemoveMailTag(label, gmID);
        }
        public void TrashMail(Mail mail, String systemFolderName)
        {
            if (systemFolderName == "Trash")
                this.MyFetcher.DeleteFromTrash(mail.Entity.Gm_mid);
            else
                this.MyFetcher.MoveToTrash(mail.GetImapFolderName(), mail.Entity.Gm_mid);
        }
        public Int32 GetUIDExternalFrom(String mailbox, Boolean max)
        {
            return this.MyFetcher.GetLimitUIDFrom(mailbox, max);
        }
        public DateTime GetLowestMailDate()
        {
            return this.MyFetcher.GetLowestMailDate();
        }
        #endregion

        public virtual void SaveOrUpdate(ISession session)
        {
            MailAccount oldAccount = FindByAddress(this.Entity.Address, session);
            if (oldAccount != null)
            {
                oldAccount.Clone(this);
                this.Entity = oldAccount.Entity;
            }
            session.SaveOrUpdate(this.Entity);
        }
        public static void SendResetPasswordMail(User user, String newPassword, ISession session)
        {
            MailAccount mailAccount = MailAccount.FindMainMailAccount(user.Entity.Username, session);
            if (mailAccount == null)
                throw new Exception("Usuario: " + user.Entity.Username + " no posee mailAccount primario.");
            Sender.SendResetPasswordMail(user.Entity.Username, mailAccount.Entity.Address, newPassword);
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
        public static MailAccount FindMainMailAccount(String username, ISession session)
        {
            MailAccountEntity entity = session.CreateCriteria<MailAccountEntity>()
                                            .Add(Restrictions.Eq("User.Username", username))
                                            .Add(Restrictions.Eq("IsMainAccount", true))
                                            .UniqueResult<MailAccountEntity>();
            return new MailAccount(entity);
        }
        #endregion
        #region Private Methods
        private void SetFetcherLabels(ISession session)
        {
            IList<LabelEntity> labels = Label.FindByAccount(this.Entity, session);
            this.MyFetcher.SetLabels(labels);
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
                    mailToSave.SetFrom(foundAddress.Entity);
                }

                session.SaveOrUpdate(mailToSave.Entity);
            }
            tran.Commit();
            session.Flush();
            session.Close();
        }
        #endregion
    }
}