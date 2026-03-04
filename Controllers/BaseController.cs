using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebApplication2.Controllers
{
    public class BaseController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Disable cache for all requests
            Response.Headers.CacheControl = "no-cache, no-store, must-revalidate, private";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "-1";

            base.OnActionExecuting(context);
        }

        protected JsonResult SuccessResponse(string message, object? data = null)
        {
            return Json(new
            {
                success = true,
                message,
                data,
                timestamp = DateTime.Now.Ticks
            });
        }

        protected JsonResult ErrorResponse(string message, int statusCode = 400)
        {
            Response.StatusCode = statusCode;
            return Json(new
            {
                success = false,
                message,
                timestamp = DateTime.Now.Ticks
            });
        }
    }
}