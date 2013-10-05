using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using Glimpse.DataAccessLayer.Entities;

namespace Glimpse.DataAccessLayer.Mappings
{
    public class MailAccountMap : ClassMap<MailAccountEntity>
    {
        public MailAccountMap()
        {
            Id(x => x.Id).Column("Id").GeneratedBy.Identity();
            References<UserEntity>(x => x.User).Column("UserId")
                                               .Cascade.None();
            Map(x => x.Address).Column("Address");
            Map(x => x.Password).Column("Password");
            Map(x => x.Active).Column("Active");
            Map(x => x.IsMainAccount).Column("IsMainAccount");
            Map(x => x.OldestMailDate).Column("OldestMailDate");

            Table("MAILACCOUNT");
        }
    }
}