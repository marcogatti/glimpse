using Glimpse.DataAccessLayer;
using Glimpse.DataAccessLayer.Entities;
using Glimpse.ErrorLogging;
using Glimpse.Exceptions.MailInterfacesExceptions;
using Glimpse.Models;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Glimpse.MailInterfaces
{
    public class MailsTasksHandler
    {
        private static Dictionary<string, MailsTask> TasksList = new Dictionary<string, MailsTask>();
        private static Mutex TasksListLock = new Mutex(false);

        private static int MAILS_AMMOUNT_PER_ROUND = 4;

        public static void StartSynchronization(String mailAddress)
        {
            MailAccount mailAccountAll;
            MailsTask newAllTask;
            MailsTask newTaskTrash;
            MailsTask newSpamTask;
            String mailAddressAll = mailAddress + "/ALL";
            String mailAddressTrash = mailAddress + "/TRASH";
            String mailAddressSpam = mailAddress + "/SPAM";

            if (MailsTasksHandler.IsAnyWorking(new String[]  { mailAddressAll, mailAddressTrash, mailAddressSpam} ))
                return; //alguna tarea se encuentra trabajando

            using (ISession session = NHibernateManager.OpenSession())
            {
                mailAccountAll = MailAccount.FindByAddress(mailAddress, session);
                try
                {
                    mailAccountAll.ConnectFull();
                }
                catch (NullReferenceException exc)
                {
                    Log.LogException(exc, "Direccion inexistente en la base de datos: " + mailAccountAll + ".");
                    return;
                }
                catch (InvalidAuthenticationException exc)
                {
                    Log.LogException(exc, "No se pudo conectar a imap con la direccion: " + mailAddress + ", ha cambiado el password.");
                    return;
                }
                catch (SocketException exc)
                {
                    Log.LogException(exc, "No se pudo conectar a imap con la direccion: " + mailAddress + ".");
                    return;
                }
                Label allLabel = Label.FindBySystemName(mailAccountAll, "All", session);
                Label trashLabel = Label.FindBySystemName(mailAccountAll, "Trash", session);
                Label spamLabel = Label.FindBySystemName(mailAccountAll, "Junk", session);

                newAllTask = new MailsTask(mailAccountAll.GetUIDLocal(session, allLabel.Entity.SystemName, true), //lastUidLocal
                                             mailAccountAll.GetUIDLocal(session, allLabel.Entity.SystemName, false), //firstUidLocal
                                             mailAccountAll.GetUIDExternalFrom(allLabel.Entity.Name, true), //lastUidExternal
                                             mailAccountAll.GetUIDExternalFrom(allLabel.Entity.Name, false), //firstUidExternal
                                             allLabel,
                                             mailAccountAll);
                newTaskTrash = new MailsTask(mailAccountAll.GetUIDLocal(session, trashLabel.Entity.SystemName, true), //lastUidLocal
                                             mailAccountAll.GetUIDLocal(session, trashLabel.Entity.SystemName, false), //firstUidLocal
                                             mailAccountAll.GetUIDExternalFrom(trashLabel.Entity.Name, true), //lastUidExternal
                                             mailAccountAll.GetUIDExternalFrom(trashLabel.Entity.Name, false), //firstUidExternal
                                             trashLabel,
                                             null);
                newSpamTask = new MailsTask(mailAccountAll.GetUIDLocal(session, spamLabel.Entity.SystemName, true), //lastUidLocal
                                             mailAccountAll.GetUIDLocal(session, spamLabel.Entity.SystemName, false), //firstUidLocal
                                             mailAccountAll.GetUIDExternalFrom(spamLabel.Entity.Name, true), //lastUidExternal
                                             mailAccountAll.GetUIDExternalFrom(spamLabel.Entity.Name, false), //firstUidExternal
                                             spamLabel,
                                             null);
                session.Close();
            }
            if (!newAllTask.HasFinished) //puede ser que no se necesite sincronizar dependiendo de los numeros
            {
                MailsTasksHandler.LockTasksList();
                MailsTasksHandler.TasksList[mailAddressAll] = newAllTask;
                MailsTasksHandler.UnlockTasksList();
                MailsTasksHandler.StartMailsTask(newAllTask);
            }
            if (!newTaskTrash.HasFinished)
            {
                MailAccount mailAccountTrash = mailAccountAll.Clone(); //crear otra conexion a IMAP
                newTaskTrash.SetMailAccount(mailAccountTrash);
                MailsTasksHandler.LockTasksList();
                MailsTasksHandler.TasksList[mailAddressTrash] = newTaskTrash;
                MailsTasksHandler.UnlockTasksList();
                MailsTasksHandler.StartMailsTask(newTaskTrash);
            }
            if (!newSpamTask.HasFinished)
            {
                MailAccount mailAccountSpam = mailAccountAll.Clone(); //crear otra conexion a IMAP
                newSpamTask.SetMailAccount(mailAccountSpam);
                MailsTasksHandler.LockTasksList();
                MailsTasksHandler.TasksList[mailAddressSpam] = newSpamTask;
                MailsTasksHandler.UnlockTasksList();
                MailsTasksHandler.StartMailsTask(newSpamTask);
            }
        }
        public static void SynchronizeTrash(String mailAddress)
        {
            MailAccount mailAccountTrash;
            MailsTask newTaskTrash;
            String mailAddressTrash = mailAddress + "/TRASH";

            if (MailsTasksHandler.IsWorking(mailAddressTrash))
                return;

            using (ISession session = NHibernateManager.OpenSession())
            {
                mailAccountTrash = MailAccount.FindByAddress(mailAddress, session);
                try
                {
                    mailAccountTrash.ConnectFull();
                }
                catch (NullReferenceException exc)
                {
                    Log.LogException(exc, "Direccion inexistente en la base de datos: " + mailAccountTrash + ".");
                    return;
                }
                catch (InvalidAuthenticationException exc)
                {
                    Log.LogException(exc, "No se pudo conectar a imap con la direccion: " + mailAddress + ", ha cambiado el password.");
                    return;
                }
                catch (SocketException exc)
                {
                    Log.LogException(exc, "No se pudo conectar a imap con la direccion: " + mailAddress + ".");
                    return;
                }
                Label trashLabel = Label.FindBySystemName(mailAccountTrash, "Trash", session);
                newTaskTrash = new MailsTask(mailAccountTrash.GetUIDLocal(session, trashLabel.Entity.SystemName, true),
                                             mailAccountTrash.GetUIDLocal(session, trashLabel.Entity.SystemName, false),
                                             mailAccountTrash.GetUIDExternalFrom(trashLabel.Entity.Name, true),
                                             mailAccountTrash.GetUIDExternalFrom(trashLabel.Entity.Name, false),
                                             trashLabel,
                                             mailAccountTrash);
                session.Close();
            }
            if (!newTaskTrash.HasFinished)
            {
                MailsTasksHandler.LockTasksList();
                MailsTasksHandler.TasksList[mailAddressTrash] = newTaskTrash;
                MailsTasksHandler.UnlockTasksList();
                MailsTasksHandler.StartMailsTask(newTaskTrash);
            }
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
                    MailsTasksHandler.SynchronizeForward(task);
                }
                else if (!task.HasFinishedBackward)
                {
                    MailsTasksHandler.SynchronizeBackward(task);
                }

                if (task.HasFinished)
                {
                    MailsTasksHandler.EndSynchronization(task);
                    return;
                }

                MailsTasksHandler.StartMailsTask(task);
            }
            catch (Exception exc)
            {
                MailsTasksHandler.EndSynchronization(task);
                Log.LogException(exc, "Error sincronizando cuenta, parametros: mailAccount:" +
                                             task.MailAccount.Entity.Address + " lowestUidLocal:" +
                                             task.LowestUidLocal.ToString() + " lowestUidExternal:"+
                                             task.LowestUidExternal.ToString() + " highestUidLocal:" +
                                             task.HighestUidLocal.ToString() + " highestUidExternal:" +
                                             task.HighestUidExternal.ToString() + " hasFinished:" +
                                             task.HasFinished.ToString() + " hasFinishedForward:" +
                                             task.HasFinishedForward.ToString() + " nextUidForward:" +
                                             task.NextUidForward.ToString() + " hasFinishedBackward:" +
                                             task.HasFinishedBackward.ToString() + " nextUidBackward:" +
                                             task.NextUidBackward.ToString() + " labelName:" + 
                                             task.Label.Entity.Name);
                throw exc;
            }
        }
        private static void SynchronizeBackward(MailsTask task)
        {
            Int64 toUid, fromUid;

            toUid = task.NextUidBackward;
            fromUid = MailsTasksHandler.GetFromUid(toUid, task.LowestUidExternal - 1); // En el backward queremos que traiga el LowestUidExternal

            task.MailAccount.FetchAndSaveMails(task.Label, fromUid, toUid);

            task.Dirty = true;

            task.NextUidBackward = MailsTasksHandler.GetFollowingNextUid(fromUid);
        }
        private static void SynchronizeForward(MailsTask task)
        {
            Int64 toUid, fromUid;

            toUid = task.NextUidForward;
            fromUid = MailsTasksHandler.GetFromUid(toUid, task.HighestUidLocal); // En el forward no queremos el HighestUidLocal porque ya lo tenemos

            task.MailAccount.FetchAndSaveMails(task.Label, fromUid, toUid);

            task.Dirty = true;

            task.NextUidForward = MailsTasksHandler.GetFollowingNextUid(fromUid);
        }
        private static void EndSynchronization(MailsTask task)
        {
            task.Working = false;
            task.MailAccount.Disconnect();
        }

        private static Int64 GetFollowingNextUid(Int64 fromUid)
        {
            if (fromUid - 1 <= 0)
                return -1;  // Task finished
            else
                return fromUid - 1; //muevo el puntero una posicion atras de que recien me traje
        }
        private static Int64 GetFromUid(Int64 toUid, Int64 UidLimit)
        {
            Int64 fromUid;

            if (toUid - MAILS_AMMOUNT_PER_ROUND > UidLimit)
                fromUid = toUid - MAILS_AMMOUNT_PER_ROUND;
            else
                fromUid = UidLimit + 1;
            return fromUid;
        }

        private static Boolean IsWorking(String taskName)
        {
            MailsTasksHandler.LockTasksList();
            if (MailsTasksHandler.TasksList.ContainsKey(taskName) && MailsTasksHandler.TasksList[taskName].IsWorking)
            {
                MailsTasksHandler.UnlockTasksList();
                return true;
            }
            MailsTasksHandler.UnlockTasksList();
            return false;
        }
        private static Boolean IsAnyWorking(String[] tasksNames)
        {
            MailsTasksHandler.LockTasksList();
            Boolean anyIsWorking = false;
            foreach (String taskName in tasksNames)
            {
                if (MailsTasksHandler.TasksList.ContainsKey(taskName) && MailsTasksHandler.TasksList[taskName].IsWorking)
                {
                    anyIsWorking = true;
                    break;
                }
            }
            MailsTasksHandler.UnlockTasksList();
            return anyIsWorking;
        }
        private static void UnlockTasksList()
        {
            MailsTasksHandler.TasksListLock.ReleaseMutex();
        }
        private static void LockTasksList()
        {
            MailsTasksHandler.TasksListLock.WaitOne();
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
        public bool Dirty { get; set; }

        public MailsTask(Int64 lastUidLocal, Int64 firstUidLocal, Int64 lastUidExternal, Int64 firstUidExternal, Label label, MailAccount mailAccount)
        {
            this.HighestUidLocal = lastUidLocal;
            this.LowestUidLocal = firstUidLocal;
            this.NextUidBackward = this.LowestUidLocal - 1; //no queremos descargar el mail que ya tenemos!
            this.NextUidForward = this.HighestUidExternal = lastUidExternal;
            this.LowestUidExternal = firstUidExternal;
            this.Label = label;
            this.Dirty = false;
            this.Working = true;
            this.MailAccount = mailAccount;
        }

        public void SetMailAccount(MailAccount mailAccount)
        {
            this.MailAccount = mailAccount;
        }
        public bool IsWorking
        {
            get
            {
                return this.Working;
            }
        }
        public bool HasFinishedBackward
        {
            get
            {
                return (this.LowestUidExternal > this.NextUidBackward) // Si el puntero al siguiente queda dentras de lo que existe en IMAP.
                    || (this.LowestUidExternal >= this.LowestUidLocal); // Si tengo lo mismo o mas en la base que en IMAP 
            }
        }
        public bool HasFinishedForward
        {
            get
            {
                return (this.HighestUidLocal >= this.NextUidForward) // Si el puntero al siguiente esta detras del que tenemos en la base
                    || (this.HighestUidExternal <= this.HighestUidLocal) // Si tengo lo mismo o mas en la base que en IMAP
                    || (this.LowestUidExternal > this.NextUidForward); // Si el puntero al siguiente queda por debajo lo que existe en IMAP
            }
        }
        public bool HasFinished
        {
            get
            {
                return this.HasFinishedForward && this.HasFinishedBackward;
            }
        }
    }
}