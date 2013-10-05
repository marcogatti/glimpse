using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.Exceptions.ModelsExceptions
{
    public class WrongPasswordException : GlimpseException
    {
        public WrongPasswordException(String message) : base(message) { }
        public WrongPasswordException(String systemMessage, String glimpseMessage)
            : base(systemMessage, glimpseMessage)
        {
        }
    }
}