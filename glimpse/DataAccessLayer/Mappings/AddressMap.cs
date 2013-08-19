using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using Glimpse.DataAccessLayer.Entities;

namespace Glimpse.DataAccessLayer.Mappings
{
    public class AddressMap : ClassMap<Address>
    {
        public AddressMap()
        {
            Table("Address");

            Id(x => x.Id).Column("ID_Address").GeneratedBy.Identity(); ;
            Map(x => x.MailAddress).Column("Address").Unique();
            Map(x => x.Name).Column("Name");      
        }
    }
}