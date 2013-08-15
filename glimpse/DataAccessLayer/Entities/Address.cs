using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate.Linq;

namespace Glimpse.DataAccessLayer.Entities
{
    public class Address
    {
        public virtual int Id { get; set; }
        public virtual String MailAddress { get; set; }
        public virtual String Name { get; set; }
    }
}