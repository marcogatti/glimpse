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

        [TestFixtureSetUp]
        public void Open_Nhibernate_Session()
        {
            session = NHibernateManager.OpenSession();
        }

        [Test]
        public void Search_For_Mail_With_Max_Int_Id_Returns_Null_Result()
        {
            MailEntity myMail = session.CreateCriteria<MailEntity>()
                                    .Add(Restrictions.Eq("Id", Int64.MaxValue))
                                    .UniqueResult<MailEntity>();
            Assert.IsNull(myMail);
        }

        [Test]
        public void Search_For_Mail_With_Id_1_And_Return_Unique_Result()
        {
            MailEntity myMail = session.CreateCriteria<MailEntity>()
                                    .Add(Restrictions.Eq("Id", Convert.ToInt64(1)))
                                    .UniqueResult<MailEntity>();
            Assert.NotNull(myMail);
        }
    }
}
