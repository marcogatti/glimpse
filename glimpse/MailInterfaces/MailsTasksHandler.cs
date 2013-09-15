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
        private static Dictionary<string, MailsTask> tasksList = new Dictionary<string, MailsTask>();
        private static Mutex tasksListLock = new Mutex(false);

        private static int MAILS_AMMOUNT_PER_PASS = 5;


        public static void StartSynchronization(MailAccount mailAccount)
        {
            bool taskIsWorking;
            String mailAddress = mailAccount.Entity.Address;

            LockTasksList();

            if (tasksList.ContainsKey(mailAddress) && tasksList[mailAddress].IsWorking)
                taskIsWorking = true;
            else
                taskIsWorking = false;

            UnlockTasksList();

            if (!taskIsWorking)
            {
                ISession session = NHibernateManager.OpenSession();
                Label label = Label.FindBySystemName(mailAccount, "INBOX", session);
                Int64 lastUidExternal = mailAccount.getLastUIDExternalFrom("INBOX"); //TODO: Deshardcodear
                Int64 lastUidLocal = mailAccount.GetLastUIDLocalFromALL(session);

                MailsTask task = new MailsTask(lastUidLocal, lastUidExternal, label, session);

                if (task.HasFinished)
                {
                    session.Close();
                    return;
                }

                LockTasksList();
                tasksList[mailAccount.Entity.Address] = task;
                UnlockTasksList();

                StartMailsTask(mailAccount, task);
            }
        }

        private static void StartMailsTask(MailAccount mailAccount, MailsTask task)
        {
            Task.Factory.StartNew(() => SynchronizeAccount(mailAccount, task));
        }

        private static void SynchronizeAccount(MailAccount mailAccount, MailsTask task)
        {
            Int64 toUid, fromUid;
            ISession currentSession = task.session;


            toUid = task.NextUid;
            fromUid = CalculateFromUid(toUid, task.UidLocal);

            new MailManager(mailAccount).FetchAndSaveMails(task.Label, fromUid, toUid);

            currentSession.Flush();

            task.Dirty = true;

            PrepareForNextRun(task, fromUid);

            if (!task.HasFinished)
            {
                StartMailsTask(mailAccount, task);
            }
            else
            {
                EndSynchronization(mailAccount, task);
            }
        }

        private static void EndSynchronization(MailAccount mailAccount, MailsTask task)
        {
            task._working = false;
        }

        private static void PrepareForNextRun(MailsTask task, Int64 fromUid)
        {
            task.NextUid = fromUid > 0 ? fromUid - 1 : 0;
        }

        private static Int64 CalculateFromUid(Int64 toUid, Int64 lastUidLocal)
        {
            Int64 fromUid;
            if (toUid - MAILS_AMMOUNT_PER_PASS > 0)
                fromUid = toUid - MAILS_AMMOUNT_PER_PASS;
            else
                fromUid = lastUidLocal;
            return fromUid;
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

    public class MailsTask
    {
        internal bool _working;
        internal Int64 UidExternal { get; set; }
        internal Int64 UidLocal { get; set; }
        internal Int64 NextUid { get; set; }
        internal Label Label { get; set; }
        internal ISession session { get; set; }

        public bool IsWorking
        {
            get
            {
                return this._working;
            }
        }
        public bool Dirty { get; set; }

        public bool HasFinished
        {
            get
            {
                return this.UidLocal == this.NextUid;
            }
        }



        public MailsTask(Int64 lastUidLocal, Int64 lastUidExternal, Label label, ISession session)
        {
            this.UidLocal = lastUidLocal;
            this.NextUid = this.UidExternal = lastUidExternal;
            this.Label = label;
            this.session = session;
            this.Dirty = false;
            this._working = true;
        }
    }
}