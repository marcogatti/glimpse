using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using glimpse.Exceptions;

namespace glimpse.Exceptions.MailInterfacesExceptions
{
    class InvalidAttachmentException : GlimpseException
    {
        public InvalidAttachmentException(String systemMessage, String glimpseMessage)
            : base(systemMessage, glimpseMessage)
        {
        }
    }
}
