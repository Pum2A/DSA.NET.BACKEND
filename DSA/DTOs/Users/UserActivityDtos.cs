using System;
using System.Collections.Generic;

namespace DSA.DTOs.Users
{
    public class UserActivityRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class UserActivityResponse
    {
        public List<UserActivityItemDto> Activities { get; set; } = new();
        public int TotalActivities { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class UserActivityItemDto
    {
        public Guid Id { get; set; }
        public ActivityType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int XpEarned { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
    }

    public enum ActivityType
    {
        LessonCompleted,
        QuizCompleted,
        StreakMilestone,
        LevelUp,
        ModuleCompleted
    }
}