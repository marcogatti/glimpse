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
    public class MailManager
    {

        private AccountInterface accountInterface;
        private MailAccount account;

        private static ISession currentSession = NHibernateManager.DefaultSesion;

        public const int ALL_MAILS = int.MaxValue;


        public MailManager(AccountInterface accountInterface, MailAccount account)
        {
            this.accountInterface = accountInterface;
            this.account = account;
        }

        public MailCollection FetchFromMailbox(String mailbox, int maxAmount = ALL_MAILS)
        {
            MailCollection mails = new MailCollection();

            Int32 lastDatabaseUID = (Int32)currentSession.CreateCriteria<Mail>()
                                                  .Add(Restrictions.Eq("MailAccount.Id", this.account.Id))
                                                  .SetProjection(Projections.Max("UidInbox")).UniqueResult<Int64>();

            mails = this.accountInterface.getMailsFromHigherThan(mailbox, lastDatabaseUID);
            mails.loadMailAccount(this.account);
            mails.Save();
            if (maxAmount > mails.Count)
            {
                mails.AddRange((MailCollection)currentSession.CreateCriteria<Mail>()
                                               .Add(Restrictions.Eq("MailAccount", this.account.Id))
                                               .Add(Restrictions.Le("UidInbox", lastDatabaseUID))
                                               .AddOrder(Order.Desc("UidInbox"))
                                               .SetMaxResults(maxAmount - mails.Count)
                                               .List());
                return mails;
            }
            else
            {
                return (MailCollection)mails.Take(maxAmount);
            }
        }
    }
}