using System;
using System.Web;

namespace Glimpse.DataAccessLayer.Entities
{
    public class LogEntity
    {
        public virtual Int64 Id { get; set; }
        public virtual Int32 Code { get; set; }
        public virtual String Message { get; set; }
        public virtual String StackTrace { get; set; }
        public virtual DateTime Date { get; set; }

        public LogEntity() { }

        public LogEntity(Int32 code, String message, String stackTrace = null)
        {
            this.Message = message;
            this.Code = code;
            this.StackTrace = stackTrace;
            this.Date = DateTime.Now;
        }
    }
}