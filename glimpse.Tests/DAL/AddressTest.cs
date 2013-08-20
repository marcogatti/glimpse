using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Glimpse.DataAccessLayer.Entities;
using Glimpse.DataAccessLayer.Mappings;
using NUnit.Framework;

namespace Glimpse.Tests.DAL
{
   [TestFixture]
    public class AddressTest
    {
        private String anAddress;
        private String aName;


        [SetUp]
        public void setSomeAddresses() {
            this.anAddress = "testAddress@example.com";
            this.aName = "TestImap";
        }


        [Test]
        public void CreateANewAddress()
        {
            Address.RemoveByAddress(anAddress);

            this.CreateOrUpdateAnAddress();
        }

        [Test]
        public void CreateOrUpdateAnAddress()
        {
            Address createdAddress = Address.Save(anAddress, aName);
            Address foundAddress = Address.FindByAddress(createdAddress.MailAddress);

            Assert.AreEqual(createdAddress.MailAddress, foundAddress.MailAddress);
            Assert.AreEqual(createdAddress.Name, foundAddress.Name);
            Assert.AreEqual(createdAddress.Id, foundAddress.Id);
        }
    }
}
