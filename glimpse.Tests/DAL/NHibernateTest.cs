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
    public class NHibernateTest
    {
        public ISession session;

        [Test]
        public void CreateConnectionAndReturnNullResult()
        {
            session = NHibernateManager.OpenSession();

            Mail myMail = session.CreateCriteria<Mail>()
                                .Add(Restrictions.Eq("Id", -1))
                                .UniqueResult<Mail>();

            Assert.IsNull(myMail);
        }
    }
}
