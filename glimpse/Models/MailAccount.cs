using Glimpse.DataAccessLayer;
using Glimpse.DataAccessLayer.Entities;
using Glimpse.MailInterfaces;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.Models
{
    public class MailAccount
    {
        private MailAccountEntity _entity;
        public MailAccountEntity Entity
        {
            get
            {
                return _entity;
            }
        }

        public MailAccount(MailAccountEntity entity)
        {
            this._entity = entity;
        }


        public virtual void Save()
        {
            MailAccount oldAccount = FindByAddress(this.Entity.Address);
            ISession currentSession = NHibernateManager.OpenSession();
            ITransaction tran = currentSession.BeginTransaction();

            if (oldAccount == null)
            {
                currentSession.Save(this);
            }
            else
            {
                oldAccount.Entity.Password = this.Entity.Password;
                currentSession.Update(oldAccount);
            }

            tran.Commit();
        }

        public static MailAccount FindByAddress(String emailAddress)
        {
            return new MailAccount(NHibernateManager.OpenSession()
                                                    .CreateCriteria<MailAccountEntity>()
                                                    .Add(Restrictions.Eq("Address", emailAddress))
                                                    .UniqueResult<MailAccountEntity>());
        }

        public virtual AccountInterface LoginExternal()
        {
            return new AccountInterface(this.Entity.Address, this.Entity.Password);
        }

    }
}