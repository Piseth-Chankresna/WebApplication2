using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebApplication2.Services;

namespace WebApplication2.Middleware
{
    public class AuthorizationMiddleware(RequestDelegate next, ILogger<AuthorizationMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<AuthorizationMiddleware> _logger = logger;

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.ToString().ToLower();
            var method = context.Request.Method.ToUpper();

            // បញ្ជី Path ដែលមិនត្រូវការ Login
            var publicPaths = new[]
            {
                "/account/login",
                "/account/register",
                "/account/roleselection",
                "/account/adminlogin",
                "/account/userlogin",
                "/account/accessdenied",
                "/account/logout",
                "/css",
                "/js",
                "/lib",
                "/images",
                "/uploads",
                "/favicon.ico",
                "/home/index",
                "/"
            };

            // ពិនិត្យថាតើ Path បច្ចុប្បន្នជា Public Path ឬអត់
            bool isPublicPath = publicPaths.Any(p => path.StartsWith(p));

            // ពិនិត្យថាតើ User បាន Login ហើយឬនៅ
            bool isAuthenticated = context.User?.Identity?.IsAuthenticated == true;

            // បើជា Public Path អនុញ្ញាតឲ្យចូល
            if (isPublicPath)
            {
                await _next(context);
                return;
            }

            // បើ User មិនទាន់ Login ហើយព្យាយាមចូល Private Path
            if (!isAuthenticated)
            {
                _logger.LogWarning("Unauthorized access attempt to {Path}", path);
                context.Response.Redirect("/Account/Login");
                return;
            }

            // បើ User បាន Login ហើយ អនុញ្ញាតឲ្យចូល
            if (isAuthenticated)
            {
                // Allow authenticated users to access their dashboard
                if (path == "/user/useraccount" || path == "/home/index")
                {
                    await _next(context);
                    return;
                }

                // Extract controller and action from route
                var routeData = context.GetRouteData();
                var controllerName = routeData.Values["controller"]?.ToString()?.ToLower();
                var actionName = routeData.Values["action"]?.ToString()?.ToLower();

                if (!string.IsNullOrEmpty(controllerName) && !string.IsNullOrEmpty(actionName))
                {
                    // Check if user has permission for this controller/action
                    if (context.User != null && !PermissionService.CanAccess(context.User, controllerName, actionName))
                    {
                        _logger.LogWarning("User {UserName} denied access to {Controller}/{Action}", context.User.Identity?.Name ?? "Unknown", controllerName, actionName);
                        context.Response.Redirect("/Account/AccessDenied");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }

    // Extension method ដើម្បីងាយស្រួលប្រើ
    public static class AuthorizationMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomAuthorization(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthorizationMiddleware>();
        }
    }
}