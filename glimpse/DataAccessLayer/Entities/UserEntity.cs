﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Glimpse.DataAccessLayer.Entities
{
    public class UserEntity
    {
        public virtual Int64 Id { get; set; }
        public virtual String Username { get; set; }
        public virtual String Password { get; set; }
        public virtual Boolean ShowTutorial { get; set; }
    }
}