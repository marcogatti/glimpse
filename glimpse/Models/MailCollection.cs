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
        public void Save(ISession currentSession)
        {
            ITransaction tran = currentSession.BeginTransaction();

            foreach (MailEntity mailToSave in this)
            {
                Address foundAddress = Address.FindByAddress(mailToSave.From.MailAddress, currentSession);

                if (foundAddress.Entity == null)
                {
                    currentSession.SaveOrUpdate(mailToSave.From);
                }
                else
                {
                    mailToSave.From = foundAddress.Entity;
                }

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