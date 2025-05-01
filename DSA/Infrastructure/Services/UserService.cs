namespace DSA.Infrastructure.Services
{
    using DSA.Core.Achieviements;
    using DSA.Core.Entities;
    using DSA.Core.Helpers;
    using DSA.Core.Interfaces;
    using Microsoft.EntityFrameworkCore;

    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;
        private readonly INotificationService _notificationService;

        public UserService(
            ApplicationDbContext context,
            ILogger<UserService> logger,
            INotificationService notificationService
        )
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<ApplicationUser> GetUserByIdAsync(string userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<bool> AddExperienceAsync(string userId, int amount)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"User {userId} not found when trying to add XP");
                    return false;
                }

                int previousLevel = user.Level;
                int previousXp = user.ExperiencePoints;

                user.ExperiencePoints += amount;
                user.Level = LevelingHelper.GetLevelForXp(user.ExperiencePoints);

                await _context.SaveChangesAsync();

                // Level up notification
                if (user.Level > previousLevel)
                {
                    _logger.LogInformation($"User {userId} leveled up from {previousLevel} to {user.Level}");
                    await _notificationService.SendNotificationAsync(userId,
                        $"Gratulacje! Awansowałeś na poziom {user.Level} 🚀", "level-up");
                }

                // Check generic achievements (XP, Level)
                var completedLessons = await _context.UserProgress
                    .Where(p => p.UserId == userId && p.IsCompleted)
                    .CountAsync();

                var ctx = new UserAchievementContext
                {
                    PreviousXp = previousXp,
                    CurrentXp = user.ExperiencePoints,
                    PreviousLevel = previousLevel,
                    CurrentLevel = user.Level,
                    CompletedLessons = completedLessons,
                    Streak = await GetCurrentStreakAsync(userId)
                };

                await CheckAndNotifyAchievementsAsync(userId, ctx);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding XP for user {userId}");
                return false;
            }
        }

        public async Task<int> GetUserLevelAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.Level ?? 1;
        }

        /// <summary>
        /// Sprawdza streak użytkownika i przyznaje osiągnięcia oraz powiadomienia.
        /// </summary>
        public async Task CheckAndNotifyStreakAsync(string userId)
        {
            int streak = await GetCurrentStreakAsync(userId);

            // milestone streak notifications
            int[] milestones = { 3, 7, 14, 30, 100, 365 };
            if (milestones.Contains(streak))
            {
                string msg = $"Gratulacje! Utrzymujesz streak {streak} dni 🔥. Otrzymujesz dodatkowe XP!";
                await _notificationService.SendNotificationAsync(userId, msg, "streak");
            }

            // Sprawdź dedykowane achievementy streakowe
            var previousXp = 0; // streak logic nie wymaga XP, ale context wymaga
            var previousLevel = 0;
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                previousXp = user.ExperiencePoints;
                previousLevel = user.Level;
            }

            var completedLessons = await _context.UserProgress
                .Where(p => p.UserId == userId && p.IsCompleted)
                .CountAsync();

            var ctx = new UserAchievementContext
            {
                PreviousXp = previousXp,
                CurrentXp = previousXp,
                PreviousLevel = previousLevel,
                CurrentLevel = previousLevel,
                CompletedLessons = completedLessons,
                Streak = streak
            };

            await CheckAndNotifyAchievementsAsync(userId, ctx);
        }

        /// <summary>
        /// Sprawdź i przyznaj osiągnięcia progresu lekcji. Wywołuj po ukończeniu lekcji!
        /// </summary>
        public async Task CheckAndNotifyLessonAchievementsAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            var completedLessons = await _context.UserProgress
                .Where(p => p.UserId == userId && p.IsCompleted)
                .CountAsync();

            var ctx = new UserAchievementContext
            {
                PreviousXp = user?.ExperiencePoints ?? 0,
                CurrentXp = user?.ExperiencePoints ?? 0,
                PreviousLevel = user?.Level ?? 1,
                CurrentLevel = user?.Level ?? 1,
                CompletedLessons = completedLessons,
                Streak = await GetCurrentStreakAsync(userId)
            };

            await CheckAndNotifyAchievementsAsync(userId, ctx);
        }

        /// <summary>
        /// Centralna metoda sprawdzająca i przyznająca osiągnięcia (powiadomienia).
        /// </summary>
        private async Task CheckAndNotifyAchievementsAsync(string userId, UserAchievementContext ctx)
        {
            // Pobierz już przyznane osiągnięcia (po wiadomości)
            var userAchievements = await _context.Notifications
                .Where(n => n.UserId == userId && n.Type == "achievement")
                .Select(n => n.Message)
                .ToListAsync();

            foreach (var rule in AchievementCatalog.Rules)
            {
                if (rule.Condition(ctx) && !userAchievements.Contains(rule.Message))
                {
                    await _notificationService.SendNotificationAsync(userId, rule.Message, rule.Type);
                }
            }
        }

        /// <summary>
        /// Utility: Oblicz streak użytkownika
        /// </summary>
        private async Task<int> GetCurrentStreakAsync(string userId)
        {
            // Pobierz wszystkie daty aktywności użytkownika (np. ukończone lekcje)
            var activityDates = await _context.UserProgress
                .Where(p => p.UserId == userId && p.IsCompleted)
                .Select(p => p.CompletedAt.HasValue ? p.CompletedAt.Value.Date : p.StartedAt.Value.Date)
                .ToListAsync();

            return StreakHelper.CalculateStreak(activityDates);
        }
    }
}