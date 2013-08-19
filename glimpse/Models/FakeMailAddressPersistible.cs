using Glimpse.Exceptions;
using Glimpse.MailInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.Models
{
    public class FakeMailAddressPersistible
    {

        private static List<FakeMailAddressPersistible> addressesRegistered = new List<FakeMailAddressPersistible>();

        public String MailAddress { get; set; }
        public String Password { get; set; }


        private FakeMailAddressPersistible(String emailAddress, String password)
        {
            this.MailAddress = emailAddress;
            this.Password = password;
        }


        public static FakeMailAddressPersistible CreateOrUpdate(String emailAddress, String password){
            FakeMailAddressPersistible one = new FakeMailAddressPersistible(emailAddress, password);
            addressesRegistered.Add(one);
            return one;
        }

        public static FakeMailAddressPersistible FindByAddress(String emailAddress)
        {
            FakeMailAddressPersistible address = addressesRegistered.Find(m => m.MailAddress == emailAddress);

            if (address != null)
            {
                return address;
            }
            else
            {
                throw new GlimpseException("mail address not found");
            }
        }

        public MailAccount LoginExternal()
        {
            return new MailAccount(this.MailAddress, this.Password);
        }
    }
}