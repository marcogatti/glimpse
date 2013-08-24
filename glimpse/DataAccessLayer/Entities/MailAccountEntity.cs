using Glimpse.Exceptions;
using Glimpse.MailInterfaces;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.DataAccessLayer.Entities
{
    public class MailAccountEntity
    {
        public virtual Int64 Id { get; set; }
        public virtual String Address { get; set; }
        public virtual String Password { get; set; }
        public virtual IList<LabelEntity> Labels { get; set; }
        public virtual IList<MailEntity> Mails { get; set; }

        public MailAccountEntity()
        {
            this.Labels = new List<LabelEntity>();
            this.Mails = new List<MailEntity>();
        }

        public MailAccountEntity(string address, string password) : this()
        {
            this.Address = address;
            this.Password = password;
        }
    }
}