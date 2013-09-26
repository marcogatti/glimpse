using Glimpse.DataAccessLayer.Entities;
using Glimpse.Exceptions;
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

        public static Label FindBySystemName(MailAccount account, String systemName, ISession session){

            LabelEntity labelEntity;

            try
            {
                labelEntity = session.CreateCriteria<LabelEntity>()
                                          .Add(Restrictions.Eq("MailAccountEntity", account.Entity))
                                          .Add(Restrictions.Eq("SystemName", systemName))
                                          .UniqueResult<LabelEntity>();
            }
            catch (NHibernate.HibernateException e)
            {
                throw new NotUniqueResultException(e, "Cuenta: " + account.Entity.Address + " ,SystemName Label: " + systemName);
            }

            return new Label(labelEntity);
        }
    }
}