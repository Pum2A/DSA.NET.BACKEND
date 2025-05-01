using DSA.Core.DTOs.Auth;
using DSA.Core.Helpers;
using Microsoft.EntityFrameworkCore;

namespace DSA.Infrastructure.Services
{
    public class RankingService
    {
        private readonly ApplicationDbContext _context;

        public RankingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserProfileDto>> GetRankingByLevelAsync(int page, int limit)
        {
            // Step 1: Get basic user data from the database
            var userData = await _context.Users
                .OrderByDescending(u => u.Level)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Level,
                    u.JoinedAt
                })
                .ToListAsync();

            // Step 2: Calculate the streak for each user in memory
            var userProfiles = new List<UserProfileDto>();
            foreach (var user in userData)
            {
                // Fetch user activity dates
                var activityDates = await _context.UserProgress
                    .Where(p => p.UserId == user.Id && p.IsCompleted)
                    .Select(p => p.CompletedAt.HasValue ? p.CompletedAt.Value.Date : p.StartedAt.Value.Date)
                    .Distinct()
                    .ToListAsync();

                // Calculate the streak
                var streak = StreakHelper.CalculateStreak(activityDates);

                // Add to the result list
                userProfiles.Add(new UserProfileDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Level = user.Level,
                    JoinedAt = user.JoinedAt,
                    Streak = streak
                });
            }

            return userProfiles;
        }

        public async Task<List<UserProfileDto>> GetRankingByStreakAsync(int page, int limit)
        {
            // Step 1: Get all users
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Level,
                    u.JoinedAt
                })
                .ToListAsync();

            // Step 2: Calculate streaks in memory
            var userProfiles = new List<UserProfileDto>();
            foreach (var user in users)
            {
                // Fetch user activity dates
                var activityDates = await _context.UserProgress
                    .Where(p => p.UserId == user.Id && p.IsCompleted)
                    .Select(p => p.CompletedAt.HasValue ? p.CompletedAt.Value.Date : p.StartedAt.Value.Date)
                    .Distinct()
                    .ToListAsync();

                // Calculate the streak
                var streak = StreakHelper.CalculateStreak(activityDates);

                // Add to the result list
                userProfiles.Add(new UserProfileDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Level = user.Level,
                    JoinedAt = user.JoinedAt,
                    Streak = streak
                });
            }

            // Step 3: Sort by streak and apply pagination
            return userProfiles
                .OrderByDescending(u => u.Streak)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToList();
        }

        public async Task<List<UserProfileDto>> GetRankingByJoinedTimeAsync(int page, int limit)
        {
            // Step 1: Get basic user data from the database
            var userData = await _context.Users
                .OrderBy(u => u.JoinedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Level,
                    u.JoinedAt
                })
                .ToListAsync();

            // Step 2: Calculate the streak for each user in memory
            var userProfiles = new List<UserProfileDto>();
            foreach (var user in userData)
            {
                // Fetch user activity dates
                var activityDates = await _context.UserProgress
                    .Where(p => p.UserId == user.Id && p.IsCompleted)
                    .Select(p => p.CompletedAt.HasValue ? p.CompletedAt.Value.Date : p.StartedAt.Value.Date)
                    .Distinct()
                    .ToListAsync();

                // Calculate the streak
                var streak = StreakHelper.CalculateStreak(activityDates);

                // Add to the result list
                userProfiles.Add(new UserProfileDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Level = user.Level,
                    JoinedAt = user.JoinedAt,
                    Streak = streak
                });
            }

            return userProfiles;
        }
    }
}