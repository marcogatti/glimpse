using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Glimpse.ErrorLogging
{
    public class ErrorHandlingControllerFactory : DefaultControllerFactory
    {
        public override IController CreateController(
        RequestContext requestContext,
        string controllerName)
        {
            var controller =
            base.CreateController(requestContext,
            controllerName);

            var c = controller as Controller;

            if (c != null)
            {
                c.ActionInvoker =
                new ErrorHandlingActionInvoker(
                new HandleErrorWithELMAHAttribute());
            }

            return controller;
        }
    }
}