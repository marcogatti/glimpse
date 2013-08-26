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
        public virtual IList<MailPerLabelEntity> MailsPerLabel { get; set; }
        public virtual Boolean IsSystemLabel { get; set; }

        public LabelEntity()
        {
            this.MailsPerLabel = new List<MailPerLabelEntity>();
        }
    }
}