using System;
using System.Collections;
using System.Linq;
using System.Web;
using NHibernate.Linq;
using NHibernate;
using NHibernate.Criterion;
using System.Collections.Generic;

namespace Glimpse.DataAccessLayer.Entities
{
    public class MailEntity
    {
        public virtual Int64 Id { get; set; }
        public virtual MailAccountEntity MailAccountEntity { get; set; }
        public virtual AddressEntity From { get; set; }
        public virtual IList<LabelEntity> Labels{ get; set; }
        public virtual IList<ExtraEntity> Extras { get; set; }
        public virtual UInt64 Gm_tid { get; set; }
        public virtual UInt64 Gm_mid { get; set; }
        public virtual DateTime Date { get; set; }
        public virtual String Subject { get; set; }
        public virtual Int64 UidInbox { get; set; }
        public virtual Int64 UidTrash { get; set; }
        public virtual Int64 UidSent { get; set; }
        public virtual Int64 UidDraft { get; set; }
        public virtual Int64 UidSpam { get; set; }
        public virtual Int64 UidAll { get; set; }
        public virtual Boolean Answered { get; set; }
        public virtual Boolean Flagged { get; set; }
        public virtual Boolean Seen { get; set; }
        public virtual Boolean Draft { get; set; }
        public virtual Boolean HasExtras { get; set; }
        public virtual String ToAddr { get; set; }
        public virtual String CC { get; set; }
        public virtual String BCC { get; set; }
        public virtual String Body { get; set; }

        public MailEntity()
        {
            this.Labels = new List<LabelEntity>();
            this.Extras = new List<ExtraEntity>();
        }
    }
}