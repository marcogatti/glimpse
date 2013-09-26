using Glimpse.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Glimpse.MailInterfaces
{
    class CouldNotSelectMailboxException : GlimpseException
    {
        public CouldNotSelectMailboxException(String glimpseMessage, Exception inner)
            : base(glimpseMessage, inner)
        {
        }
    }
}
