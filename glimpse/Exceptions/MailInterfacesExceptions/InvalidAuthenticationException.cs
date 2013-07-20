using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace glimpse.Exceptions.MailInterfacesExceptions
{
    public class InvalidAuthenticationException : GlimpseException
    {
        public InvalidAuthenticationException(String systemMessage, String glimpseMessage)
            : base(systemMessage, glimpseMessage)
        {
        }
    }
}