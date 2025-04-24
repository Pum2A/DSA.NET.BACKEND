using System.Collections.Generic;

namespace DSA.Core.DTOs.Auth
{
    public class AuthResponse
    {
        public bool Succeeded { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public string Expiration { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string[] Roles { get; set; }
        public string[] Errors { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    }
}