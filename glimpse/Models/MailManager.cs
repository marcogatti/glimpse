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

        public List<Mail> FetchFromMailbox(String mailbox, int maxAmount = ALL_MAILS)
        {
            List<Mail> imapMails = new List<Mail>();

            Int64 lastDatabaseUID = currentSession.CreateCriteria<MailEntity>()
                                                  .Add(Restrictions.Eq("MailAccountEntity", this.mailAccount.Entity))
                                                  .SetProjection(Projections.Max("UidInbox")).UniqueResult<Int64>();
            Int32 lastImapUID = this.mailAccount.getLastUIDFrom(mailbox);

            if (lastImapUID > lastDatabaseUID)
            {
                imapMails = this.mailAccount.getMailsFromHigherThan(mailbox, lastDatabaseUID);
                foreach (Mail mail in imapMails)
                {
                    mail.Entity.MailAccountEntity = mailAccount.Entity;
                }

                this.Save(imapMails);
            }

            List<Mail> returnMails = imapMails;

            if (maxAmount > imapMails.Count)
            {
                List<MailEntity> mailList = (List<MailEntity>)currentSession.CreateCriteria<MailEntity>()
                                                .Add(Restrictions.Eq("MailAccountEntity", this.mailAccount.Entity))
                                                .Add(Restrictions.Le("UidInbox", lastDatabaseUID))
                                                .AddOrder(Order.Desc("UidInbox"))
                                                .SetMaxResults(maxAmount - imapMails.Count)
                                                .List<MailEntity>();

                foreach (MailEntity mail in mailList)
                {
                    returnMails.Add(new Mail(mail));
                }
            }
            else
            {
                returnMails = returnMails.Take<Mail>(maxAmount).ToList<Mail>();
            }

            currentSession.Flush();

            return returnMails;
        }

        private void Save(List<Mail> mails)
        {
            ITransaction tran = currentSession.BeginTransaction();

            foreach (Mail mailToSave in mails)
            {
                Address foundAddress = Address.FindByAddress(mailToSave.Entity.From.MailAddress, currentSession);

                if (foundAddress.Entity == null)
                {
                    currentSession.SaveOrUpdate(mailToSave.Entity.From);
                }
                else
                {
                    mailToSave.setFrom(foundAddress.Entity);
                }

                currentSession.SaveOrUpdate(mailToSave.Entity);
            }

            tran.Commit();
        }
    }
}