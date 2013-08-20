using Glimpse.Exceptions;
using Glimpse.MailInterfaces;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.DataAccessLayer.Entities
{
    public class MailAccount
    {
        public virtual Int32 Id { get; set; }
        public virtual String Address { get; set; }
        public virtual String Password { get; set; }

        private static ISession currentSession = NHibernateManager.DefaultSesion;

        public MailAccount() { }

        public MailAccount(String emailAddress, String password)
        {
            this.Address = emailAddress;
            this.Password = password;
        }

        public virtual void Save()
        {
            MailAccount oldAccount = FindByAddress(this.Address);

            ITransaction tran = currentSession.BeginTransaction();

            if (oldAccount == null)
            {
                currentSession.Save(this);
            }
            else
            {
                oldAccount.Password = this.Password;
                currentSession.Update(oldAccount);
            }

            tran.Commit();
        }

        public static MailAccount FindByAddress(String emailAddress)
        {
            MailAccount foundAccount = currentSession.CreateCriteria<MailAccount>()
                                                     .Add(Restrictions.Eq("Address", emailAddress))
                                                     .UniqueResult<MailAccount>();

            return foundAccount;
        }

        public virtual AccountInterface LoginExternal()
        {
            return new AccountInterface(this.Address, this.Password);
        }

    }
}