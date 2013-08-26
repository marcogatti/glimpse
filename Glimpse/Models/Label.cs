using Glimpse.DataAccessLayer.Entities;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.Models
{
    public class Label
    {
        public LabelEntity Entity { get; private set; }

        public Label(LabelEntity labelEntity)
        {
            this.Entity = labelEntity;
        }

        public void SaveOrUpdate(ISession session)
        {
            session.SaveOrUpdate(this.Entity);
        }
    }
}