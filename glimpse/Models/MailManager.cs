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

        private MailAccount mailAccount;        

        private static ISession currentSession = NHibernateManager.OpenSession();

        public const int ALL_MAILS = int.MaxValue;

        public MailManager(MailAccount mailAccount)
        {
            this.mailAccount = mailAccount;            
        }

        public List<MailEntity> FetchFromMailbox(String mailbox, int maxAmount = ALL_MAILS)
        {
            MailCollection mails = new MailCollection();

            Int64 lastDatabaseUID = currentSession.CreateCriteria<MailEntity>()
                                                  .Add(Restrictions.Eq("MailAccount.Id", this.mailAccount.Entity.Id))
                                                  .SetProjection(Projections.Max("UidInbox"))
                                                  .UniqueResult<Int64>();

            Int32 lastImapUID = this.mailAccount.getLastUIDFrom(mailbox);
            if (lastImapUID > lastDatabaseUID)
            {
                mails = this.mailAccount.getMailsFromHigherThan(mailbox, lastDatabaseUID);
                mails.loadMailAccount(this.mailAccount);
                mails.Save(currentSession);
            }

            List<MailEntity> returnMails = mails.ToList<MailEntity>();

            if (maxAmount > mails.Count)
            { 
                returnMails.AddRange(currentSession.CreateCriteria<MailEntity>()
                                               .Add(Restrictions.Eq("MailAccount", this.mailAccount.Entity))
                                               .Add(Restrictions.Le("UidInbox", lastDatabaseUID))
                                               .AddOrder(Order.Desc("UidInbox"))
                                               .SetMaxResults(maxAmount - mails.Count)
                                               .List<MailEntity>());
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