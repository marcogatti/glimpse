using Glimpse.DataAccessLayer.Entities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glimpse.Tests.DAL
{
    [TestFixture]
    public class MailTest
    {
        [Test]
        public void FetchMailsFromInboxWithNoFilters()
        {
            Assert.NotNull(Mail.FindFromInbox());
        }
    }

}
