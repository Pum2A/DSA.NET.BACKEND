using DSA.Core.Entities;
using DSA.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DSA.Infrastructure.Repositories
{
    public class ModuleRepository : BaseRepository<Module>, IModuleRepository
    {
        public ModuleRepository(ApplicationDbContext context) : base(context)
        {
        }

        public override async Task<Module> GetByExternalIdAsync(string externalId)
        {
            return await _entities
                .FirstOrDefaultAsync(m => m.ExternalId == externalId);
        }

        public async Task<Module> GetModuleWithLessonsAsync(int moduleId)
        {
            return await _entities
                .Include(m => m.Lessons.OrderBy(l => l.Id))
                .FirstOrDefaultAsync(m => m.Id == moduleId);
        }

        public async Task<Module> GetModuleWithLessonsByExternalIdAsync(string externalId)
        {
            return await _entities
                .Include(m => m.Lessons.OrderBy(l => l.Id))
                .FirstOrDefaultAsync(m => m.ExternalId == externalId);
        }
    }
}