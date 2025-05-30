using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DSA.Models
{
    public class Quiz
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ModuleId { get; set; }

        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int XpReward { get; set; }

        public int TimeLimit { get; set; } // In minutes

        public bool IsActive { get; set; }

        // Navigation properties
        public Module Module { get; set; } = null!;
        public List<QuizQuestion> Questions { get; set; } = new();
        public List<QuizResult> UserResults { get; set; } = new();
    }

    public class QuizQuestion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid QuizId { get; set; }

        [Required]
        public string QuestionText { get; set; } = string.Empty;

        public string? CodeSnippet { get; set; }

        public QuestionType Type { get; set; }

        // Navigation properties
        public Quiz Quiz { get; set; } = null!;
        public List<QuizOption> Options { get; set; } = new();
    }

    public enum QuestionType
    {
        SingleChoice,
        MultipleChoice,
        TrueFalse
    }

    public class QuizOption
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid QuestionId { get; set; }

        [Required]
        public string Text { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }

        // Navigation properties
        public QuizQuestion Question { get; set; } = null!;
    }

    public class QuizResult
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }

        public Guid QuizId { get; set; }

        public int Score { get; set; }

        public int TotalQuestions { get; set; }

        public int CorrectAnswers { get; set; }

        public int XpEarned { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime CompletedAt { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public Quiz Quiz { get; set; } = null!;
        public List<QuizAnswer> Answers { get; set; } = new();
    }

    public class QuizAnswer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid QuizResultId { get; set; }

        public Guid QuestionId { get; set; }

        [Column(TypeName = "jsonb")]
        public List<Guid> SelectedOptionIds { get; set; } = new();

        public bool IsCorrect { get; set; }

        // Navigation properties
        public QuizResult QuizResult { get; set; } = null!;
        public QuizQuestion Question { get; set; } = null!;
    }
}