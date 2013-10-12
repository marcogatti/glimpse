using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.Exceptions.MailInterfacesExceptions
{
    public class InvalidAuthenticationException : GlimpseException
    {
        public InvalidAuthenticationException(String systemMessage, String glimpseMessage)
            : base(systemMessage, glimpseMessage)
        {
        }

        public InvalidAuthenticationException(String glimpseMessage)
            : base(glimpseMessage)
        {
        }
    }
}