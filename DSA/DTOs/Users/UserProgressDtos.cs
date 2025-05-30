using System;
using System.Collections.Generic;

namespace DSA.DTOs.Users
{
    public class UserProgressResponse
    {
        public int TotalModules { get; set; }
        public int CompletedModules { get; set; }
        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }
        public int TotalQuizzes { get; set; }
        public int CompletedQuizzes { get; set; }
        public int OverallProgressPercentage { get; set; }
        public List<ModuleProgressDto> ModuleProgresses { get; set; } = new();
    }

    public class ModuleProgressDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }
        public int TotalQuizzes { get; set; }
        public int CompletedQuizzes { get; set; }
        public int ProgressPercentage { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class UserXpResponse
    {
        public int TotalXp { get; set; }
        public int CurrentLevel { get; set; }
        public int XpForCurrentLevel { get; set; }
        public int XpForNextLevel { get; set; }
        public int XpProgress { get; set; } // Percentage towards next level
        public List<XpHistoryItemDto> RecentXpHistory { get; set; } = new();
    }

    public class XpHistoryItemDto
    {
        public Guid Id { get; set; }
        public int Amount { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class UserStreakResponse
    {
        public int CurrentStreak { get; set; }
        public int MaxStreak { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public bool IsActiveToday { get; set; }
        public int DaysUntilStreakLost { get; set; }
        public List<StreakDayDto> RecentDays { get; set; } = new();
    }

    public class StreakDayDto
    {
        public DateTime Date { get; set; }
        public bool WasActive { get; set; }
    }
}