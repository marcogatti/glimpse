﻿using System;
using System.Collections;
using System.Linq;
using System.Web;
using NHibernate.Linq;
using NHibernate;
using NHibernate.Criterion;
using System.Collections.Generic;

namespace Glimpse.DataAccessLayer.Entities
{
    public class Mail
    {
        public virtual int Id { get; set; }
        public virtual int IdMailAccount { get; set; }
        public virtual Address From { get; set; }
        public virtual long gm_tid { get; set; }
        public virtual long gm_mid { get; set; }
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
        public virtual String To { get; set; }
        public virtual String CC { get; set; }
        public virtual String BCC { get; set; }
        public virtual String Body { get; set; }


        private static ISession currentSession = NHibernateManager.DefaultSesion;


        public Mail() { }

        public virtual Mail Save()
        {
            currentSession.Save(this);
            return this;
        }

        public virtual IList<Mail> FindFromInbox()
        {
            return (IList<Mail>)currentSession.CreateCriteria<Mail>()
                                              .Add(Restrictions.Gt("UID_Inbox", 0)).List();
        }
    }



}