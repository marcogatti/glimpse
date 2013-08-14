using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Glimpse.Exceptions;

namespace Glimpse.Exceptions.MailInterfacesExceptions
{
    public class InvalidAttachmentException : GlimpseException
    {
        public InvalidAttachmentException(String systemMessage, String glimpseMessage)
            : base(systemMessage, glimpseMessage)
        {
        }
    }
}
