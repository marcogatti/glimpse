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

        public virtual MailAccount CreateOrUpdate()
        {
            MailAccount newAccount = this;
            MailAccount oldAccount = FindByAddress(newAccount.Address);
            MailAccount returnAccount;

            ITransaction tran = currentSession.BeginTransaction();

            if (oldAccount == null)
            {
                currentSession.Save(newAccount);
                returnAccount = newAccount;
            }
            else
            {
                ResetWithOtherAddress(oldAccount, newAccount);
                currentSession.Update(oldAccount);
                returnAccount = oldAccount;
            }

            tran.Commit();

            return returnAccount;
        }

        private void ResetWithOtherAddress(MailAccount from, MailAccount to)
        {
            to.Address = from.Address;
            to.Password = from.Password;
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