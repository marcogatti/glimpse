using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Glimpse.Exceptions;

namespace Glimpse.Exceptions.Internationalization
{
    public class LanguageElementNotFoundException : GlimpseException
    {
        public LanguageElementNotFoundException(String glimpseMessage)
            : base(glimpseMessage)
        {
        }
    }
}