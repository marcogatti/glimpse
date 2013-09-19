using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using Glimpse.DataAccessLayer.Entities;

namespace Glimpse.DataAccessLayer.Mappings
{
    public class LogMap : ClassMap<LogEntity>
    {
        public LogMap()
        {
            Id(x => x.Id).Column("Id").GeneratedBy.Identity();
            Map(x => x.Message).Column("Message");
            Map(x => x.StackTrace).Column("StackTrace");
            Map(x => x.Code).Column("Code");
            Map(x => x.Date).Column("Date");

            Table("LOG");
        }
    }
}