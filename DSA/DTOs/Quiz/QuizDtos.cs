using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DSA.DTOs.Quizzes
{
    public class ModuleQuizzesResponse
    {
        public Guid ModuleId { get; set; }
        public string ModuleTitle { get; set; } = string.Empty;
        public List<QuizDto> Quizzes { get; set; } = new();
        public int TotalQuizzes { get; set; }
    }

    public class QuizDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int XpReward { get; set; }
        public int TimeLimit { get; set; } // in minutes
        public bool IsActive { get; set; }
        public int QuestionCount { get; set; }
        public bool IsCompleted { get; set; }
        public int? BestScore { get; set; }
        public int? BestPercentage { get; set; }
        public int AttemptCount { get; set; }
    }

    public class QuizDetailDto
    {
        public Guid Id { get; set; }
        public Guid ModuleId { get; set; }
        public string ModuleTitle { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int XpReward { get; set; }
        public int TimeLimit { get; set; } // in minutes
        public bool IsActive { get; set; }
        public List<QuizQuestionDto> Questions { get; set; } = new();
        public bool IsCompleted { get; set; }
        public int AttemptCount { get; set; }
    }

    public class QuizQuestionDto
    {
        public Guid Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string? CodeSnippet { get; set; }
        public string QuestionType { get; set; } = string.Empty; // "SingleChoice", "MultipleChoice", "TrueFalse"
        public List<QuizOptionDto> Options { get; set; } = new();
    }

    public class QuizOptionDto
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public class QuizSubmitRequest
    {
        [Required]
        public DateTime StartedAt { get; set; }

        [Required]
        public List<QuizAnswerSubmitDto> Answers { get; set; } = new();
    }

    public class QuizAnswerSubmitDto
    {
        [Required]
        public Guid QuestionId { get; set; }

        [Required]
        public List<Guid> SelectedOptionIds { get; set; } = new();
    }

    public class QuizResultResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public Guid? ResultId { get; set; }
        public int Score { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int ScorePercentage { get; set; }
        public int XpEarned { get; set; }
        public string Grade { get; set; } = string.Empty;
        public List<QuizAnswerResultDto> AnswerResults { get; set; } = new();
        public bool IsPassing { get; set; }
        public bool IsFirstAttempt { get; set; }
        public bool IsPersonalBest { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }
    }

    public class QuizAnswerResultDto
    {
        public Guid QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public List<Guid> SelectedOptionIds { get; set; } = new();
        public List<Guid> CorrectOptionIds { get; set; } = new();
        public bool IsCorrect { get; set; }
        public string? Explanation { get; set; }
    }

    public class UserQuizResultsResponse
    {
        public Guid QuizId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
        public int AttemptCount { get; set; }
        public int? BestScore { get; set; }
        public int? BestPercentage { get; set; }
        public DateTime? FirstAttemptDate { get; set; }
        public DateTime? LastAttemptDate { get; set; }
        public List<QuizAttemptDto> Attempts { get; set; } = new();
    }

    public class QuizAttemptDto
    {
        public Guid Id { get; set; }
        public int Score { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int ScorePercentage { get; set; }
        public int XpEarned { get; set; }
        public string Grade { get; set; } = string.Empty;
        public bool IsPassing { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }
        public TimeSpan Duration { get; set; }
    }
}