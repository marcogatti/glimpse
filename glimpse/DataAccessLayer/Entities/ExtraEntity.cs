using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.DataAccessLayer.Entities
{
    public class ExtraEntity
    {
        public virtual Int64 Id { get; set; }
        public virtual MailEntity Mail { get; set; }
        public virtual String Name { get; set; }
        public virtual Int32 Size { get; set; }
        public virtual String FileType { get; set; }
        public virtual Int16 ExtraType { get; set; }

        public ExtraEntity() { }
    }
}