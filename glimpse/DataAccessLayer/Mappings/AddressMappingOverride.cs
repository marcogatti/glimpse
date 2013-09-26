using FluentNHibernate.Automapping;
using FluentNHibernate.Automapping.Alterations;
using Glimpse.DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using NHibernate.Mapping;

namespace Glimpse.DataAccessLayer.Mappings
{
    public class AddressMappingOverride : IAutoMappingOverride<AddressEntity>
    {
        public void Override(AutoMapping<AddressEntity> mapping)
        {
            mapping.Map(x => x.MailAddress).Unique().Not.Nullable();
            mapping.Map(x => x.Name).Length(100).Not.Nullable();
        }
    }
}