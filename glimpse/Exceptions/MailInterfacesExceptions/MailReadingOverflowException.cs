using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace glimpse.Exceptions.MailInterfacesExceptions
{
    public class MailReadingOverflowException : GlimpseException
    {
        public MailReadingOverflowException(String glimpseMessage)
            : base(glimpseMessage)
        {
        }
    }
}
