using Glimpse.DataAccessLayer;
using Glimpse.DataAccessLayer.Entities;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.Models
{
    public class Log
    {
        public LogEntity Entity { get; private set; }

        public Log(LogEntity entity)
        {
            this.Entity = entity;
        }
        public void Save()
        {
            ISession session = NHibernateManager.OpenSession();
            ITransaction tran = session.BeginTransaction();
            session.SaveOrUpdate(this.Entity);
            tran.Commit();
            session.Flush();
            session.Close();
        }

    }
}