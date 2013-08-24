using Glimpse.DataAccessLayer.Entities;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.Models
{
    public class Address
    {
        private AddressEntity _entity;
        public AddressEntity Entity
        {
            get
            {
                return _entity;
            }
        }

        public Address(AddressEntity entity)
        {
            this._entity = entity;
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
                oldAddress.CopyEntityDataFrom(this.Entity);
                persistAddress = oldAddress.Entity;
            }

            currentSession.SaveOrUpdate(persistAddress);

            tran.Commit();
        }

        private void CopyEntityDataFrom(AddressEntity from)
        {
            this.Entity.MailAddress = from.MailAddress;
            this.Entity.Name = from.Name;
        }

        public static Address FindByAddress(String address, ISession session)
        {
            return new Address(session.CreateCriteria<AddressEntity>()
                    .Add(Restrictions.Eq("MailAddress", address))
                    .UniqueResult<AddressEntity>());
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