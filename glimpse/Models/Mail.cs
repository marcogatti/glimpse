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
    public class Mail
    {
        private MailEntity _entity;
        public MailEntity Entity
        {
            get
            {
                return _entity;
            }
        }

        public Mail(MailEntity entity)
        {
            this._entity = entity;
        }
 
        public static IList<MailEntity> FetchFromInbox(MailAccount mailAccount, int maxAmount){
            return NHibernateManager.OpenSession()
                                    .CreateCriteria<MailEntity>()
                                    .Add(Restrictions.Eq("MailAccountEntity", mailAccount.Entity))
                                    .List<MailEntity>();
        }

        public void Save()
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();

            session.SaveOrUpdate(this);

            tran.Commit();
        }
    }
}