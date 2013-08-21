using Glimpse.DataAccessLayer;
using Glimpse.DataAccessLayer.Entities;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.Models
{
    public class MailCollection : List<MailEntity>, IList<MailEntity>
    {
        private static ISession currentSession = NHibernateManager.DefaultSesion;

        public void Save()
        {
            ITransaction tran = currentSession.BeginTransaction();

            foreach (MailEntity mailToSave in this)
            {
                currentSession.SaveOrUpdate(mailToSave.From);
                currentSession.SaveOrUpdate(mailToSave);
            }

            tran.Commit();
        }

        public void loadMailAccount(MailAccount mailAccount)
        {
            foreach (MailEntity mail in this)
            {
                mail.MailAccount = mailAccount.Entity;
            }
        }
    }
}