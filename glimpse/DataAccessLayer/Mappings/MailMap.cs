using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using Glimpse.DataAccessLayer.Entities;

namespace Glimpse.DataAccessLayer.Mappings
{
    public class MailMap : ClassMap<Mail>
    {
        public MailMap()
        {

            Table("Mail");
        }
    }
}