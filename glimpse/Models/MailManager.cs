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

        public List<MailEntity> FetchFromMailbox(String mailbox, int maxAmount = ALL_MAILS)
        {
            MailCollection mails = new MailCollection();

            Int64 lastDatabaseUID = currentSession.CreateCriteria<MailEntity>()
                                                  .Add(Restrictions.Eq("MailAccountEntity", this.account.Entity))
                                                  .SetProjection(Projections.Max("UidInbox")).UniqueResult<Int64>();
            Int32 lastImapUID = this.accountInterface.getLastUIDFrom(mailbox);
            if (lastImapUID > lastDatabaseUID)
            {
                mails = this.accountInterface.getMailsFromHigherThan(mailbox, lastDatabaseUID);
                mails.loadMailAccount(this.account);
                mails.Save(currentSession);
            }

            List<MailEntity> returnMails = mails.ToList<MailEntity>();

            if (maxAmount > mails.Count)
            {
                IList<MailEntity> dbMails = currentSession.CreateCriteria<MailEntity>()
                                               .Add(Restrictions.Eq("MailAccountEntity", this.account.Entity))
                                               .Add(Restrictions.Le("UidInbox", lastDatabaseUID))
                                               .AddOrder(Order.Desc("UidInbox"))
                                               .SetMaxResults(maxAmount - mails.Count)
                                               .List<MailEntity>();

                returnMails.AddRange(dbMails);
            }
            else
            {
                returnMails = (List<MailEntity>)returnMails.Take<MailEntity>(maxAmount);
            }

            currentSession.Flush();

            return returnMails;
        }
    }
}