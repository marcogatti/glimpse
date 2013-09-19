using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using Glimpse.DataAccessLayer.Entities;

namespace Glimpse.DataAccessLayer.Mappings
{
    public class LabelMap : ClassMap<LabelEntity>
    {
        public LabelMap()
        {
            Id(x => x.Id).Column("Id").GeneratedBy.Identity();
            Map(x => x.Name).Column("Name");
            Map(x => x.SystemName).Column("SystemName");
            References<MailAccountEntity>(x => x.MailAccountEntity).Column("MailAccountId")
                                                                    .Cascade.None();
            
            Table("LABEL");
        }

    }
}