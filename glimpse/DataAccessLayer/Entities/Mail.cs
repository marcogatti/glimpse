using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate.Linq;

namespace Glimpse.DataAccessLayer.Entities
{
    public class Mail
    {
        public virtual int Id { get; set; }
        //public virtual MailAccount IdMailAccount { get; set; }
        public virtual Address From { get; set; }
        public virtual DateTime Date { get; set; }
        public virtual String To { get; set; }
        public virtual String CC { get; set; }
        public virtual String BCC { get; set; }
        public virtual String Subject { get; set; }
        public virtual String Body { get; set; }
        public virtual Int64 UidInbox { get; set; }
        public virtual Int64 UidTrash { get; set; }
        public virtual Int64 UidSent { get; set; }
        public virtual Int64 UidDraft { get; set; }
        public virtual Int64 UidSpam { get; set; }
        public virtual Int64 UidAll { get; set; }
        public virtual Boolean Answered { get; set; }
        public virtual Boolean Flagged { get; set; }
        public virtual Boolean Seen { get; set; }
        public virtual Boolean Spam { get; set; }
        public virtual Boolean HasAttachments { get; set; }
    }
}