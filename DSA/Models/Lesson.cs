using System;
using System.Collections.Generic;

namespace DSA.Models
{
    public class Lesson
    {
        public Guid Id { get; set; }
        public Guid ModuleId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int XpReward { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; }

        // Navigation properties
        public Module Module { get; set; } = null!;
        public List<LessonStep> Steps { get; set; } = new();
        public List<UserProgress> UserProgresses { get; set; } = new();
    }

    public class LessonStep
    {
        public Guid Id { get; set; }
        public Guid LessonId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? CodeExample { get; set; }
        public int Order { get; set; }

        // Navigation properties
        public Lesson Lesson { get; set; } = null!;
        public List<StepProgress> StepProgresses { get; set; } = new();
    }

    public class UserProgress
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid LessonId { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public Lesson Lesson { get; set; } = null!;
        public List<StepProgress> StepProgresses { get; set; } = new();
    }

    public class StepProgress
    {
        public Guid Id { get; set; }
        public Guid UserProgressId { get; set; }
        public Guid LessonStepId { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Navigation properties
        public UserProgress UserProgress { get; set; } = null!;
        public LessonStep LessonStep { get; set; } = null!;
    }
}