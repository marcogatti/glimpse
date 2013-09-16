using Glimpse.DataAccessLayer.Entities;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ActiveUp.Net.Mail;

namespace Glimpse.Models
{
    public class Address
    {
        public AddressEntity Entity { get; private set; }

        public Address(AddressEntity entity)
        {
            this.Entity = entity;
        }

        public static Address FindByAddress(String address, ISession session)
        {
            var entity = session.CreateCriteria<AddressEntity>()
                    .Add(Restrictions.Eq("MailAddress", address))
                    .UniqueResult<AddressEntity>();

            Address foundAddress = new Address(entity);

            return foundAddress;
        }

        public static void RemoveByAddress(String mailAddress, ISession session)
        {
            Address foundAddress = FindByAddress(mailAddress, session);

            if (foundAddress != null)
            {
                ITransaction tran = session.BeginTransaction();

                session.Delete(foundAddress);

                tran.Commit();
            }
        }

        public static AddressCollection ParseAddresses(String toAddresses)
        {
            ActiveUp.Net.Mail.AddressCollection addresses = new AddressCollection();
            String[] recipients = new String[toAddresses.Split(new String[] { "," }, StringSplitOptions.RemoveEmptyEntries).Length];

            toAddresses = System.Text.RegularExpressions.Regex.Replace(toAddresses, @"\s+", ","); //reemplaza todos los espacios por ""
            recipients = toAddresses.Split(new String[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            foreach (String recipient in recipients)
            {
                addresses.Add(new ActiveUp.Net.Mail.Address(recipient));
            }

            return addresses;
        }

        public void Save(ISession currentSession)
        {
            AddressEntity persistAddress;

            ITransaction tran = currentSession.BeginTransaction();

            Address oldAddress = FindByAddress(this.Entity.MailAddress, currentSession);

            if (oldAddress.Entity == null)
            {
                persistAddress = this.Entity;
            }
            else
            {
                oldAddress.Clone(this.Entity);
                persistAddress = oldAddress.Entity;
            }

            currentSession.SaveOrUpdate(persistAddress);

            tran.Commit();
        }        

        private void Clone(AddressEntity from)
        {
            this.Entity.MailAddress = from.MailAddress;
            this.Entity.Name = from.Name;
        }
    }
}