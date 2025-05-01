using DSA.Core.Entities;
using System.Threading.Tasks;

namespace DSA.Core.Interfaces
{
    public interface IUserService
    {
        Task<ApplicationUser> GetUserByIdAsync(string userId);
        Task<bool> AddExperienceAsync(string userId, int amount);
        Task<int> GetUserLevelAsync(string userId);

        Task CheckAndNotifyLessonAchievementsAsync(string userId);
        Task CheckAndNotifyStreakAsync(string userId);

    }
}