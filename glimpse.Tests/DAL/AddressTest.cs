using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Glimpse.DataAccessLayer.Entities;
using NUnit.Framework;
using NHibernate;
using Glimpse.DataAccessLayer;
using Glimpse.Models;
using NHibernate.Criterion;

namespace Glimpse.Tests.DAL
{
    [TestFixture]
    public class AddressTest
    {
        private String anAddress;
        private String aName;
        private ISession aSession;


        [TestFixtureSetUp]
        public void setSomeAddressesAndSession()
        {
            this.anAddress = "testAddress@example.com";
            this.aName = "TestImap";
            this.aSession = NHibernateManager.OpenSession();
        }


        [Test]
        public void CreateANewAddress()
        {
            ITransaction tran = aSession.BeginTransaction();

            AddressEntity entity = new AddressEntity(anAddress, aName);

            aSession.Save(entity);

            tran.Commit();

            Assert.AreEqual(entity.MailAddress, Address.FindByAddress(anAddress, aSession).Entity.MailAddress);
        }

        [TestFixtureTearDown]
        public void CleanUpDBAndEndSession()
        {
            ITransaction tran = aSession.BeginTransaction();

            aSession.Delete(Address.FindByAddress(anAddress, aSession).Entity);

            tran.Commit();

            aSession.Flush();
            aSession.Close();
        }
    }
}
