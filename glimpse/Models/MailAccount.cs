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


        public virtual void SaveOrUpdate()
        {
            ISession currentSession = NHibernateManager.OpenSession();
            ITransaction tran = currentSession.BeginTransaction();

            MailAccount persistAccount;

            MailAccount oldAccount = FindByAddress(this.Entity.Address, currentSession);
            if (oldAccount == null)
            {
                persistAccount = this;
            }
            else
            {
                persistAccount = oldAccount;
                persistAccount.CopyEntityDataFrom(this);
            }

            currentSession.SaveOrUpdate(persistAccount.Entity);
   
            tran.Commit();
        }

        private void CopyEntityDataFrom(MailAccount fromAccount)
        {
            this.Entity.Address = fromAccount.Entity.Address;
            this.Entity.Password = fromAccount.Entity.Password;
        }

        public static MailAccount FindByAddress(String emailAddress, ISession session)
        {
            return new MailAccount(session.CreateCriteria<MailAccountEntity>()
                                          .Add(Restrictions.Eq("Address", emailAddress))
                                          .UniqueResult<MailAccountEntity>());
        }

        public static MailAccount FindByAddress(String emailAddress)
        {
            ISession session = NHibernateManager.OpenSession();
            return FindByAddress(emailAddress, session);
        }

        public virtual AccountInterface LoginExternal()
        {
            return new AccountInterface(this.Entity.Address, this.Entity.Password);
        }

    }
}