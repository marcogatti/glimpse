using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using glimpse.Exceptions;

namespace glimpse.Exceptions.CommonExceptions
{
    public class DefaultLanguageNotSettedException : GlimpseException
    {
        public DefaultLanguageNotSettedException(String glimpseMessage)
            : base(glimpseMessage)
        {
        }
    }
}