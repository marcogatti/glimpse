using Glimpse.DataAccessLayer;
using Glimpse.DataAccessLayer.Entities;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.Models
{
    public class Mail
    {
        public MailEntity Entity { get; private set; }

        public void setFrom(AddressEntity from)
        {
            this.Entity.From = from;
        }

        public Mail(MailEntity entity)
        {
            this.Entity = entity;
        }
 
        public static List<MailEntity> FindByMailAccount(MailAccount mailAccount, ISession session){

            List<MailEntity> foundMails = (List<MailEntity>)session.CreateCriteria<MailEntity>()
                                                        .Add(Restrictions.Eq("MailAccountEntity", mailAccount.Entity))
                                                        .List<MailEntity>();
            return foundMails;
        }

        public void Save()
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();

            session.SaveOrUpdate(this);

            tran.Commit();
        }
    }
}