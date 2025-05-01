namespace DSA.Core.Achieviements
{
    public class UserAchievementContext
    {
        public int PreviousXp { get; init; }
        public int CurrentXp { get; init; }
        public int PreviousLevel { get; init; }
        public int CurrentLevel { get; init; }
        public int CompletedLessons { get; init; }
        public int Streak { get; init; }
        // Dodaj co potrzebujesz
    }
}
