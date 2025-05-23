using DSA.Core.Entities.Learning;
using System.Threading.Tasks;

namespace DSA.Core.Interfaces
{
    public interface ILessonRepository : IRepository<Lesson>
    {
        Task<Lesson> GetLessonWithStepsAsync(int lessonId);
        Task<Lesson> GetLessonWithStepsByExternalIdAsync(string externalId);
    }
}