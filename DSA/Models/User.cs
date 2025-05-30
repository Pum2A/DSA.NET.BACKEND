using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DSA.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(64)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string? Avatar { get; set; }

        public bool EmailVerified { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // User statistics and progress
        public int XpPoints { get; set; }

        public int CurrentStreak { get; set; }

        public int MaxStreak { get; set; }

        public DateTime? LastActivityDate { get; set; }

        // Navigation properties
        public List<UserProgress> LessonProgresses { get; set; } = new();

        public List<QuizResult> QuizResults { get; set; } = new();

        public List<RefreshToken> RefreshTokens { get; set; } = new();

    }

    public class RefreshToken
    {
        [Key]
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public User User { get; set; } = null!;

        public string Token { get; set; } = string.Empty;

        public DateTime Expires { get; set; }

        public DateTime Created { get; set; }

        public string? ReplacedByToken { get; set; }

        public DateTime? Revoked { get; set; }

        public string? ReasonRevoked { get; set; }

        public bool IsExpired => DateTime.UtcNow >= Expires;

        public bool IsRevoked => Revoked != null;

        public bool IsActive => !IsRevoked && !IsExpired;
    }
}