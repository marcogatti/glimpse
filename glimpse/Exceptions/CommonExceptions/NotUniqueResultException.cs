using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Glimpse.Exceptions
{
    class NotUniqueResultException : GlimpseException
    {
        public NotUniqueResultException(Exception innerException, String description) : base(description, innerException) { }

        public NotUniqueResultException(String description) : base(description) { }
    }
}
