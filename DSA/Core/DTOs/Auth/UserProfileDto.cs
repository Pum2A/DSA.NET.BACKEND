namespace DSA.Core.DTOs.Auth
{
    public class UserProfileDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int ExperiencePoints { get; set; }
        public int Level { get; set; }
        public string[] Roles { get; set; }
    }
}