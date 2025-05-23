// DSA.Infrastructure/Services/UserActivityService.cs
using DSA.Core.Entities.User;
using DSA.Core.Enums;
using DSA.Core.Interfaces;
using DSA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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

        public async Task LogActivityAsync(string userId, UserActionType actionType, string referenceId = null, string additionalInfo = null)
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

        public async Task<List<UserActivity>> GetUserActivitiesAsync(string userId, int count = 10)
        {
            return await _context.UserActivities
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.ActionTime)
                .Take(count)
                .ToListAsync();
        }
    }
}