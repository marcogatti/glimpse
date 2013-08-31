﻿using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.DataAccessLayer.Entities
{
    public class LabelEntity
    {
        public virtual Int32 Id { get; set; }
        public virtual MailAccountEntity MailAccountEntity { get; set; }
        public virtual String Name { get; set; }
        public virtual String SystemName { get; set; }
        public virtual IList<MailEntity> Mails { get; set; }
        

        public LabelEntity()
        {
            this.Mails = new List<MailEntity>();
        }
    }
}