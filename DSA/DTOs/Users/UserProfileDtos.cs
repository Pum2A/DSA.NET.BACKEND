using System;

namespace DSA.DTOs.Users
{
    public class UserProfileDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public bool EmailVerified { get; set; }
        public int XpPoints { get; set; }
        public int CurrentStreak { get; set; }
        public int MaxStreak { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserStatsDto Stats { get; set; } = new();
    }

    public class PublicUserProfileDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public int XpPoints { get; set; }
        public int CurrentStreak { get; set; }
        public int MaxStreak { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserStatsDto Stats { get; set; } = new();
    }

    public class UserStatsDto
    {
        public int CompletedLessons { get; set; }
        public int CompletedModules { get; set; }
        public int CompletedQuizzes { get; set; }
        public double AverageQuizScore { get; set; }
        public int Ranking { get; set; }
    }

    public class UpdateProfileRequest
    {
        public string? Username { get; set; }
        public string? Avatar { get; set; }
    }

    public class UpdateProfileResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public UserProfileDto? Data { get; set; }
    }
}