using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using Glimpse.DataAccessLayer.Entities;

namespace Glimpse.DataAccessLayer.Mappings
{
    public class AddressMap : ClassMap<AddressEntity>
    {
        public AddressMap()
        {
            Id(x => x.Id).Column("Id").GeneratedBy.Identity();
            Map(x => x.MailAddress).Column("MailAddress");
            Map(x => x.Name).Column("Name");
            
            Table("ADDRESS");
        }
    }
}