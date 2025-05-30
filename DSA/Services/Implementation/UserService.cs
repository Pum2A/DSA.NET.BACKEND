using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSA.Data;
using DSA.DTOs.Users;
using DSA.Models;
using Microsoft.EntityFrameworkCore;

namespace DSA.Services.Implementation
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.LessonProgresses)
                .Include(u => u.QuizResults)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return null;

            var stats = await CalculateUserStatsAsync(userId);

            return new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                Avatar = user.Avatar,
                EmailVerified = user.EmailVerified,
                XpPoints = user.XpPoints,
                CurrentStreak = user.CurrentStreak,
                MaxStreak = user.MaxStreak,
                LastActivityDate = user.LastActivityDate,
                CreatedAt = user.CreatedAt,
                Stats = stats
            };
        }

        public async Task<UpdateProfileResult> UpdateProfileAsync(Guid userId, UpdateProfileRequest model)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return new UpdateProfileResult
                {
                    Success = false,
                    Message = "User not found."
                };
            }

            // Check if username is already taken by another user
            if (!string.IsNullOrEmpty(model.Username) &&
                model.Username != user.Username &&
                await _context.Users.AnyAsync(u => u.Username == model.Username))
            {
                return new UpdateProfileResult
                {
                    Success = false,
                    Message = "Username is already taken."
                };
            }

            // Update user properties
            if (!string.IsNullOrEmpty(model.Username))
                user.Username = model.Username;

            if (model.Avatar != null)
                user.Avatar = model.Avatar;

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new UpdateProfileResult
            {
                Success = true,
                Message = "Profile updated successfully.",
                Data = await GetUserProfileAsync(userId)
            };
        }

        public async Task<PublicUserProfileDto?> GetPublicUserProfileAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return null;

            var stats = await CalculateUserStatsAsync(userId);

            return new PublicUserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Avatar = user.Avatar,
                XpPoints = user.XpPoints,
                CurrentStreak = user.CurrentStreak,
                MaxStreak = user.MaxStreak,
                LastActivityDate = user.LastActivityDate,
                CreatedAt = user.CreatedAt,
                Stats = stats
            };
        }

        public async Task<UserRankingResponse> GetUserRankingAsync(UserRankingRequest request)
        {
            // Validate request
            request.Page = Math.Max(1, request.Page);
            request.PageSize = Math.Clamp(request.PageSize, 1, 100);

            // Create query
            var query = _context.Users.AsQueryable();

            // Apply ordering
            switch (request.OrderBy.ToLower())
            {
                case "completedlessons":
                    query = request.Descending
                        ? query.OrderByDescending(u => u.LessonProgresses.Count(lp => lp.IsCompleted))
                        : query.OrderBy(u => u.LessonProgresses.Count(lp => lp.IsCompleted));
                    break;

                case "completedquizzes":
                    query = request.Descending
                        ? query.OrderByDescending(u => u.QuizResults.Count)
                        : query.OrderBy(u => u.QuizResults.Count);
                    break;

                case "streak":
                    query = request.Descending
                        ? query.OrderByDescending(u => u.CurrentStreak)
                        : query.OrderBy(u => u.CurrentStreak);
                    break;

                case "xppoints":
                default:
                    query = request.Descending
                        ? query.OrderByDescending(u => u.XpPoints)
                        : query.OrderBy(u => u.XpPoints);
                    break;
            }

            // Calculate total users for pagination
            var totalUsers = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalUsers / (double)request.PageSize);

            // Apply pagination
            var users = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(u => new UserRankingItemDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Avatar = u.Avatar,
                    XpPoints = u.XpPoints,
                    CompletedLessons = u.LessonProgresses.Count(lp => lp.IsCompleted),
                    CompletedQuizzes = u.QuizResults.Count,
                    CurrentStreak = u.CurrentStreak,
                    // We'll calculate the rank below
                    Rank = 0
                })
                .ToListAsync();

            // Calculate ranks
            // Note: This is a simplified ranking. For a real app, you'd want a more efficient approach.
            int startRank = (request.Page - 1) * request.PageSize + 1;
            for (int i = 0; i < users.Count; i++)
            {
                users[i].Rank = startRank + i;
            }

            return new UserRankingResponse
            {
                Users = users,
                TotalUsers = totalUsers,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages
            };
        }

        public async Task<UserActivityResponse> GetUserActivityAsync(Guid userId, UserActivityRequest request)
        {
            // Validate request
            request.Page = Math.Max(1, request.Page);
            request.PageSize = Math.Clamp(request.PageSize, 1, 100);

            // Combine activities from different sources
            var activities = new List<UserActivityItemDto>();

            // Get completed lessons
            var lessonActivities = await _context.UserProgresses
                .Where(up => up.UserId == userId && up.IsCompleted)
                .OrderByDescending(up => up.CompletedAt)
                .Select(up => new UserActivityItemDto
                {
                    Id = up.Id,
                    Type = DSA.DTOs.Users.ActivityType.LessonCompleted, // Fully qualified name
                    Title = $"Completed lesson: {up.Lesson.Title}",
                    Description = $"You've completed {up.Lesson.Title} in module {up.Lesson.Module.Title}",
                    XpEarned = up.Lesson.XpReward,
                    CreatedAt = up.CompletedAt ?? DateTime.UtcNow,
                    RelatedEntityId = up.LessonId,
                    RelatedEntityType = "Lesson"
                })
                .ToListAsync();

            activities.AddRange(lessonActivities);

            // Get completed quizzes
            var quizActivities = await _context.QuizResults
                .Where(qr => qr.UserId == userId)
                .OrderByDescending(qr => qr.CompletedAt)
                .Select(qr => new UserActivityItemDto
                {
                    Id = qr.Id,
                    Type = DSA.DTOs.Users.ActivityType.QuizCompleted, // Fully qualified name
                    Title = $"Completed quiz: {qr.Quiz.Title}",
                    Description = $"You scored {qr.Score}/{qr.TotalQuestions} on {qr.Quiz.Title}",
                    XpEarned = qr.XpEarned,
                    CreatedAt = qr.CompletedAt,
                    RelatedEntityId = qr.QuizId,
                    RelatedEntityType = "Quiz"
                })
                .ToListAsync();

            activities.AddRange(quizActivities);

            // Sort all activities by date
            activities = activities
                .OrderByDescending(a => a.CreatedAt)
                .ToList();

            // Apply date filtering if provided
            if (request.StartDate.HasValue)
            {
                activities = activities
                    .Where(a => a.CreatedAt >= request.StartDate.Value)
                    .ToList();
            }

            if (request.EndDate.HasValue)
            {
                activities = activities
                    .Where(a => a.CreatedAt <= request.EndDate.Value)
                    .ToList();
            }

            // Apply pagination
            var totalActivities = activities.Count;
            var totalPages = (int)Math.Ceiling(totalActivities / (double)request.PageSize);

            var paginatedActivities = activities
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new UserActivityResponse
            {
                Activities = paginatedActivities,
                TotalActivities = totalActivities,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages
            };
        }

        public async Task<UserProgressResponse> GetUserGlobalProgressAsync(Guid userId)
        {
            // Get all modules and their lessons
            var modules = await _context.Modules
                .Include(m => m.Lessons)
                .OrderBy(m => m.Order)
                .ToListAsync();

            // Get user progress for lessons
            var userProgress = await _context.UserProgresses
                .Where(up => up.UserId == userId)
                .ToListAsync();

            // Get user quiz results
            var userQuizResults = await _context.QuizResults
                .Where(qr => qr.UserId == userId)
                .ToListAsync();

            // Calculate statistics
            int totalModules = modules.Count;
            int totalLessons = modules.Sum(m => m.Lessons.Count);
            int totalQuizzes = await _context.Quizzes.CountAsync();

            int completedLessons = userProgress.Count(up => up.IsCompleted);

            // A quiz is considered completed if the user has any result for it
            var completedQuizIds = userQuizResults
                .Select(qr => qr.QuizId)
                .Distinct()
                .ToHashSet();
            int completedQuizzes = completedQuizIds.Count;

            // A module is completed if all its lessons and quizzes are completed
            var moduleProgresses = new List<ModuleProgressDto>();
            int completedModules = 0;

            foreach (var module in modules)
            {
                var moduleLessons = module.Lessons.Count;
                var moduleCompletedLessons = userProgress
                    .Count(up => up.IsCompleted && module.Lessons.Any(l => l.Id == up.LessonId));

                var moduleQuizzes = await _context.Quizzes
                    .Where(q => q.ModuleId == module.Id)
                    .CountAsync();

                var moduleCompletedQuizzes = userQuizResults
                    .Where(qr => _context.Quizzes.Any(q => q.Id == qr.QuizId && q.ModuleId == module.Id))
                    .Select(qr => qr.QuizId)
                    .Distinct()
                    .Count();

                bool isModuleCompleted = moduleLessons > 0 && moduleQuizzes > 0 &&
                                         moduleCompletedLessons == moduleLessons &&
                                         moduleCompletedQuizzes == moduleQuizzes;

                if (isModuleCompleted)
                {
                    completedModules++;
                }

                int progressPercentage = moduleLessons > 0 ?
                    (int)Math.Round((moduleCompletedLessons / (double)moduleLessons) * 100) : 0;

                moduleProgresses.Add(new ModuleProgressDto
                {
                    Id = module.Id,
                    Title = module.Title,
                    TotalLessons = moduleLessons,
                    CompletedLessons = moduleCompletedLessons,
                    TotalQuizzes = moduleQuizzes,
                    CompletedQuizzes = moduleCompletedQuizzes,
                    ProgressPercentage = progressPercentage,
                    IsCompleted = isModuleCompleted
                });
            }

            // Calculate overall progress percentage
            int overallProgressPercentage = totalLessons > 0 ?
                (int)Math.Round((completedLessons / (double)totalLessons) * 100) : 0;

            return new UserProgressResponse
            {
                TotalModules = totalModules,
                CompletedModules = completedModules,
                TotalLessons = totalLessons,
                CompletedLessons = completedLessons,
                TotalQuizzes = totalQuizzes,
                CompletedQuizzes = completedQuizzes,
                OverallProgressPercentage = overallProgressPercentage,
                ModuleProgresses = moduleProgresses
            };
        }

        public async Task<UserXpResponse> GetUserXpAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                throw new ArgumentException("User not found", nameof(userId));

            int totalXp = user.XpPoints;

            // Calculate level based on XP (example formula)
            int currentLevel = CalculateLevel(totalXp);

            // Calculate XP needed for current and next level
            int xpForCurrentLevel = CalculateXpForLevel(currentLevel);
            int xpForNextLevel = CalculateXpForLevel(currentLevel + 1);

            // Calculate progress to next level
            int xpSinceLastLevel = totalXp - xpForCurrentLevel;
            int xpNeededForNextLevel = xpForNextLevel - xpForCurrentLevel;
            int xpProgress = (int)Math.Round((xpSinceLastLevel / (double)xpNeededForNextLevel) * 100);

            // Get recent XP history (last 10 entries)
            var recentXpHistory = new List<XpHistoryItemDto>();

            // From lesson completions
            var lessonXp = await _context.UserProgresses
                .Where(up => up.UserId == userId && up.IsCompleted)
                .OrderByDescending(up => up.CompletedAt)
                .Take(5)
                .Select(up => new XpHistoryItemDto
                {
                    Id = up.Id,
                    Amount = up.Lesson.XpReward,
                    Source = "Lesson",
                    Description = $"Completed lesson: {up.Lesson.Title}",
                    CreatedAt = up.CompletedAt ?? DateTime.UtcNow
                })
                .ToListAsync();

            recentXpHistory.AddRange(lessonXp);

            // From quiz completions
            var quizXp = await _context.QuizResults
                .Where(qr => qr.UserId == userId)
                .OrderByDescending(qr => qr.CompletedAt)
                .Take(5)
                .Select(qr => new XpHistoryItemDto
                {
                    Id = qr.Id,
                    Amount = qr.XpEarned,
                    Source = "Quiz",
                    Description = $"Quiz score: {qr.Score}/{qr.TotalQuestions} on {qr.Quiz.Title}",
                    CreatedAt = qr.CompletedAt
                })
                .ToListAsync();

            recentXpHistory.AddRange(quizXp);

            // Sort by date
            recentXpHistory = recentXpHistory
                .OrderByDescending(x => x.CreatedAt)
                .Take(10)
                .ToList();

            return new UserXpResponse
            {
                TotalXp = totalXp,
                CurrentLevel = currentLevel,
                XpForCurrentLevel = xpForCurrentLevel,
                XpForNextLevel = xpForNextLevel,
                XpProgress = xpProgress,
                RecentXpHistory = recentXpHistory
            };
        }

        public async Task<UserStreakResponse> GetUserStreakAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                throw new ArgumentException("User not found", nameof(userId));

            bool isActiveToday = user.LastActivityDate?.Date == DateTime.UtcNow.Date;

            // Calculate days until streak is lost (1 day if active today, 0 if not)
            int daysUntilStreakLost = isActiveToday ? 1 : 0;

            // Get recent activity for the last 14 days
            var today = DateTime.UtcNow.Date;
            var recentDays = new List<StreakDayDto>();

            for (int i = 13; i >= 0; i--)
            {
                var date = today.AddDays(-i);

                // Check if user was active on this date
                bool wasActive = await _context.UserProgresses
                    .AnyAsync(up => up.UserId == userId &&
                             (up.StartedAt.Date == date ||
                             (up.CompletedAt.HasValue && up.CompletedAt.Value.Date == date)));

                if (!wasActive)
                {
                    // Check quiz activity
                    wasActive = await _context.QuizResults
                        .AnyAsync(qr => qr.UserId == userId &&
                                (qr.StartedAt.Date == date || qr.CompletedAt.Date == date));
                }

                recentDays.Add(new StreakDayDto
                {
                    Date = date,
                    WasActive = wasActive
                });
            }

            return new UserStreakResponse
            {
                CurrentStreak = user.CurrentStreak,
                MaxStreak = user.MaxStreak,
                LastActivityDate = user.LastActivityDate,
                IsActiveToday = isActiveToday,
                DaysUntilStreakLost = daysUntilStreakLost,
                RecentDays = recentDays
            };
        }

        // Helper methods
        private async Task<UserStatsDto> CalculateUserStatsAsync(Guid userId)
        {
            // Calculate completed lessons
            var completedLessonsCount = await _context.UserProgresses
                .CountAsync(up => up.UserId == userId && up.IsCompleted);

            // Calculate completed modules
            var completedModulesCount = await _context.Modules
                .Where(m => m.Lessons.All(l =>
                    _context.UserProgresses.Any(up =>
                        up.UserId == userId && up.LessonId == l.Id && up.IsCompleted)))
                .CountAsync();

            // Calculate completed quizzes and average score
            var quizResults = await _context.QuizResults
                .Where(qr => qr.UserId == userId)
                .ToListAsync();

            var completedQuizCount = quizResults
                .Select(qr => qr.QuizId)
                .Distinct()
                .Count();

            double averageScore = 0;
            if (quizResults.Any())
            {
                averageScore = quizResults.Average(qr => qr.Score / (double)qr.TotalQuestions * 100);
            }

            // Calculate user ranking
            var userXp = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.XpPoints)
                .FirstOrDefaultAsync();

            var ranking = await _context.Users
                .CountAsync(u => u.XpPoints > userXp) + 1;

            return new UserStatsDto
            {
                CompletedLessons = completedLessonsCount,
                CompletedModules = completedModulesCount,
                CompletedQuizzes = completedQuizCount,
                AverageQuizScore = Math.Round(averageScore, 2),
                Ranking = ranking
            };
        }

        private int CalculateLevel(int xp)
        {
            // Simple level calculation: level = sqrt(xp / 100)
            return Math.Max(1, (int)Math.Floor(Math.Sqrt(xp / 100.0)));
        }

        private int CalculateXpForLevel(int level)
        {
            // XP needed for level: level² * 100
            return level * level * 100;
        }
    }
}