using FluentNHibernate.Automapping;
using FluentNHibernate.Automapping.Alterations;
using Glimpse.DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.DataAccessLayer.Mappings
{
    public class MailPerLabelAutomappinOverride : IAutoMappingOverride<MailPerLabelEntity>
    {
        public void Override(AutoMapping<MailPerLabelEntity> mapping)
        {
            mapping.HasOne<MailEntity>(x => x.Mail);
            mapping.HasOne<MailEntity>(x => x.Label);
        }
    }
}