using DSA.Core.Entities;
using System.Threading.Tasks;

namespace DSA.Core.Interfaces
{
    public interface IModuleRepository : IRepository<Module>
    {
        Task<Module> GetModuleWithLessonsAsync(int moduleId);
        Task<Module> GetModuleWithLessonsByExternalIdAsync(string externalId);
    }
}