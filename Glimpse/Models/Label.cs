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
        private static String DefaultColor = "#91BCD5";
        private static IList<String> LabelColors =
             new String[] 
                {
                    "#FFA500",
                    "#0059FF",
                    "#FF8040",
                    "#40BFFF",
                    "#40FF80",
                    "#FF40BF",
                    "#7F40FF",
                    "#BFFF40",
                    "#FF3300",
                    "#00CC00",
                    "#6666FF",
                    "#E7E01F",
                };

        public Label(LabelEntity labelEntity)
        {
            this.Entity = labelEntity;
        }

        public void Rename(String oldName, String newName, ISession session)
        {
            if (this.Entity.Name == oldName)
                this.Entity.Name = newName;
            else
            {
                List<String> labelHierarchy = oldName.Split(new String[] { "/" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                String newHierarchy = "";
                foreach (String label in labelHierarchy)
                    if (label == oldName)
                        newHierarchy += newName + "/";
                    else
                        newHierarchy += label + "/";
                this.Entity.Name = newHierarchy.Trim('/');
            }
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

        public static void ColorInitialLabels(IList<LabelEntity> labelsToColor)
        {
            Int16 index = 0;
            for (; index < labelsToColor.Count && index < Label.LabelColors.Count; index++)
                labelsToColor[index].Color = Label.LabelColors[index];

            //si me quedo sin colores
            if (labelsToColor.Count > LabelColors.Count)
                for (; index < labelsToColor.Count; index++)
                    labelsToColor[index].Color = Label.DefaultColor;
        }
        public static void ColorLabel(LabelEntity labelToColor, MailAccount labelAccount, User currentUser, ISession session)
        {
            if (currentUser != null)
            {
                IList<MailAccount> currentMailAccounts = currentUser.GetAccounts();
                List<LabelEntity> availableLabels = new List<LabelEntity>();

                foreach (MailAccount mailAccount in currentMailAccounts)
                    availableLabels.AddRange(Label.FindByAccount(mailAccount.Entity, session));

                //si alguna de las cuentas ya tiene un label con el mismo nombre
                if (availableLabels.Any(x => x.SystemName == null && x.Name == labelToColor.Name))
                {
                    labelToColor.Color = availableLabels.Where(x => x.SystemName == null && x.Name == labelToColor.Name).ToList()[0].Color;
                    return;
                }
            }
            labelToColor.Color = Label.GetNextColor(labelAccount.Entity, session);
        }
        public static String GetNextColor(MailAccountEntity labelAccount, ISession session)
        {
            IList<LabelEntity> accountLabels = Label.FindByAccount(labelAccount, session);
            for (Int16 currentColor = 0; currentColor < Label.LabelColors.Count; currentColor++)
                if (!accountLabels.Any(x => x.Color == Label.LabelColors[currentColor]))
                    return Label.LabelColors[currentColor];
            return Label.DefaultColor;
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
                return new Label(labelEntity);
            }
            catch (NHibernate.HibernateException e)
            {
                throw new NotUniqueResultException(e, "Cuenta: " + account.Entity.Address + " ,SystemName Label: " + systemName);
            }
        }
        public static Label FindByName(MailAccount mailAcccount, String labelName, ISession session)
        {
            LabelEntity labelEntity;

            labelEntity = session.CreateCriteria<LabelEntity>()
                                          .Add(Restrictions.Eq("MailAccountEntity", mailAcccount.Entity))
                                          .Add(Restrictions.Eq("Name", labelName))
                                          .Add(Restrictions.Eq("Active", true))
                                          .UniqueResult<LabelEntity>();

            if (labelEntity == null) return null;

            return new Label(labelEntity);
        }
        public static List<LabelEntity> RemoveDuplicates(List<LabelEntity> dupLabels)
        {
            List<LabelEntity> uniqueLabels = new List<LabelEntity>();
            foreach (LabelEntity label in dupLabels)
                if (uniqueLabels.Any(x => (x.Name == label.Name && label.SystemName == null) ||
                    (x.SystemName == label.SystemName && x.Name == label.Name)))
                    continue;
                else
                    uniqueLabels.Add(label);
            return uniqueLabels;
        }
    }
}