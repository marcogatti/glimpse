using Elmah;
using Glimpse.Exceptions;
using System;

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