using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate.Linq;
using NHibernate.Criterion;
using NHibernate;

namespace Glimpse.DataAccessLayer.Entities
{
    public class AddressEntity
    {
        public virtual Int64 Id { get; set; }
        public virtual String MailAddress { get; set; }
        public virtual String Name { get; set; }

        private static ISession currentSession = NHibernateManager.DefaultSesion;


        private AddressEntity(String mailAddress, String name)
        {
            this.MailAddress = mailAddress;
            this.Name = name;
        }

        public AddressEntity() { }


        public static AddressEntity Save(String mailAddress, String name)
        {
            AddressEntity newAddress = new AddressEntity(mailAddress, name);
            AddressEntity oldAddress = FindByAddress(mailAddress);
            AddressEntity returnAddress;

            ITransaction tran = currentSession.BeginTransaction();

            if (oldAddress == null)
            {
                currentSession.Save(newAddress);
                returnAddress = newAddress;
            }
            else
            {
                ResetWithOtherAddress(oldAddress, newAddress);
                currentSession.Update(oldAddress);
                returnAddress = oldAddress;
            }

            tran.Commit();

            return returnAddress;
        }

        private static void ResetWithOtherAddress(AddressEntity to, AddressEntity from)
        {
            to.MailAddress = from.MailAddress;
            to.Name = from.Name;
        }

        public static AddressEntity FindByAddress(String address)
        {
            AddressEntity foundAddress = currentSession.CreateCriteria<AddressEntity>()
                    .Add(Restrictions.Eq("MailAddress", address))
                    .UniqueResult<AddressEntity>();

            return foundAddress;
        }

        public static void RemoveByAddress(String mailAddress)
        {
            if (FindByAddress(mailAddress) != null)
            {
                ITransaction tran = currentSession.BeginTransaction();

                currentSession.Delete(FindByAddress(mailAddress));

                tran.Commit();
            }
        }
    }
}