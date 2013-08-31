using Glimpse.DataAccessLayer.Entities;
using NHibernate;
using NHibernate.Criterion;
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

        public static IList<LabelEntity> FindByAccount(MailAccountEntity account, ISession session)
        {
            IList<LabelEntity> labels = session.CreateCriteria<LabelEntity>()
                                              .Add(Restrictions.Eq("MailAccountEntity", account))
                                              .List<LabelEntity>();
            return labels;
        }

        public void SaveOrUpdate(ISession session)
        {
            session.SaveOrUpdate(this.Entity);
        }
    }
}