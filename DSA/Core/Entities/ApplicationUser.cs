using Microsoft.AspNetCore.Identity;
using System;

namespace DSA.Core.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public int Level { get; set; }
        public int ExperiencePoints { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Add this property to fix the error
        public virtual ICollection<UserProgress> UserProgresses { get; set; }
    }
}