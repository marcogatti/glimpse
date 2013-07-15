using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace glimpse.Exceptions.MailInterfacesExceptions
{
    public class InvalidConnectionException:GlimpseException
    {
        public InvalidConnectionException(String glimpseMessage) : base(glimpseMessage)
        {
        }
    }
}