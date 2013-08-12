﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using glimpse.Exceptions;

namespace glimpse.Exceptions.Internationalization
{
    public class LanguageElementNotFoundException : GlimpseException
    {
        public LanguageElementNotFoundException(String glimpseMessage)
            : base(glimpseMessage)
        {
        }
    }
}