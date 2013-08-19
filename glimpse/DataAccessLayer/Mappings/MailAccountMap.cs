using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using Glimpse.DataAccessLayer.Entities;

namespace Glimpse.DataAccessLayer.Mappings
{
    public class MailAccountMap : ClassMap<MailAccount>
    {
        public MailAccountMap()
        {
            Table("MailAccount");

            Id(x => x.Id).Column("ID_MailAccount").GeneratedBy.Identity();
            Map(x => x.Address).Column("Address");
            Map(x => x.Password).Column("Password");
        }
    }
}