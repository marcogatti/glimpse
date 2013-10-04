﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using Glimpse.DataAccessLayer.Entities;

namespace Glimpse.DataAccessLayer.Mappings
{
    public class UserMap : ClassMap<UserEntity>
    {
        public UserMap()
        {
            Id(x => x.Id).Column("Id").GeneratedBy.Identity();
            Map(x => x.Username).Column("Username");
            Map(x => x.Password).Column("Password");
            Map(x => x.ShowTutorial).Column("ShowTutorial");

            Table("USER");
        }
    }
}