using DSA.Core.Achieviements;
using System.Collections.Generic;

public static class AchievementCatalog
{
    public static List<AchievementRule> Rules = new()
    {
        new AchievementRule
        {
            Key = "first-lesson",
            Message = "🎉 Osiągnięcie: Ukończono pierwszą lekcję!",
            Condition = ctx => ctx.CompletedLessons == 1
        },
        new AchievementRule
        {
            Key = "10-lessons",
            Message = "📚 Osiągnięcie: Ukończono 10 lekcji!",
            Condition = ctx => ctx.CompletedLessons == 10
        },
        new AchievementRule
        {
            Key = "1000-xp",
            Message = "🏅 Zdobyto osiągnięcie: 1000 XP!",
            Condition = ctx => ctx.PreviousXp < 1000 && ctx.CurrentXp >= 1000
        },
        new AchievementRule
        {
            Key = "level-5",
            Message = "🏆 Osiągnięcie: Poziom 5!",
            Condition = ctx => ctx.PreviousLevel < 5 && ctx.CurrentLevel >= 5
        },
        new AchievementRule
        {
            Key = "streak-7",
            Message = "🔥 Osiągnięcie: 7-dniowy streak!",
            Condition = ctx => ctx.Streak == 7
        },
        // Dodawaj kolejne reguły
    };
}