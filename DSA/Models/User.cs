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

        public string? VerificationToken { get; set; }
        public DateTime? VerificationTokenExpires { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpires { get; set; }

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
        public Guid UserId { get; set; } // Foreign key to User
        public User User { get; set; } = null!; // Navigation property

        public string Token { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
        public DateTime CreatedAt { get; set; } // Renamed from 'Created' for clarity
        public DateTime? RevokedAt { get; set; } // Renamed from 'Revoked' for clarity
        public string? ReasonRevoked { get; set; }
        public string? ReplacedByToken { get; set; }

        [NotMapped]
        public bool IsExpired => DateTime.UtcNow >= Expires;
        [NotMapped]
        public bool IsRevoked => RevokedAt != null;
        [NotMapped]
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}