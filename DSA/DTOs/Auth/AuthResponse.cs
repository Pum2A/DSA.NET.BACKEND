using System;

namespace DSA.DTOs.Auth
{
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public UserDto? User { get; set; }
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public bool EmailVerified { get; set; }
        public int XpPoints { get; set; }
        public int CurrentStreak { get; set; }
        public int MaxStreak { get; set; }
    }
}