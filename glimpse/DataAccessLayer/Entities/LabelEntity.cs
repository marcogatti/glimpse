using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.DataAccessLayer.Entities
{
    public class LabelEntity
    {
        public virtual Int32 Id { get; set; }
        public virtual MailAccountEntity MailAccount { get; set; }
        public virtual String Name { get; set; }
        public virtual IList<MailPerLabel> MailsPerLabel { get; set; }

        public LabelEntity() { }
    }
}