using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate.Linq;
using NHibernate.Criterion;
using NHibernate;

namespace Glimpse.DataAccessLayer.Entities
{
    public class AddressEntity
    {
        public virtual Int64 Id { get; set; }
        public virtual String MailAddress { get; set; }
        public virtual String Name { get; set; }

        public AddressEntity(String mailAddress, String name)
        {
            this.MailAddress = mailAddress;
            this.Name = name;
        }

        public AddressEntity() { }
    }
}