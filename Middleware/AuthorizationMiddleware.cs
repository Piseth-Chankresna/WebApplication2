using System.Security.Claims;

namespace WebApplication2.Middleware
{
    public class AuthorizationMiddleware(RequestDelegate next, ILogger<AuthorizationMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<AuthorizationMiddleware> _logger = logger;

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.ToString().ToLower();

            // បញ្ជី Path ដែលមិនត្រូវការ Login
            var publicPaths = new[]
            {
                "/account/login",
                "/account/register",
                "/account/accessdenied",
                "/css",
                "/js",
                "/lib",
                "/images",
                "/uploads"
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
                _logger.LogWarning($"Unauthorized access attempt to {path}");
                context.Response.Redirect("/Account/Login");
                return;
            }

            // បើ User បាន Login ហើយ អនុញ្ញាតឲ្យចូល
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