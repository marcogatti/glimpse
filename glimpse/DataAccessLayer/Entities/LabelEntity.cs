using NHibernate;
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
        public virtual String Color { get; set; }
        public virtual Boolean Active { get; set; }

        public LabelEntity() { }

        public LabelEntity(String name, String systemName)
        {
            this.Name = name;
            this.SystemName = systemName;
            this.Active = true;
        }
    }
}