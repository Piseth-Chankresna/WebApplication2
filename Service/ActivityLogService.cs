using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Service
{
    public class ActivityLogService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        public async Task LogAsync(int userId, string userName, string action, string description, string status = "ជោគជ័យ")
        {
            try
            {
                var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";

                var log = new ActivityLog
                {
                    UserId = userId,
                    UserName = userName,
                    Action = action,
                    Description = description,
                    IpAddress = ipAddress,
                    Status = status,
                    Timestamp = DateTime.Now
                };

                await _context.ActivityLogs.AddAsync(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging activity: {ex.Message}");
            }
        }
    }
}