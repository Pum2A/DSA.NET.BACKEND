using System;

namespace DSA.Core.Helpers
{
    public static class LevelingHelper
    {
        // Przykładowa progresja: XP = 100 * N^1.5, dla level N
        public static int GetXpForLevel(int level)
        {
            // Level 1: 0 XP, Level 2: 142, Level 3: 346, Level 4: 632, ...
            if (level <= 1) return 0;
            return (int)Math.Round(100 * Math.Pow(level - 1, 1.5));
        }

        // Podaj poziom na podstawie XP
        public static int GetLevelForXp(int xp)
        {
            int level = 1;
            while (GetXpForLevel(level + 1) <= xp)
            {
                level++;
                // zabezpieczenie na bardzo wysokie XP
                if (level > 1000) break;
            }
            return level;
        }

        // XP wymagane na kolejny poziom
        public static int GetXpForNextLevel(int level)
        {
            return GetXpForLevel(level + 1);
        }

        // XP wymagane na aktualny poziom (dolny próg)
        public static int GetXpForCurrentLevel(int level)
        {
            return GetXpForLevel(level);
        }

        // Ile XP brakuje do następnego poziomu
        public static int GetXpToNextLevel(int xp)
        {
            int level = GetLevelForXp(xp);
            int nextLevelXp = GetXpForNextLevel(level);
            return Math.Max(0, nextLevelXp - xp);
        }
    }
}