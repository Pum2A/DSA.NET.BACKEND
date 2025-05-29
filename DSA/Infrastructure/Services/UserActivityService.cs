using DSA.Core.Entities;
using DSA.Core.Interfaces;
using DSA.Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace DSA.Infrastructure.Services
{
    public class UserActivityService : IUserActivityService
    {
        private readonly ApplicationDbContext _context;
        public UserActivityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogActivityAsync(string userId, UserActionType actionType, string? referenceId = null, string? additionalInfo = null)
        {
            var activity = new UserActivity
            {
                UserId = userId,
                ActionType = actionType,
                ActionTime = DateTime.UtcNow,
                ReferenceId = referenceId,
                AdditionalInfo = additionalInfo
            };
            _context.UserActivities.Add(activity);
            await _context.SaveChangesAsync();
        }
    }
}