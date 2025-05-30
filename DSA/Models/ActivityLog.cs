using System;

namespace DSA.Models
{
    public class ActivityLog
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public ActivityType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int XpEarned { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }

        // Navigation property
        public User User { get; set; } = null!;
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