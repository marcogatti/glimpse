using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.Exceptions.ControllersExceptions
{
    public class CookieNotFoundException : GlimpseException
    {
        public CookieNotFoundException(String glimpseMessage)
            : base(glimpseMessage)
        {
        }
    }
}