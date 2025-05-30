using System;
using System.Collections.Generic;

namespace DSA.DTOs.Lessons
{
    public class LessonStepsResponse
    {
        public Guid LessonId { get; set; }
        public string LessonTitle { get; set; } = string.Empty;
        public List<LessonStepDto> Steps { get; set; } = new();
        public int TotalSteps { get; set; }
        public int CompletedSteps { get; set; }
        public int ProgressPercentage { get; set; }
    }

    public class LessonStepDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? CodeExample { get; set; }
        public int Order { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class StepCompleteResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public bool LessonCompleted { get; set; }
        public int XpEarned { get; set; }
        public LessonStepDto? Step { get; set; }
    }

    public class LessonCompleteResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int TotalXpEarned { get; set; }
        public bool ModuleCompleted { get; set; }
        public Guid? NextLessonId { get; set; }
        public Guid? NextQuizId { get; set; }
    }

    public class LessonProgressDto
    {
        public Guid LessonId { get; set; }
        public bool IsStarted { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int StepCount { get; set; }
        public int CompletedStepCount { get; set; }
        public int ProgressPercentage { get; set; }
        public List<StepProgressDto> StepProgresses { get; set; } = new();
    }

    public class StepProgressDto
    {
        public Guid StepId { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class UserProgressResponse
    {
        public int TotalModules { get; set; }
        public int CompletedModules { get; set; }
        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }
        public int OverallProgressPercentage { get; set; }
        public List<ModuleProgressDto> ModuleProgresses { get; set; } = new();
    }

    public class ModuleProgressDto
    {
        public Guid ModuleId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Order { get; set; }
        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }
        public int ProgressPercentage { get; set; }
        public bool IsCompleted { get; set; }
        public List<LessonProgressSummaryDto> LessonProgresses { get; set; } = new();
    }

    public class LessonProgressSummaryDto
    {
        public Guid LessonId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsCompleted { get; set; }
        public int ProgressPercentage { get; set; }
    }
}