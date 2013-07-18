using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace glimpse.Exceptions.MailInterfacesExceptions
{
    public class GlimpseException:Exception
    {
        public String GlimpseMessage { get; set; }
        public String SystemMessage { get; set; }

        public GlimpseException(String glimpseMessage)
        {
            this.GlimpseMessage = glimpseMessage;
        }

        public GlimpseException(String systemMessage, String glimpseMessage)
        {
            this.GlimpseMessage = glimpseMessage;
            this.SystemMessage = systemMessage;
        }

        public GlimpseException(SystemException exception, String glimpseMessage)
        {
            this.GlimpseMessage = glimpseMessage;
            this.SystemMessage = exception.Message;
        }
    }
}
