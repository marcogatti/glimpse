using Glimpse.DataAccessLayer.Entities;
using Glimpse.Exceptions;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Glimpse.Models
{
    public class Mail
    {
        public MailEntity Entity { get; private set; }

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

        public bool HasLabel(Label aLabel)
        {
            LabelEntity entity = (from label
                                  in this.Entity.Labels
                                  where label.MailAccountEntity.Id == aLabel.Entity.MailAccountEntity.Id &&
                                        label.Name == aLabel.Entity.Name
                                  select label).SingleOrDefault<LabelEntity>();

            return entity != null;
        }
        public void SetFrom(AddressEntity from)
        {
            this.Entity.From = from;
        }
        public void AddLabel(Label theLabel, ISession session)
        {
            if (!this.HasLabel(theLabel) && !this.Entity.Labels.Any(x => x.SystemName == "Trash"))
            {
                this.Entity.Labels.Add(theLabel.Entity);
                this.Save(session);
            }
        }
        public void RemoveLabel(String label, Boolean isSystemLabel, ISession session)
        {
            LabelEntity labelToRemove;

            if (isSystemLabel)
                labelToRemove = this.Entity.Labels.First<LabelEntity>(x => x.SystemName == label);
            else
                labelToRemove = this.Entity.Labels.First<LabelEntity>(x => x.Name == label);

            this.Entity.Labels.Remove(labelToRemove);
            this.Save(session);
        }
        public void SetImportance(UInt16 newImportance, ISession session)
        {
            if ((newImportance >= 5 && this.Entity.Importance == 5) ||
                (newImportance <= 1 && this.Entity.Importance == 1) ||
                (newImportance == this.Entity.Importance))
                return;
            else if (newImportance == this.Entity.Importance + 1 || newImportance == this.Entity.Importance - 1)
            {
                this.Entity.Importance = newImportance;
                this.Save(session);
            }
        }
        public void Archive(ISession session)
        {
            if (this.Entity.Labels.Any(x => x.SystemName == "INBOX"))
            {
                this.Entity.Labels.Remove(this.Entity.Labels.Single(x => x.SystemName == "INBOX"));
                this.Save(session);
            }
        }
        public void Unarchive(Label inboxLabel, ISession session)
        {
            if (!this.Entity.Labels.Any(x => x.SystemName == "INBOX"))
            {
                this.Entity.Labels.Add(inboxLabel.Entity);
                this.Save(session);
            }
        }
        public void Delete(ISession session)
        {
            session.Delete(this.Entity);
        }
        public void Save(ISession session)
        {
            session.SaveOrUpdate(this.Entity);
        }
        public String GetImapFolderName()
        {
            String systemName = this.GetSystemFolderProperty();
            return this.Entity.Labels
                       .Where(x => x.SystemName == systemName)
                       .Select(x => x.Name)
                       .Single();
        }
        public String GetSystemFolderProperty()
        {
            if (this.Entity.UidTrash > 0) return "Trash";
            if (this.Entity.UidSpam > 0) return "Junk";
            return "All";
        }

        public static List<MailEntity> FindByMailAccount(MailAccount mailAccount, ISession session)
        {

            List<MailEntity> foundMails = (List<MailEntity>)session.CreateCriteria<MailEntity>()
                                                        .Add(Restrictions.Eq("MailAccountEntity", mailAccount.Entity))
                                                        .List<MailEntity>();
            return foundMails;
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
        private void MoveToTrash(ISession session)
        {
            Label trashLabel = Label.FindBySystemName(new MailAccount(this.Entity.MailAccountEntity), "Trash", session);
            this.AddLabel(trashLabel, session);
        }
    }
}