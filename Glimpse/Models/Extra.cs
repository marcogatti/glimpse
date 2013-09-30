using Glimpse.DataAccessLayer;
using Glimpse.DataAccessLayer.Entities;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.Models
{
    public class Extra
    {
        public ExtraEntity Entity;

        public Extra(ExtraEntity entity)
        {
            this.Entity = entity;
        }

        public static Extra FindByID(Int64 id)
        {
            ISession session = NHibernateManager.OpenSession();
            ExtraEntity extraEntity = session.CreateCriteria<ExtraEntity>()
                                         .Add(Restrictions.Eq("Id", id))
                                         .UniqueResult<ExtraEntity>();
            session.Close();
            return new Extra(extraEntity);
        }
    }
}