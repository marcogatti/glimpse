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

        private static int MAILS_AMMOUNT_PER_PASS = 4;


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

            if (taskIsWorking)
                return;

            ISession session = NHibernateManager.OpenSession();

            Label label = Label.FindBySystemName(mailAccount, "INBOX", session);
            Int64 lastUidExternal = mailAccount.getLastUIDExternalFrom("INBOX"); //TODO: Deshardcodear
            Int64 firstUidExternal = mailAccount.getFirstUIDExternalFrom("INBOX"); //TODO: Deshardcodear
            Int64 lastUidLocal = mailAccount.GetLastUIDLocalFromALL(session);
            Int64 firstUidLocal = mailAccount.GetFirstUIDLocalFromALL(session);

            session.Close();

            MailsTask task = new MailsTask(lastUidLocal, firstUidLocal, lastUidExternal, firstUidExternal, label, mailAccount);

            if (task.HasFinished)
            {
                return;
            }

            LockTasksList();
            tasksList[mailAccount.Entity.Address] = task;
            UnlockTasksList();

            StartMailsTask(task);
        }

        private static void StartMailsTask(MailsTask task)
        {
            Task.Factory.StartNew(() => SynchronizeAccount(task));
        }

        private static void SynchronizeAccount(MailsTask task)
        {
            try
            {
                if (!task.HasFinishedForward)
                {
                    SynchronizeForward(task);
                }
                else if (!task.HasFinishedBackward)
                {
                    SynchronizeBackward(task);
                }

                if (task.HasFinished)
                {
                    EndSynchronization(task);
                    return;
                }

                StartMailsTask(task);

            }
            catch (Exception exc)
            {
                EndSynchronization(task);

                Log logger = new Log(new LogEntity(003, "Error generico SynchronizeAccount. Parametros: mailAddress(" + task.MailAccount.Entity.Address + ").", exc.StackTrace));
                logger.Save();
            }
        }

        private static void SynchronizeBackward(MailsTask task)
        {
            Int64 toUid, fromUid;

            toUid = task.NextUidBackward;
            fromUid = GetFromUid(toUid, task.LowestUidExternal);

            task.MailAccount.FetchAndSaveMails(task.Label, fromUid, toUid);

            task.Dirty = true;

            task.NextUidBackward = GetFollowingNextUid(fromUid);
        }

        private static void SynchronizeForward(MailsTask task)
        {
            Int64 toUid, fromUid;

            toUid = task.NextUidForward;
            fromUid = GetFromUid(toUid, task.HighestUidLocal);

            task.MailAccount.FetchAndSaveMails(task.Label, fromUid, toUid);

            task.Dirty = true;

            task.NextUidForward = GetFollowingNextUid(fromUid);
        }

        private static void EndSynchronization(MailsTask task)
        {
            task.Working = false;
        }

        private static Int64 GetFollowingNextUid(Int64 fromUid)
        {
            if (fromUid - 1 <= 0)
                return -1;  // Task finished
            else
                return fromUid - 1;
        }

        private static Int64 GetFromUid(Int64 toUid, Int64 UidLimit)
        {
            Int64 fromUid;

            if (toUid - MAILS_AMMOUNT_PER_PASS > UidLimit)
                fromUid = toUid - MAILS_AMMOUNT_PER_PASS;
            else
                fromUid = UidLimit + 1;
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
        internal bool Working { get; set; }
        internal Int64 HighestUidExternal { get; set; }
        internal Int64 LowestUidExternal { get; set; }
        internal Int64 HighestUidLocal { get; set; }
        internal Int64 LowestUidLocal { get; set; }
        internal Int64 NextUidForward { get; set; }
        internal Int64 NextUidBackward { get; set; }
        internal Label Label { get; set; }
        internal MailAccount MailAccount { get; set; }

        public bool IsWorking
        {
            get
            {
                return this.Working;
            }
        }
        public bool Dirty { get; set; }

        public bool HasFinishedBackward
        {
            get
            {
                return (this.LowestUidExternal > this.NextUidBackward) || (this.LowestUidExternal == this.LowestUidLocal);
            }
        }

        public bool HasFinishedForward
        {
            get
            {
                return (this.HighestUidLocal > this.NextUidForward) || (this.HighestUidExternal == this.HighestUidLocal);
            }
        }

        public bool HasFinished
        {
            get
            {
                return this.HasFinishedForward && this.HasFinishedBackward;
            }
        }



        public MailsTask(Int64 lastUidLocal, Int64 firstUidLocal, Int64 lastUidExternal, Int64 firstUidExternal, Label label, MailAccount mailAccount)
        {
            this.HighestUidLocal = lastUidLocal;
            this.NextUidBackward = this.LowestUidLocal = firstUidLocal;
            this.NextUidForward = this.HighestUidExternal = lastUidExternal;
            this.LowestUidExternal = firstUidExternal;
            this.Label = label;
            this.Dirty = false;
            this.Working = true;
            this.MailAccount = mailAccount;
        }
    }
}