﻿using Glimpse.DataAccessLayer.Entities;
using Glimpse.Exceptions.ModelsExceptions;
using Glimpse.Helpers;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Glimpse.Exceptions;

namespace Glimpse.Models
{
    public class User
    {
        public UserEntity Entity { get; private set; }
        public IList<MailAccount> mailAccounts { get; set; }

        public User(UserEntity entity)
        {
            this.Entity = entity;
            this.mailAccounts = new List<MailAccount>();
        }
        public User(String username, String password)
        {
            this.Entity = new UserEntity();
            this.Entity.Username = username;
            this.Entity.Password = password;
            this.mailAccounts = new List<MailAccount>();
        }
        public User(String username, String password, String firstName, String lastName) : this(username, password)
        {
            this.Entity.Firstname = firstName;
            this.Entity.Lastname = lastName;
        }

        public void AddAccount(MailAccount mailAccount)
        {
            if (!this.mailAccounts.Any(x => x.Entity.Address == mailAccount.Entity.Address)) //si no lo tiene
                this.mailAccounts.Add(mailAccount);
            else //si ya lo tiene que lo actualice
            {
                MailAccount newMailaccount = this.mailAccounts.Single(x => x.Entity.Address == mailAccount.Entity.Address);
                newMailaccount = mailAccount;
            }
        }
        public void UpdateAccounts(ISession session)
        {
            IList<MailAccount> databaseMailAccounts = new List<MailAccount>();
            IList<MailAccountEntity> mailAccountEntities = session.CreateCriteria<MailAccountEntity>()
                                                                  .Add(Restrictions.Eq("User", this.Entity))
                                                                  .Add(Restrictions.Eq("Active", true))
                                                                  .List<MailAccountEntity>();
            foreach (MailAccountEntity entity in mailAccountEntities)
                databaseMailAccounts.Add(new MailAccount(entity));
            this.mailAccounts = databaseMailAccounts;
        }
        public void UpdateLabels(ISession session)
        {
            foreach (MailAccount mailAccount in this.mailAccounts)
                mailAccount.UpdateLabels(session);
        }
        public void SetOldestMailDates()
        {
            foreach (MailAccount mailAccount in this.mailAccounts)
                mailAccount.SetOldestMailDate();
        }
        public void ConnectLight()
        {
            foreach (MailAccount mailAccount in this.mailAccounts)
                mailAccount.ConnectLight();
        }
        public IList<MailAccount> GetAccounts()
        {
            return this.mailAccounts;
        }
        public void SaveOrUpdate(ISession session)
        {
            session.SaveOrUpdate(this.Entity);
        }
        public void ChangePassword(String oldPassword, String newPassword, ISession session)
        {
            if (CryptoHelper.PasswordsMatch(this.Entity.Password, oldPassword))
            {
                this.Entity.Password = newPassword;
                this.SaveOrUpdate(session);
            }
            else
            {
                throw new GlimpseException("La contraseña de usuario ingresada es incorrecta.");
            }
        }
        public void Disconnect()
        {
            foreach (MailAccount mailAccount in this.mailAccounts)
                mailAccount.Disconnect();
        }

        public static bool IsEmail(String phrase)
        {
            return Regex.IsMatch(phrase, @"^[A-Za-z0-9]([\w\.\-]*)@([A-Za-z0-9-]+)((\.(\w){2,3})+)$");
        }
        public static bool IsGlimpseUser(String phrase)
        {
            return Regex.IsMatch(phrase, @"^[A-Za-z]{1}[A-Za-z0-9]{3,15}$");
        }
        public static bool IsPassword(String phrase)
        {
            if (phrase == null)
                return false;

            return Regex.IsMatch(phrase, @"^(?!.{20})[A-Za-z0-9\!#\$%&'\*\.\+\-/=\?\^`\{|\}~_]{6,20}");
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