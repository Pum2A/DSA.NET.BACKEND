using DSA.Core.Interfaces;
using DSA.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DSA.Infrastructure.Data;

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
        public async Task<IActionResult> GetUserStreak()
        {
            string userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID cannot be determined.");
            }

            int streak = await GetCurrentStreakAsync(userId);
            return Ok(new { Streak = streak });
        }

        private async Task<int> GetCurrentStreakAsync(string userId)
        {
            // Example logic to calculate the user's streak
            var today = DateTime.UtcNow.Date;
            var activities = await _context.UserActivities
                .Where(a => a.UserId == userId && a.ActionTime.Date <= today)
                .OrderByDescending(a => a.ActionTime)
                .ToListAsync();

            int streak = 0;
            DateTime? lastDate = null;

            foreach (var activity in activities)
            {
                if (lastDate == null)
                {
                    lastDate = activity.ActionTime.Date;
                    streak++;
                }
                else if (lastDate.Value.AddDays(-1) == activity.ActionTime.Date)
                {
                    lastDate = activity.ActionTime.Date;
                    streak++;
                }
                else
                {
                    break;
                }
            }

            return streak;
        }
    }
}