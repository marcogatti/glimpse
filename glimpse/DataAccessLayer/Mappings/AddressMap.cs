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
            Id(x => x.Id).Column("ID_Address");
            Map(x => x.MailAddress).Column("Address");
            Map(x => x.Name).Column("Name");

            Table("Address");
        }
    }
}