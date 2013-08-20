using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate.Linq;
using NHibernate.Criterion;
using NHibernate;

namespace Glimpse.DataAccessLayer.Entities
{
    public class Address
    {
        public virtual int Id { get; set; }
        public virtual String MailAddress { get; set; }
        public virtual String Name { get; set; }

        private static ISession currentSession = NHibernateManager.DefaultSesion;


        private Address(String mailAddress, String name)
        {
            this.MailAddress = mailAddress;
            this.Name = name;
        }

        public Address() { }


        public static Address Save(String mailAddress, String name)
        {
            Address newAddress = new Address(mailAddress, name);
            Address oldAddress = FindByAddress(mailAddress);
            Address returnAddress;

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

        private static void ResetWithOtherAddress(Address to, Address from)
        {
            to.MailAddress = from.MailAddress;
            to.Name = from.Name;
        }

        public static Address FindByAddress(String address)
        {
            Address foundAddress = currentSession.CreateCriteria<Address>()
                    .Add(Restrictions.Eq("MailAddress", address))
                    .UniqueResult<Address>();

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