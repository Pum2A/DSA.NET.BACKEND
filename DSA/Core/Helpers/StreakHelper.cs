using System;
using System.Collections.Generic;
using System.Linq;

namespace DSA.Core.Helpers
{
    public static class StreakHelper
    {
        // Przyjmuje listę dat aktywności użytkownika (Date tylko, bez czasu)
        public static int CalculateStreak(IEnumerable<DateTime> activityDates, DateTime? todayOverride = null)
        {
            var days = activityDates
                .Select(d => d.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();

            if (!days.Any())
                return 0;

            int streak = 0;
            var today = (todayOverride ?? DateTime.UtcNow).Date;

            foreach (var day in days)
            {
                if (day == today.AddDays(-streak))
                    streak++;
                else
                    break;
            }
            return streak;
        }

        public static int CalculateStreakBonusXp(int streak)
        {
            if (streak >= 30) return 50;
            if (streak >= 14) return 30;
            if (streak >= 7) return 20;
            if (streak >= 3) return 10;
            return 0;
        }
    }
}