using Microsoft.AspNetCore.Identity;
using System;

namespace DSA.Core.Entities
{
    public class ApplicationUser : IdentityUser
    {
        // Podstawowe pola z IdentityUser:
        // Id, UserName, Email, PasswordHash, etc.

        // Dodatkowe pola
        public string FirstName { get; set; }
        public string LastName { get; set; }

        // Zmiana na nullable string
        public string? RefreshToken { get; set; }

        // Zmiana na nullable DateTime (jeśli potrzebne)
        public DateTime? RefreshTokenExpiryTime { get; set; }

        // Pola gamifikacji
        public int Level { get; set; } = 1;
        public int ExperiencePoints { get; set; } = 0;
    }
}