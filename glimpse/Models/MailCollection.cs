using Glimpse.DataAccessLayer;
using Glimpse.DataAccessLayer.Entities;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.Models
{
    public class MailCollection : List<MailEntity>
    {
        private static ISession currentSession = NHibernateManager.DefaultSesion;

        public void Save()
        {
            ITransaction tran = currentSession.BeginTransaction();

            currentSession.Save(this);

            tran.Commit();
        }
    }
}