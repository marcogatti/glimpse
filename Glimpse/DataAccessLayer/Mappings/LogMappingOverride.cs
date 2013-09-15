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
    public class LogMappingOverride : IAutoMappingOverride<LogEntity>
    {
        public void Override(AutoMapping<LogEntity> mapping)
        {
            mapping.Map(x => x.Message).Length(500).Not.Nullable();
            mapping.Map(x => x.StackTrace).Length(4000);
        }
    }
}