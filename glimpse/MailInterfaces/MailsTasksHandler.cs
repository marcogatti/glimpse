using Glimpse.DataAccessLayer;
using Glimpse.Models;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Glimpse.MailInterfaces
{
    public class MailsTasksHandler
    {
        private static Dictionary<string, Task<string>> tasksList = new Dictionary<string, Task<string>>();
        private static Mutex tasksListLock = new Mutex(false);

        public static void StartSynchronization(MailAccount mailAccount)
        {
            LockTasksList();

            if (!tasksList.ContainsKey(mailAccount.Entity.Address))
            {
                tasksList[mailAccount.Entity.Address] = Task.Factory.StartNew<string>(() => SynchronizeAccount(mailAccount));
            }

            UnlockTasksList();
        }

        private static string SynchronizeAccount(MailAccount mailAccount)
        {
            ISession session = NHibernateManager.OpenSession();

            new MailManager(mailAccount).FetchFromMailbox("INBOX", session);

            session.Flush();
            session.Close();

            EndSynchronization(mailAccount);

            return "OK";
        }

        private static void EndSynchronization(MailAccount mailAccount)
        {
            LockTasksList();

            if (tasksList.ContainsKey(mailAccount.Entity.Address))
            {
                tasksList.Remove(mailAccount.Entity.Address);
            }

            UnlockTasksList();
        }

        private static void UnlockTasksList()
        {
            tasksListLock.ReleaseMutex();
        }

        private static void LockTasksList()
        {
            tasksListLock.WaitOne();
        }
    }
}