using Glimpse.DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.Models
{
    public class MailCollection : List<MailEntity>
    {
        public MailCollection(IList<MailEntity> mails)
        {
            this.AddRange(mails);
        }
    }
}