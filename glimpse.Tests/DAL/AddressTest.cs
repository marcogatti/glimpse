using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Glimpse.DataAccessLayer.Entities;
using NUnit.Framework;
using NHibernate;
using Glimpse.DataAccessLayer;

namespace Glimpse.Tests.DAL
{
   [TestFixture]
    public class AddressTest
    {
        private String anAddress;
        private String aName;
        private ISession aSession = NHibernateManager.OpenSession();


        [SetUp]
        public void setSomeAddresses() {
            this.anAddress = "testAddress@example.com";
            this.aName = "TestImap";
        }


        [Test]
        public void CreateANewAddress()
        {
            AddressEntity.RemoveByAddress(anAddress, aSession);

            this.CreateOrUpdateAnAddress();
        }

        [Test]
        public void CreateOrUpdateAnAddress()
        {
            AddressEntity createdAddress = AddressEntity.Save(anAddress, aName, aSession);
            AddressEntity foundAddress = AddressEntity.FindByAddress(createdAddress.MailAddress, aSession);

            Assert.AreEqual(createdAddress.MailAddress, foundAddress.MailAddress);
            Assert.AreEqual(createdAddress.Name, foundAddress.Name);
            Assert.AreEqual(createdAddress.Id, foundAddress.Id);
        }
    }
}
