using Glimpse.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Glimpse.MailInterfaces
{
    class InvalidRecipientsException : GlimpseException
    {
        public InvalidRecipientsException(String glimpseMessage, Exception inner)
            : base(glimpseMessage, inner)
        {
        }
    }
}
