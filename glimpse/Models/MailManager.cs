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

        public const int ALL_MAILS = int.MaxValue;

        public MailManager(MailAccount mailAccount)
        {
            this.mailAccount = mailAccount;
        }

        private void Save(List<Mail> mails)
        {
            ISession session = NHibernateManager.OpenSession();

            ITransaction tran = session.BeginTransaction();

            foreach (Mail mailToSave in mails)
            {
                Address foundAddress = Address.FindByAddress(mailToSave.Entity.From.MailAddress, session);

                if (foundAddress.Entity == null)
                {
                    session.SaveOrUpdate(mailToSave.Entity.From);
                }
                else
                {
                    mailToSave.setFrom(foundAddress.Entity);
                }

                session.SaveOrUpdate(mailToSave.Entity);
            }

            tran.Commit();
            session.Close();
        }

        public MailCollection GetMailsFrom(string tag, int amountOfEmails, ISession session)
        {
            List<MailEntity> mailList = (List<MailEntity>)session.CreateCriteria<MailEntity>()
                                                .Add(Restrictions.Eq("MailAccountEntity", this.mailAccount.Entity))
                                                .AddOrder(Order.Desc("Date"))
                                                .SetMaxResults(amountOfEmails)
                                                .List<MailEntity>();

            return new MailCollection(mailList);
        }

        public void FetchAndSaveMails(Label label, Int64 fromUid, Int64 toUid)
        {
            this.mailAccount.FetchAndSaveMails(label, fromUid, toUid);
        }
    }
}