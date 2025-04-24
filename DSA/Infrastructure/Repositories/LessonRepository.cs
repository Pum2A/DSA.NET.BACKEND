using DSA.Core.Entities;
using DSA.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DSA.Infrastructure.Repositories
{
    public class LessonRepository : BaseRepository<Lesson>, ILessonRepository
    {
        public LessonRepository(ApplicationDbContext context) : base(context)
        {
        }

        public override async Task<Lesson> GetByExternalIdAsync(string externalId)
        {
            return await _entities
                .FirstOrDefaultAsync(l => l.ExternalId == externalId);
        }

        public async Task<Lesson> GetLessonWithStepsAsync(int lessonId)
        {
            return await _entities
                .Include(l => l.Steps.OrderBy(s => s.Order))
                .FirstOrDefaultAsync(l => l.Id == lessonId);
        }

        public async Task<Lesson> GetLessonWithStepsByExternalIdAsync(string externalId)
        {
            return await _entities
                .Include(l => l.Steps.OrderBy(s => s.Order))
                .FirstOrDefaultAsync(l => l.ExternalId == externalId);
        }
    }
}