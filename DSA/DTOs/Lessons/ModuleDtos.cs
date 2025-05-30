using System;
using System.Collections.Generic;

namespace DSA.DTOs.Lessons
{
    public class ModulesResponse
    {
        public List<ModuleDto> Modules { get; set; } = new();
        public int TotalModules { get; set; }
    }

    public class ModuleDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsActive { get; set; }
        public int LessonCount { get; set; }
        public int QuizCount { get; set; }
        public int CompletedLessonCount { get; set; }
        public int CompletedQuizCount { get; set; }
        public bool IsCompleted { get; set; }
        public int ProgressPercentage { get; set; }
    }

    public class ModuleDetailDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsActive { get; set; }
        public List<LessonSummaryDto> Lessons { get; set; } = new();
        public List<QuizSummaryDto> Quizzes { get; set; } = new();
        public bool IsCompleted { get; set; }
        public int ProgressPercentage { get; set; }
        public ModuleDependencyDto? NextModule { get; set; }
    }

    public class LessonSummaryDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Order { get; set; }
        public int XpReward { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsActive { get; set; }
        public int StepCount { get; set; }
        public int CompleteStepCount { get; set; }
        public int ProgressPercentage { get; set; }
    }

    public class QuizSummaryDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int XpReward { get; set; }
        public int TimeLimit { get; set; }
        public bool IsCompleted { get; set; }
        public int? BestScore { get; set; }
        public int QuestionCount { get; set; }
    }

    public class ModuleDependencyDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int Order { get; set; }
    }
}