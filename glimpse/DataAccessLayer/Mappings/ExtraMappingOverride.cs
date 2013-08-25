using FluentNHibernate.Automapping;
using FluentNHibernate.Automapping.Alterations;
using Glimpse.DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.DataAccessLayer.Mappings
{
    public class ExtraMappingOverride : IAutoMappingOverride<ExtraEntity>
    {
        public void Override(AutoMapping<ExtraEntity> mapping)
        {
            mapping.Map(x => x.FileType).Length(15);
            mapping.References<MailEntity>(x => x.MailEntity).Cascade.None();
        }
    }
}