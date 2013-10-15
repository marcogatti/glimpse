using Glimpse.DataAccessLayer.Entities;
using Glimpse.Exceptions;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.Models
{
    public class Label
    {
        public LabelEntity Entity { get; private set; }

        public Label(LabelEntity labelEntity)
        {
            this.Entity = labelEntity;
        }

        public void Rename(String oldName, String newName, ISession session)
        {
            this.Entity.Name = this.Entity.Name.Replace(oldName, newName);
            this.SaveOrUpdate(session);
        }
        public void Delete(ISession session)
        {
            this.Entity.Active = false; //Trigger en DB borra links con mails
            this.SaveOrUpdate(session);
        }
        public void SaveOrUpdate(ISession session)
        {
            session.SaveOrUpdate(this.Entity);
        }

        public static IList<LabelEntity> FindByAccount(MailAccountEntity account, ISession session)
        {
            IList<LabelEntity> labels = session.CreateCriteria<LabelEntity>()
                                              .Add(Restrictions.Eq("MailAccountEntity", account))
                                              .Add(Restrictions.Eq("Active", true))
                                              .List<LabelEntity>();
            return labels;
        }
        public static Label FindBySystemName(MailAccount account, String systemName, ISession session)
        {

            LabelEntity labelEntity;

            try
            {
                labelEntity = session.CreateCriteria<LabelEntity>()
                                          .Add(Restrictions.Eq("MailAccountEntity", account.Entity))
                                          .Add(Restrictions.Eq("SystemName", systemName))
                                          .Add(Restrictions.Eq("Active", true))
                                          .UniqueResult<LabelEntity>();
            }
            catch (NHibernate.HibernateException e)
            {
                throw new NotUniqueResultException(e, "Cuenta: " + account.Entity.Address + " ,SystemName Label: " + systemName);
            }

            return new Label(labelEntity);
        }
        public static Label FindByName(MailAccount mailAcccount, String labelName, ISession session)
        {
            LabelEntity labelEntity;

            labelEntity = session.CreateCriteria<LabelEntity>()
                                          .Add(Restrictions.Eq("MailAccountEntity", mailAcccount.Entity))
                                          .Add(Restrictions.Eq("Name", labelName))
                                          .Add(Restrictions.Eq("Active", true))
                                          .UniqueResult<LabelEntity>();

            if (labelEntity == null)
                return null;
            else
                return new Label(labelEntity);
        }
        public static List<LabelEntity> RemoveDuplicates(List<LabelEntity> dupLabels)
        {
            List<LabelEntity> uniqueLabels = new List<LabelEntity>();
            foreach (LabelEntity label in dupLabels)
            {
                if (uniqueLabels.Any(x => x.Name == label.Name || 
                    (x.SystemName == label.SystemName && x.Name == label.Name)))
                    continue;
                else
                    uniqueLabels.Add(label);
            }
            return uniqueLabels;
        }
    }
}