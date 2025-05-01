using DSA.Core.Interfaces;
using DSA.Infrastructure;
using DSA.Infrastructure.Data;
using DSA.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DSA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService; // Fix for CS0103 and CS0119

        public NotificationController(ApplicationDbContext context, INotificationService notificationService) // Inject INotificationService
        {
            _context = context;
            _notificationService = notificationService; // Assign injected service to the field
        }

        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
            return Ok(notifications);
        }

        [HttpPost("{notificationId}/mark-as-read")]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            try
            {
                await _notificationService.MarkNotificationAsReadAsync(notificationId);
                return Ok(new { message = "Notification marked as read." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}