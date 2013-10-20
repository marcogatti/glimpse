using ActiveUp.Net.Mail;
using Glimpse.DataAccessLayer.Entities;
using NHibernate;
using NHibernate.Criterion;
using System;

namespace Glimpse.Models
{
    public class Address
    {
        public AddressEntity Entity { get; private set; }

        public Address(AddressEntity entity)
        {
            this.Entity = entity;
        }

        public void SaveOrUpdate(ISession session)
        {
            Address databaseAddress = FindByAddress(this.Entity.MailAddress, session);

            if (databaseAddress.Entity != null)
            {
                databaseAddress.Entity.Name = this.Entity.Name;
                this.Entity = databaseAddress.Entity;
            }

            session.SaveOrUpdate(this.Entity);
        }

        public static void RemoveByAddress(String mailAddress, ISession session)
        {
            Address foundAddress = FindByAddress(mailAddress, session);

            if (foundAddress != null)
                session.Delete(foundAddress);
        }
        public static Address FindByAddress(String address, ISession session)
        {
            var entity = session.CreateCriteria<AddressEntity>()
                    .Add(Restrictions.Eq("MailAddress", address))
                    .UniqueResult<AddressEntity>();

            Address foundAddress = new Address(entity);

            return foundAddress;
        }
        public static AddressCollection ParseAddresses(String toAddresses)
        {
            /*test.imap.performance@gmail.com, "Simpson Marge" <test.imap.performance@gmail.com>,
             * "Simpson Bart" <ezequiel.lopez.2009@hotmail.com>, , "Simpson Bart" <ezequiel.lopez.2009@hotmail.com>,
             * "Simpson Maggie" <ezequiel.lopez.2010@hotmail.com>, */
            String recipientName, line;
            String[] inlineRecipients;
            ActiveUp.Net.Mail.AddressCollection addresses = new AddressCollection();

            toAddresses = toAddresses.Replace("<", String.Empty);
            toAddresses = toAddresses.Replace(">", String.Empty);
            String[] recipients = toAddresses.Split(new String[] { ",", ";" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (String recipientLine in recipients)
            {
                line = recipientLine;
                if (recipientLine.Contains("\""))
                {
                    recipientName = recipientLine.Substring(recipientLine.IndexOf("\"") + 1, recipientLine.LastIndexOf("\"") - recipientLine.IndexOf("\"") - 1);
                    line = recipientLine.Remove(0, recipientLine.LastIndexOf("\"") + 1);
                }
                else
                {
                    recipientName = "";
                }
                line = System.Text.RegularExpressions.Regex.Replace(line, @"\s+", " ");
                inlineRecipients = line.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                for (Int16 currentInlineRecipient = 0; currentInlineRecipient < inlineRecipients.Length; currentInlineRecipient++)
                {
                    if (inlineRecipients[currentInlineRecipient] == " " || inlineRecipients[currentInlineRecipient] == "\"" ||
                        inlineRecipients[currentInlineRecipient] == ":")
                        continue;
                    if (currentInlineRecipient == 0 && recipientName != "")
                        addresses.Add(new ActiveUp.Net.Mail.Address(inlineRecipients[currentInlineRecipient], recipientName));
                    else
                        addresses.Add(new ActiveUp.Net.Mail.Address(inlineRecipients[currentInlineRecipient]));
                }
            }
            return addresses;
        }
    }
}