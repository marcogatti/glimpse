using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.DataAccessLayer.Entities
{
    public class ExtraEntity
    {
        public virtual Int64 Id { get; set; }
        public virtual String FileType { get; set; }
        public virtual String Name { get; set; }
        public virtual UInt32 Size { get; set; }
        public virtual Int16 ExtraType { get; set; }
        public virtual String EmbObjectContentId { get; set; }
        public virtual Byte[] Data { get; set; }
        public virtual MailEntity MailEntity { get; set; }
    }
}