using FluentNHibernate.Automapping;
using FluentNHibernate.Automapping.Alterations;
using Glimpse.DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.DataAccessLayer.Mappings
{
    public class LabelAutomappingOverride : IAutoMappingOverride<LabelEntity>
    {
        public void Override(AutoMapping<LabelEntity> mapping)
        {
            mapping.HasOne<MailAccountEntity>(x => x.MailAccount);
        }

    }
}