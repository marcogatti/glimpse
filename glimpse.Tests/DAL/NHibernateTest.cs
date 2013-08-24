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
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;

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

            MailEntity myMail = session.CreateCriteria<MailEntity>()
                                .Add(Restrictions.Eq("Id", Int64.MaxValue))
                                .UniqueResult<MailEntity>();

            Assert.IsNull(myMail);
        }
    }
}
