using FluentNHibernate.Automapping;
using FluentNHibernate.Automapping.Alterations;
using Glimpse.DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.DataAccessLayer.Mappings
{
    public class MailMappingOverride : IAutoMappingOverride<MailEntity>
    {
        public void Override(AutoMapping<MailEntity> mapping)
        {
            mapping.Map(x => x.ToAddr).CustomSqlType("TEXT");
            mapping.Map(x => x.CC).CustomSqlType("TEXT");
            mapping.Map(x => x.BCC).CustomSqlType("TEXT");
            mapping.Map(x => x.Body).CustomSqlType("LONGTEXT").LazyLoad();
            mapping.References<MailAccountEntity>(x => x.MailAccountEntity).Cascade.None();
            mapping.HasMany<ExtraEntity>(x => x.Extras).Inverse().Cascade.All();
            mapping.HasMany<MailPerLabelEntity>(x => x.LabelsPerMail).Inverse().Cascade.None();
            mapping.Map(x => x.ToAddr).Not.LazyLoad();
        }
    }
}