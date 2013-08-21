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
        private MailAccountEntity account;

        private static ISession currentSession = NHibernateManager.DefaultSesion;

        public const int ALL_MAILS = int.MaxValue;


        public MailManager(AccountInterface accountInterface, MailAccountEntity account)
        {
            this.accountInterface = accountInterface;
            this.account = account;
        }

        public MailCollection FetchFromMailbox(String mailbox, int maxAmount = ALL_MAILS)
        {
            MailCollection mails = new MailCollection();

            Int64 lastDatabaseUID = currentSession.CreateCriteria<MailEntity>()
                                                  .Add(Restrictions.Eq("MailAccount.Id", this.account.Id))
                                                  .SetProjection(Projections.Max("UidInbox")).UniqueResult<Int64>();

            mails = this.accountInterface.getMailsFromHigherThan(mailbox, lastDatabaseUID);
            mails.Save();
            if (maxAmount > mails.Count)
            {
                mails.AddRange((MailCollection)currentSession.CreateCriteria<MailEntity>()
                                               .Add(Restrictions.Eq("ID_MailAccount", this.account.Id))
                                               .Add(Restrictions.Le("UID_Inbox", lastDatabaseUID))
                                               .AddOrder(Order.Desc("UID_Inbox"))
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