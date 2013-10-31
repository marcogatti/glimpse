using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using Glimpse.DataAccessLayer.Entities;

namespace Glimpse.DataAccessLayer.Mappings
{
    public class ExtraMap : ClassMap<ExtraEntity>
    {
        public ExtraMap()
        {
            Id(x => x.Id).Column("Id").GeneratedBy.Identity();
            Map(x => x.FileType).Column("FileType");
            Map(x => x.Name).Column("Name");
            Map(x => x.Size).Column("Size");
            Map(x => x.ExtraType).Column("ExtraType");
            Map(x => x.EmbObjectContentId).Column("EmbObjectContentId");
            Map(x => x.Data).Column("Data").LazyLoad();
            References<MailEntity>(x => x.MailEntity).Column("MailId")
                                                    .Cascade.None();

            Table("EXTRA");
        }
    }
}