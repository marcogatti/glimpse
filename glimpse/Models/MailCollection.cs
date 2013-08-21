using Glimpse.DataAccessLayer;
using Glimpse.DataAccessLayer.Entities;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.Models
{
    public class MailCollection : List<Mail>
    {
        private static ISession currentSession = NHibernateManager.DefaultSesion;

        public void Save()
        {
            ITransaction tran = currentSession.BeginTransaction();

            //currentSession.Save(this);

            foreach(Mail mailToSave in this)
            {
                mailToSave.Save();
            }
            tran.Commit();
        }

        public void loadMailAccount(MailAccount mailAccount)
        {
            foreach (Mail mail in this)
            {
                mail.MailAccount = mailAccount;
            }
        }
    }
}