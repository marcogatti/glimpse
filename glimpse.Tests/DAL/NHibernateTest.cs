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

        private static void BuildSchema(Configuration cfg)
        {
            new SchemaExport(cfg).SetOutputFile("D://dropschema").Drop(true, true);
            new SchemaExport(cfg).SetOutputFile("D://schema").Create(true, true);
        }

        [Test]
        public void CreateConnectionAndReturnNullResult()
        {
            session = NHibernateManager.OpenSession();

            MailEntity myMail = session.CreateCriteria<MailEntity>()
                                .Add(Restrictions.Eq("Id", Int64.MaxValue))
                                .UniqueResult<MailEntity>();

            Assert.IsNull(myMail);
        }

        [Test]
        [Explicit]
        public void CreateSchema()
        {
            var dataAccessService = new NHibernateManager();
            dataAccessService.CreateSessionFactory();
            BuildSchema(dataAccessService.NhibernateConfiguration);
        }
    }
}
