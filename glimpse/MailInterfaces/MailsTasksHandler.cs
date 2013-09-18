using Glimpse.DataAccessLayer;
using Glimpse.DataAccessLayer.Entities;
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
                Int64 firstUidExternal = mailAccount.getFirstUIDExternalFrom("INBOX"); //TODO: Deshardcodear
                Int64 lastUidLocal = mailAccount.GetLastUIDLocalFromALL(session);
                Int64 firstUidLocal = mailAccount.GetFirstUIDLocalFromALL(session);
                session.Close();

                MailsTask task = new MailsTask(lastUidLocal, firstUidLocal, lastUidExternal, firstUidExternal, label);

                if (task.HasFinished)
                {
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

            try
            {
                toUid = task.NextUid;
                fromUid = CalculateFromUid(toUid, task.HighestUidLocal);

                mailAccount.FetchAndSaveMails(task.Label, fromUid, toUid);

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
            catch (Exception exc)
            {
                EndSynchronization(mailAccount, task);

                Log logger = new Log(new LogEntity(003, "Error generico SynchronizeAccount. Parametros: mailAddress(" + mailAccount.Entity.Address + ").", exc.StackTrace));
                logger.Save();
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
        internal Int64 HighestUidExternal { get; set; }
        internal Int64 LowestUidExternal { get; set; }
        internal Int64 HighestUidLocal { get; set; }
        internal Int64 LowestUidLocal { get; set; }
        internal Int64 NextUid { get; set; }
        internal Label Label { get; set; }

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
                return this.HighestUidLocal == this.NextUid;
            }
        }



        public MailsTask(Int64 lastUidLocal,Int64 firstUidLocal, Int64 lastUidExternal, Int64 firstUidExternal, Label label)
        {
            this.HighestUidLocal = lastUidLocal;
            this.LowestUidLocal = firstUidLocal;
            this.NextUid = this.HighestUidExternal = lastUidExternal;
            this.LowestUidExternal = firstUidExternal;
            this.Label = label;
            this.Dirty = false;
            this._working = true;
        }
    }
}