using DSA.Core.Interfaces;
using DSA.Infrastructure;
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
    public class UserActivityController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public UserActivityController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetUserActivityHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var activities = _context.UserActivities
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.ActionTime)
                .Take(100)
                .ToList();

            return Ok(activities);
        }

        [HttpGet("streak")]
        public IActionResult GetUserStreak()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var days = _context.UserActivities
                .Where(a => a.UserId == userId)
                .GroupBy(a => a.ActionTime.Date)
                .Select(g => g.Key)
                .OrderByDescending(d => d)
                .ToList();

            int streak = 0;
            DateTime? current = DateTime.UtcNow.Date;
            foreach (var day in days)
            {
                if (day == current)
                {
                    streak++;
                    current = current.Value.AddDays(-1);
                }
                else
                {
                    break;
                }
            }

            return Ok(new { streak });
        }
    }
}