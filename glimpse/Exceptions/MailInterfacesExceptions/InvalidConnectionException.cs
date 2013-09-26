using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.Exceptions.MailInterfacesExceptions
{
    public class InvalidConnectionException : GlimpseException
    {
        public InvalidConnectionException(String glimpseMessage)
            : base(glimpseMessage)
        {
        }
    }
}