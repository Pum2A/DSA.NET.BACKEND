using DSA.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DSA.Core.Interfaces
{
    public interface IUserProgressRepository : IRepository<UserProgress>
    {
        Task<UserProgress> GetUserProgressAsync(string userId, int lessonId);
        Task<IEnumerable<UserProgress>> GetUserProgressForModuleAsync(string userId, int moduleId);
        Task<int> GetCompletedLessonsCountAsync(string userId);
    }
}