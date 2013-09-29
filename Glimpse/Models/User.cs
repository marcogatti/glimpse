using Glimpse.DataAccessLayer;
using Glimpse.DataAccessLayer.Entities;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Glimpse.Models
{
    public class User
    {
        public UserEntity Entity { get; private set; }

        public User(UserEntity entity)
        {
            this.Entity = entity;
        }
        public User(String username, String password)
        {
            this.Entity = new UserEntity();
            this.Entity.Username = username;
            this.Entity.Password = password;
            this.Entity.ShowTutorial = true;
        }

        public IList<MailAccount> GetAccounts(ISession session)
        {
            IList<MailAccount> mailAccounts = new List<MailAccount>();
            IList<MailAccountEntity> mailAccountEntities = session.CreateCriteria<MailAccountEntity>()
                                                                  .Add(Restrictions.Eq("User", this.Entity))
                                                                  .List<MailAccountEntity>();
            foreach (MailAccountEntity entity in mailAccountEntities)
            {
                mailAccounts.Add(new MailAccount(entity));
            }
            return mailAccounts;
        }
        public void SaveOrUpdate(ISession session)
        {
            session.SaveOrUpdate(this.Entity);
        }

        public static bool IsEmail(String phrase)
        {
            return Regex.IsMatch(phrase, @"^[A-Za-z0-9]([\w\.\-]*)@([A-Za-z0-9-]+)((\.(\w){2,3})+)$");
        }
        
        public static User FindByUsername(String username, ISession session)
        {
            UserEntity userEntity = session.CreateCriteria<UserEntity>()
                                            .Add(Restrictions.Eq("Username", username))
                                            .UniqueResult<UserEntity>();
            if (userEntity != null)
            {
                return new User(userEntity);
            }
            return null;
        }

    }
}