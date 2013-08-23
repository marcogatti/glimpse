using FluentNHibernate.Automapping;
using FluentNHibernate.Automapping.Alterations;
using Glimpse.DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.DataAccessLayer.Mappings
{
    public class MailAccountAutomappingOverride : IAutoMappingOverride<MailAccountEntity>
    {
        public void Override(AutoMapping<MailAccountEntity> mapping)
        {
            mapping.Map(x => x.Address).Unique().Not.Nullable();
            mapping.Map(x => x.Password).Not.Nullable();
            mapping.HasMany<MailEntity>(x => x.Mails);
            mapping.HasMany<LabelEntity>(x => x.Labels);
        }
    }
}