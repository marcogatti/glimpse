using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Glimpse.Attributes
{
    public class AjaxOnly : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext filterContext) { }  
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!filterContext.HttpContext.Request.IsAjaxRequest())
                filterContext.Result = new HttpNotFoundResult();
        }
        
    }
}