using Glimpse.DataAccessLayer;
using Glimpse.DataAccessLayer.Entities;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.Models
{
    public class Mail
    {
        public MailEntity Entity { get; private set; }

        public void setFrom(AddressEntity from)
        {
            this.Entity.From = from;
        }

        public Mail(MailEntity entity)
        {
            this.Entity = entity;
        }
        public Mail(Int64 mailId, ISession session)
        {
            MailEntity entity = session.CreateCriteria<MailEntity>()
                                    .Add(Restrictions.Eq("Id", mailId))
                                    .SetMaxResults(1)
                                    .UniqueResult<MailEntity>();
            this.Entity = entity;
        }

        public void RemoveLabel(String label, ISession session)
        {
            LabelEntity labelToRemove = this.Entity.Labels.First<LabelEntity>(x => x.Name == label);
            this.Entity.Labels.Remove(labelToRemove);
        }

        public String GetImapFolderName()
        {
            String systemName;
            if (this.Entity.UidTrash > 0)
                systemName = "Trash";
            else if (this.Entity.UidSpam > 0)
                systemName = "Junk";
            else
                systemName = "All";
            return this.Entity.Labels
                       .Where(x => x.SystemName == systemName)
                       .Select(x => x.Name)
                       .Single();
        }

        public static List<MailEntity> FindByMailAccount(MailAccount mailAccount, ISession session)
        {

            List<MailEntity> foundMails = (List<MailEntity>)session.CreateCriteria<MailEntity>()
                                                        .Add(Restrictions.Eq("MailAccountEntity", mailAccount.Entity))
                                                        .List<MailEntity>();
            return foundMails;
        }

        public void Save(ISession session)
        {
            session.SaveOrUpdate(this.Entity);
        }

        private void Clone(MailEntity from)
        {
            this.Entity.MailAccountEntity = from.MailAccountEntity;
            this.Entity.From = from.From;
            this.Entity.Labels = from.Labels;
            this.Entity.Extras = from.Extras;
            this.Entity.Gm_tid = from.Gm_tid;
            this.Entity.Gm_mid = from.Gm_mid;
            this.Entity.Date = from.Date;
            this.Entity.Subject = from.Subject;
            this.Entity.UidTrash = from.UidTrash;
            this.Entity.UidSpam = from.UidSpam;
            this.Entity.UidAll = from.UidAll;
            this.Entity.Answered = from.Answered;
            this.Entity.Flagged = from.Flagged;
            this.Entity.Seen = from.Seen;
            this.Entity.Draft = from.Draft;
            this.Entity.HasExtras = from.HasExtras;
            this.Entity.ToAddress = from.ToAddress;
            this.Entity.CC = from.CC;
            this.Entity.BCC = from.BCC;
            this.Entity.Body = from.Body;
        }

        public void AddLabel(Label theLabel, ISession session)
        {
            if (!this.hasLabel(theLabel))
            {
                this.Entity.Labels.Add(theLabel.Entity);
                this.Save(session);
            }
        }

        public bool hasLabel(Label aLabel)
        {
            LabelEntity entity = (from label 
                                  in this.Entity.Labels 
                                  where label.MailAccountEntity.Id == aLabel.Entity.MailAccountEntity.Id && 
                                        label.Name == aLabel.Entity.Name
                                  select label).SingleOrDefault<LabelEntity>();

            return entity != null;
        }
    }
}