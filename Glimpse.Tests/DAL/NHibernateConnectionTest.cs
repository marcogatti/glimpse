using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using NUnit.Framework;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Criterion;
using Glimpse;
using Glimpse.DataAccessLayer;
using Glimpse.DataAccessLayer.Entities;
using Glimpse.DataAccessLayer.Mappings;

namespace Glimpse.Tests.DAL
{
    [TestFixture]
    public class NHibernateConnectionTest
    {
        public ISession session;

        [Test]
        public void CreateConnectionAndReturnNullResult()
        {
            session = NHibernateManager.OpenSession();

            Address myAddress = session.CreateCriteria<Address>()
                                .Add(Restrictions.Eq("Id", -1))
                                .UniqueResult<Address>();

            Assert.IsNull(myAddress);
        }
    }
}
