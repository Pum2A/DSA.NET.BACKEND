using DSA.Core.Entities;
using DSA.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DSA.Infrastructure.Repositories
{
    public class UserProgressRepository : BaseRepository<UserProgress>, IUserProgressRepository
    {
        public UserProgressRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<UserProgress> GetUserProgressAsync(string userId, int lessonId)
        {
            return await _entities
                .FirstOrDefaultAsync(up => up.UserId == userId && up.LessonId == lessonId);
        }

        public async Task<IEnumerable<UserProgress>> GetUserProgressForModuleAsync(string userId, int moduleId)
        {
            return await _context.UserProgress
                .Include(up => up.Lesson)
                .Where(up => up.UserId == userId && up.Lesson.ModuleId == moduleId)
                .ToListAsync();
        }

        public async Task<int> GetCompletedLessonsCountAsync(string userId)
        {
            return await _entities
                .CountAsync(up => up.UserId == userId && up.IsCompleted);
        }
    }
}