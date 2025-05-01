namespace DSA.Core.Achieviements
{
    public class AchievementRule
    {
        public string Key { get; init; }
        public string Message { get; init; }
        public string Type { get; init; } = "achievement";
        public Func<UserAchievementContext, bool> Condition { get; init; }
    }
}
