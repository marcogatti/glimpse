using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Glimpse.Exceptions;

namespace Glimpse.Exceptions.MailInterfacesExceptions
{
    public class MailReadingOverflowException : GlimpseException
    {
        public MailReadingOverflowException(String glimpseMessage)
            : base(glimpseMessage)
        {
        }
    }
}
