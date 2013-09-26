using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Glimpse.Exceptions;

namespace Glimpse.Exceptions.MailInterfacesExceptions
{
    public class NoRecipientsException : GlimpseException
    {
        public NoRecipientsException(String glimpseMessage)
            : base(glimpseMessage)
        {
        }
    }
}
