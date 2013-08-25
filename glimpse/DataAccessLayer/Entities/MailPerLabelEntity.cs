using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.DataAccessLayer.Entities
{
    public class MailPerLabelEntity
    {
        public virtual Int64 Id { get; set; }
        public virtual LabelEntity Label { get; set; }
        public virtual MailEntity Mail { get; set; }
        public virtual Int64 Uid { get; set; }
    }
}