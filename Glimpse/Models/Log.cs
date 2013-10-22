using Elmah;
using Glimpse.DataAccessLayer;
using Glimpse.DataAccessLayer.Entities;
using Glimpse.Exceptions;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.Models
{
    public static class Log
    {
        public static void LogException(GlimpseException exc, String contextualMessage = null)
        {
            Exception exceptionToLog;
            String logMessage = "";

            if (!String.IsNullOrEmpty(exc.GlimpseMessage))
                logMessage = exc.GlimpseMessage;

            if (String.IsNullOrEmpty(contextualMessage))
                logMessage += contextualMessage;

            exceptionToLog = new Exception(logMessage, exc);

            Elmah.ErrorLog.GetDefault(null).Log(new Error(exceptionToLog));
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