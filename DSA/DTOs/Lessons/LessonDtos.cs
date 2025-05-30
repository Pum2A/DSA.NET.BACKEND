using System;
using System.Collections.Generic;

namespace DSA.DTOs.Lessons
{
    public class LessonsResponse
    {
        public Guid ModuleId { get; set; }
        public string ModuleTitle { get; set; } = string.Empty;
        public List<LessonDto> Lessons { get; set; } = new();
        public int TotalLessons { get; set; }
    }

    public class LessonDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Order { get; set; }
        public int XpReward { get; set; }
        public bool IsActive { get; set; }
        public bool IsCompleted { get; set; }
        public int StepCount { get; set; }
        public int CompletedStepCount { get; set; }
        public int ProgressPercentage { get; set; }
    }

    public class LessonDetailDto
    {
        public Guid Id { get; set; }
        public Guid ModuleId { get; set; }
        public string ModuleTitle { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Order { get; set; }
        public int XpReward { get; set; }
        public bool IsActive { get; set; }
        public bool IsCompleted { get; set; }
        public LessonProgressDto Progress { get; set; } = new();
        public LessonDependencyDto? PreviousLesson { get; set; }
        public LessonDependencyDto? NextLesson { get; set; }
    }

    public class LessonDependencyDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int Order { get; set; }
        public Guid ModuleId { get; set; }
    }
}