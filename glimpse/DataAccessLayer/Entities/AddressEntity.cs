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

        private AddressEntity(String mailAddress, String name)
        {
            this.MailAddress = mailAddress;
            this.Name = name;
        }

        public AddressEntity() { }


        public static AddressEntity Save(String mailAddress, String name, ISession session)
        {
            AddressEntity newAddress = new AddressEntity(mailAddress, name); 
            AddressEntity returnAddress;

            ITransaction tran = session.BeginTransaction();

            AddressEntity oldAddress = FindByAddress(mailAddress, session);

            if (oldAddress == null)
            {
                session.Save(newAddress);
                returnAddress = newAddress;
            }
            else
            {
                ResetWithOtherAddress(oldAddress, newAddress);
                session.Update(oldAddress);
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

        public static AddressEntity FindByAddress(String address, ISession session)
        {
            AddressEntity foundAddress = session.CreateCriteria<AddressEntity>()
                    .Add(Restrictions.Eq("MailAddress", address))
                    .UniqueResult<AddressEntity>();

            return foundAddress;
        }

        public static void RemoveByAddress(String mailAddress, ISession session)
        {
            if (FindByAddress(mailAddress, session) != null)
            {
                ITransaction tran = session.BeginTransaction();

                session.Delete(FindByAddress(mailAddress, session));

                tran.Commit();
            }
        }
    }
}