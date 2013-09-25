using Elmah;
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

        public static void LogException(Exception exc, String contextualMessage = null)
        {
            Exception exceptionToLog;

            if (contextualMessage != null)
                exceptionToLog = new Exception(contextualMessage, exc);
            else
                exceptionToLog = exc;

            Elmah.ErrorLog.GetDefault(null).Log(new Error(exceptionToLog));
        }

    }
}