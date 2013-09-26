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
        public virtual UserEntity User { get; set; }
        public virtual String Address { get; set; }
        public virtual String Password { get; set; }
        public virtual Boolean Active { get; set; }

        public MailAccountEntity() { }

        public MailAccountEntity(String address, String password) : this()
        {
            this.Address = address;
            this.Password = password;
        }
    }
}