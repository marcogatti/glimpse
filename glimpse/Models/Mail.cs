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
 
        public static List<MailEntity> FindByMailAccount(MailAccount mailAccount, ISession session){

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
            this.Entity.UidInbox = from.UidInbox;
            this.Entity.UidTrash = from.UidTrash;
            this.Entity.UidSent = from.UidSent;
            this.Entity.UidDraft = from.UidDraft;
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
    }
}